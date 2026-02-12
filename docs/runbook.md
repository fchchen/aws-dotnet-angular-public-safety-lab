# Runbook

## Local Development
### API
```bash
dotnet run --project src/PublicSafetyLab.Api
```

### Worker
```bash
dotnet run --project src/PublicSafetyLab.Worker
```

### Web
```bash
cd web
npm install
npm start
```

## LocalStack Option
```bash
scripts/localstack-up.sh
```
Then set:
- `AwsResources__UseAws=true`
- `AwsResources__ServiceUrl=http://localhost:4566`

## Terraform Deploy
```bash
cd infra/terraform
terraform init
terraform plan
terraform apply
```

## EC2 Deploy
```bash
scripts/deploy-ec2.sh <ec2-host> <ssh-key-path>
```

## Teardown
```bash
scripts/destroy-dev.sh
```

## Operational Checks
- API health: `GET /api/v1/incidents`
- Queue depth: SQS metrics in CloudWatch
- Error logs: CloudWatch logs from EC2 instance
- Cost watch: Billing alarms ($5, $15)
