variable "project_name" {
  description = "Project prefix used for resource names"
  type        = string
  default     = "public-safety-lab"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "dev"
}

variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "instance_type" {
  description = "EC2 instance type"
  type        = string
  default     = "t3.micro"
}

variable "allow_ssh_cidr" {
  description = "CIDR allowed to SSH into EC2"
  type        = string
  default     = "0.0.0.0/0"
}

variable "ssh_key_name" {
  description = "Optional existing EC2 key pair name"
  type        = string
  default     = null
}

variable "alert_email" {
  description = "Optional email for billing alarms"
  type        = string
  default     = ""
}

variable "enable_ec2" {
  description = "Whether to provision an EC2 instance"
  type        = bool
  default     = true
}
