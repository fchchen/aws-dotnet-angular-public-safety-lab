output "incident_table_name" {
  value       = aws_dynamodb_table.incident_items.name
  description = "DynamoDB incident table"
}

output "evidence_bucket_name" {
  value       = aws_s3_bucket.evidence.bucket
  description = "S3 bucket for incident evidence"
}

output "incident_queue_url" {
  value       = aws_sqs_queue.incident_queue.id
  description = "SQS queue URL"
}

output "ec2_public_ip" {
  value       = var.enable_ec2 ? aws_instance.app[0].public_ip : null
  description = "Public IP of app EC2 instance"
}

output "ec2_public_dns" {
  value       = var.enable_ec2 ? aws_instance.app[0].public_dns : null
  description = "Public DNS of app EC2 instance"
}
