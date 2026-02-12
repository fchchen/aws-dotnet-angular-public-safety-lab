#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "usage: $0 <ec2-host> <ssh-key-path>"
  exit 1
fi

EC2_HOST="$1"
SSH_KEY="$2"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

rsync -avz -e "ssh -i ${SSH_KEY}" \
  --exclude '.git' \
  --exclude 'web/node_modules' \
  --exclude 'infra/terraform/.terraform' \
  "$ROOT_DIR/" "ec2-user@${EC2_HOST}:~/public-safety-lab/"

ssh -i "$SSH_KEY" "ec2-user@${EC2_HOST}" <<'REMOTE'
  cd ~/public-safety-lab
  docker compose -f docker-compose.ec2.yml up -d --build
REMOTE
