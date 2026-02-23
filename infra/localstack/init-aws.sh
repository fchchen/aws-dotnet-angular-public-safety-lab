#!/bin/bash
set -euo pipefail

echo "Initializing LocalStack AWS resources..."

REGION="us-east-1"

# DynamoDB table
awslocal dynamodb create-table \
  --table-name incident_items \
  --attribute-definitions \
    AttributeName=PK,AttributeType=S \
    AttributeName=SK,AttributeType=S \
    AttributeName=Status,AttributeType=S \
    AttributeName=CreatedAt,AttributeType=S \
  --key-schema \
    AttributeName=PK,KeyType=HASH \
    AttributeName=SK,KeyType=RANGE \
  --global-secondary-indexes \
    '[{
      "IndexName": "StatusCreatedAtIndex",
      "KeySchema": [
        {"AttributeName": "Status", "KeyType": "HASH"},
        {"AttributeName": "CreatedAt", "KeyType": "RANGE"}
      ],
      "Projection": {"ProjectionType": "ALL"}
    }]' \
  --billing-mode PAY_PER_REQUEST \
  --region "$REGION"

echo "Created DynamoDB table: incident_items"

# S3 bucket
awslocal s3 mb s3://public-safety-lab-evidence-dev --region "$REGION"

awslocal s3api put-bucket-cors \
  --bucket public-safety-lab-evidence-dev \
  --cors-configuration '{
    "CORSRules": [{
      "AllowedHeaders": ["*"],
      "AllowedMethods": ["GET", "PUT", "POST"],
      "AllowedOrigins": ["http://localhost:4200"],
      "ExposeHeaders": ["ETag"]
    }]
  }'

echo "Created S3 bucket: public-safety-lab-evidence-dev"

# SQS queues
awslocal sqs create-queue \
  --queue-name incident-events-dev-dlq \
  --region "$REGION"

awslocal sqs create-queue \
  --queue-name incident-events-dev \
  --attributes '{
    "VisibilityTimeout": "60",
    "RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:us-east-1:000000000000:incident-events-dev-dlq\",\"maxReceiveCount\":\"5\"}"
  }' \
  --region "$REGION"

echo "Created SQS queues: incident-events-dev, incident-events-dev-dlq"

echo "LocalStack initialization complete."
