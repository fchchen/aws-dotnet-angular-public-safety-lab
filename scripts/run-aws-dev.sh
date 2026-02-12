#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TF_DIR="$ROOT_DIR/infra/terraform"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1"
    exit 1
  fi
}

require_command terraform
require_command dotnet
require_command npm

if [[ ! -f "$TF_DIR/terraform.tfstate" ]]; then
  echo "Terraform state not found at $TF_DIR/terraform.tfstate"
  echo "Run 'terraform apply' in infra/terraform first."
  exit 1
fi

export AwsResources__UseAws=true
export AwsResources__Region=us-east-1
export AwsResources__IncidentTableName="$(terraform -chdir="$TF_DIR" output -raw incident_table_name)"
export AwsResources__EvidenceBucketName="$(terraform -chdir="$TF_DIR" output -raw evidence_bucket_name)"
export AwsResources__IncidentQueueUrl="$(terraform -chdir="$TF_DIR" output -raw incident_queue_url)"
export ASPNETCORE_ENVIRONMENT=Development

echo "Starting API, worker, and web in AWS mode..."
echo "Incident table: $AwsResources__IncidentTableName"
echo "Evidence bucket: $AwsResources__EvidenceBucketName"
echo "Incident queue: $AwsResources__IncidentQueueUrl"

dotnet run --project "$ROOT_DIR/src/PublicSafetyLab.Api" --urls http://localhost:5200 &
API_PID=$!

dotnet run --project "$ROOT_DIR/src/PublicSafetyLab.Worker" &
WORKER_PID=$!

(cd "$ROOT_DIR/web" && npm start) &
WEB_PID=$!

cleanup() {
  echo
  echo "Stopping dev services..."
  kill "$API_PID" "$WORKER_PID" "$WEB_PID" 2>/dev/null || true
}

trap cleanup INT TERM EXIT
wait
