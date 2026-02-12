#!/usr/bin/env bash
set -euo pipefail

if ! command -v docker >/dev/null 2>&1; then
  echo "docker is required"
  exit 1
fi

docker run -d --name public-safety-localstack -p 4566:4566 localstack/localstack:latest || true

echo "LocalStack endpoint: http://localhost:4566"
echo "Set AwsResources__UseAws=true and AwsResources__ServiceUrl=http://localhost:4566 for API/Worker"
