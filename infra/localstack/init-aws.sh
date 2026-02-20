#!/usr/bin/env bash
set -euo pipefail

echo "Initializing LocalStack resources..."

awslocal s3api create-bucket \
  --bucket public-safety-lab-evidence-dev \
  --region us-east-1 >/dev/null 2>&1 || true

awslocal sqs create-queue \
  --queue-name incident-events-dev >/dev/null

echo "LocalStack resources are ready."
