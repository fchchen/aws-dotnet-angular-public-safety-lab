locals {
  prefix          = "${var.project_name}-${var.environment}"
  incident_table  = "incident-items-${var.environment}"
  evidence_bucket = "${var.project_name}-${var.environment}-evidence-${data.aws_caller_identity.current.account_id}"
  incident_queue  = "${var.project_name}-${var.environment}-incident-events"
  incident_dlq    = "${var.project_name}-${var.environment}-incident-events-dlq"
  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

data "aws_caller_identity" "current" {}

data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

data "aws_ssm_parameter" "al2023" {
  name = "/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64"
}

resource "aws_dynamodb_table" "incident_items" {
  name         = local.incident_table
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "PK"
  range_key    = "SK"

  attribute {
    name = "PK"
    type = "S"
  }

  attribute {
    name = "SK"
    type = "S"
  }

  attribute {
    name = "Status"
    type = "S"
  }

  attribute {
    name = "CreatedAt"
    type = "S"
  }

  global_secondary_index {
    name            = "StatusCreatedAtIndex"
    hash_key        = "Status"
    range_key       = "CreatedAt"
    projection_type = "ALL"
  }

  tags = local.common_tags
}

resource "aws_s3_bucket" "evidence" {
  bucket        = local.evidence_bucket
  force_destroy = true
  tags          = local.common_tags
}

resource "aws_s3_bucket_public_access_block" "evidence" {
  bucket                  = aws_s3_bucket.evidence.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "evidence" {
  bucket = aws_s3_bucket.evidence.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "evidence" {
  bucket = aws_s3_bucket.evidence.id

  rule {
    id     = "expire-old-evidence"
    status = "Enabled"

    expiration {
      days = 7
    }

    noncurrent_version_expiration {
      noncurrent_days = 7
    }
  }
}

resource "aws_sqs_queue" "incident_dlq" {
  name                      = local.incident_dlq
  message_retention_seconds = 1209600
  tags                      = local.common_tags
}

resource "aws_sqs_queue" "incident_queue" {
  name                       = local.incident_queue
  visibility_timeout_seconds = 60

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.incident_dlq.arn
    maxReceiveCount     = 5
  })

  tags = local.common_tags
}

resource "aws_security_group" "app" {
  count       = var.enable_ec2 ? 1 : 0
  name        = "${local.prefix}-app-sg"
  description = "Security group for public safety lab app"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    description = "SSH"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = [var.allow_ssh_cidr]
  }

  ingress {
    description = "API"
    from_port   = 8080
    to_port     = 8080
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = local.common_tags
}

data "aws_iam_policy_document" "ec2_assume_role" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "app" {
  count              = var.enable_ec2 ? 1 : 0
  name               = "${local.prefix}-ec2-role"
  assume_role_policy = data.aws_iam_policy_document.ec2_assume_role.json
  tags               = local.common_tags
}

data "aws_iam_policy_document" "app_permissions" {
  statement {
    sid = "DynamoDbAccess"

    actions = [
      "dynamodb:GetItem",
      "dynamodb:PutItem",
      "dynamodb:Query",
      "dynamodb:UpdateItem"
    ]

    resources = [
      aws_dynamodb_table.incident_items.arn,
      "${aws_dynamodb_table.incident_items.arn}/index/*"
    ]
  }

  statement {
    sid = "S3Access"

    actions = [
      "s3:GetObject",
      "s3:PutObject",
      "s3:DeleteObject"
    ]

    resources = [
      "${aws_s3_bucket.evidence.arn}/*"
    ]
  }

  statement {
    sid = "SqsAccess"

    actions = [
      "sqs:SendMessage",
      "sqs:ReceiveMessage",
      "sqs:DeleteMessage",
      "sqs:GetQueueAttributes"
    ]

    resources = [
      aws_sqs_queue.incident_queue.arn,
      aws_sqs_queue.incident_dlq.arn
    ]
  }

  statement {
    sid = "CloudWatchLogs"

    actions = [
      "logs:CreateLogGroup",
      "logs:CreateLogStream",
      "logs:PutLogEvents"
    ]

    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "app" {
  count  = var.enable_ec2 ? 1 : 0
  name   = "${local.prefix}-policy"
  role   = aws_iam_role.app[0].id
  policy = data.aws_iam_policy_document.app_permissions.json
}

resource "aws_iam_instance_profile" "app" {
  count = var.enable_ec2 ? 1 : 0
  name  = "${local.prefix}-profile"
  role  = aws_iam_role.app[0].name
}

resource "aws_instance" "app" {
  count                       = var.enable_ec2 ? 1 : 0
  ami                         = data.aws_ssm_parameter.al2023.value
  instance_type               = var.instance_type
  subnet_id                   = data.aws_subnets.default.ids[0]
  vpc_security_group_ids      = [aws_security_group.app[0].id]
  key_name                    = var.ssh_key_name
  iam_instance_profile        = aws_iam_instance_profile.app[0].name
  associate_public_ip_address = true

  user_data = <<-EOT
    #!/bin/bash
    dnf update -y
    dnf install -y docker docker-compose-plugin git
    systemctl enable docker
    systemctl start docker
    usermod -aG docker ec2-user
  EOT

  tags = merge(local.common_tags, {
    Name = "${local.prefix}-ec2"
  })
}

resource "aws_sns_topic" "billing_alerts" {
  name = "${local.prefix}-billing-alerts"
  tags = local.common_tags
}

resource "aws_sns_topic_subscription" "billing_email" {
  count     = var.alert_email != "" ? 1 : 0
  topic_arn = aws_sns_topic.billing_alerts.arn
  protocol  = "email"
  endpoint  = var.alert_email
}

resource "aws_cloudwatch_metric_alarm" "billing_alarm_5" {
  alarm_name          = "${local.prefix}-billing-5-usd"
  comparison_operator = "GreaterThanOrEqualToThreshold"
  evaluation_periods  = 1
  metric_name         = "EstimatedCharges"
  namespace           = "AWS/Billing"
  period              = 21600
  statistic           = "Maximum"
  threshold           = 5
  alarm_description   = "Alarm when estimated charges exceed $5"
  alarm_actions       = [aws_sns_topic.billing_alerts.arn]

  dimensions = {
    Currency = "USD"
  }
}

resource "aws_cloudwatch_metric_alarm" "billing_alarm_15" {
  alarm_name          = "${local.prefix}-billing-15-usd"
  comparison_operator = "GreaterThanOrEqualToThreshold"
  evaluation_periods  = 1
  metric_name         = "EstimatedCharges"
  namespace           = "AWS/Billing"
  period              = 21600
  statistic           = "Maximum"
  threshold           = 15
  alarm_description   = "Alarm when estimated charges exceed $15"
  alarm_actions       = [aws_sns_topic.billing_alerts.arn]

  dimensions = {
    Currency = "USD"
  }
}
