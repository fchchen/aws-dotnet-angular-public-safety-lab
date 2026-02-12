# Terraform Deployment

## Prerequisites
- AWS account credentials configured (`aws configure`)
- Terraform >= 1.5

## Usage
```bash
cp terraform.tfvars.example terraform.tfvars
# edit values in terraform.tfvars
terraform init
terraform plan
terraform apply
```

## Outputs
- DynamoDB table name
- S3 evidence bucket
- SQS queue URL
- EC2 public IP/DNS

## Free-Tier Notes
- Default is one `t3.micro` EC2 instance
- S3 lifecycle expires objects after 7 days
- Billing alarms at $5 and $15

## Destroy
```bash
terraform destroy
```
