#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "usage: $0 <ec2-host> <ssh-key-path>"
  exit 1
fi

EC2_HOST="$1"
SSH_KEY="$2"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TF_DIR="$ROOT_DIR/infra/terraform"

if ! command -v terraform >/dev/null 2>&1; then
  echo "terraform is required"
  exit 1
fi

AWS_REGION="${AWS_REGION:-us-east-1}"
AWS_STORAGE_PROVIDER="${AWS_STORAGE_PROVIDER:-DynamoDb}"
INCIDENT_TABLE_NAME="${INCIDENT_TABLE_NAME:-$(terraform -chdir="$TF_DIR" output -raw incident_table_name)}"
EVIDENCE_BUCKET_NAME="${EVIDENCE_BUCKET_NAME:-$(terraform -chdir="$TF_DIR" output -raw evidence_bucket_name)}"
INCIDENT_QUEUE_URL="${INCIDENT_QUEUE_URL:-$(terraform -chdir="$TF_DIR" output -raw incident_queue_url)}"

rsync -avz -e "ssh -i ${SSH_KEY}" \
  --exclude '.git' \
  --exclude 'web/node_modules' \
  --exclude 'infra/terraform/.terraform' \
  "$ROOT_DIR/" "ec2-user@${EC2_HOST}:~/public-safety-lab/"

ssh -i "$SSH_KEY" "ec2-user@${EC2_HOST}" <<REMOTE
  cd ~/public-safety-lab
  AWS_STORAGE_PROVIDER='${AWS_STORAGE_PROVIDER}' \
  AWS_REGION='${AWS_REGION}' \
  INCIDENT_TABLE_NAME='${INCIDENT_TABLE_NAME}' \
  EVIDENCE_BUCKET_NAME='${EVIDENCE_BUCKET_NAME}' \
  INCIDENT_QUEUE_URL='${INCIDENT_QUEUE_URL}' \
  docker compose -f docker-compose.ec2.yml up -d --build
REMOTE
