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

### Default Local Mode
`appsettings.json` defaults to:
- `AwsResources:StorageProvider=InMemory`
- `AwsResources:UseAws=false`
- `Authentication:ApiKeys[0].Key=demo-api-key`

## PostgreSQL Local Mode (No AWS)
Set:
- `AwsResources__StorageProvider=PostgreSql`
- `AwsResources__UseAws=false`
- `AwsResources__PostgreSqlConnectionString=Host=localhost;Port=5432;Database=public_safety_lab;Username=postgres;Password=postgres`

## LocalStack Option (S3 + SQS)
```bash
scripts/localstack-up.sh
```
Then set:
- `AwsResources__UseAws=true`
- `AwsResources__ServiceUrl=http://localhost:4566`

## Local Full Stack (PostgreSQL + LocalStack + API + Worker)
```bash
docker compose -f docker-compose.local.yml up --build
```

## Kubernetes Manifests
Render base:
```bash
kubectl kustomize k8s/base
```

Render overlays:
```bash
kubectl kustomize k8s/overlays/dev
kubectl kustomize k8s/overlays/prod
```

### Secret Strategy
- `k8s/base/secret.yaml` is a placeholder template only.
- `k8s/overlays/dev/secret-patch.yaml` provides local/dev values.
- For production, replace placeholder secret management with External Secrets or CSI driver-backed secrets.

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
- API liveness: `GET /healthz/live`
- API readiness: `GET /healthz/ready`
- Queue depth: SQS metrics in CloudWatch
- Error logs: CloudWatch logs from EC2 instance
- Cost watch: Billing alarms ($5, $15)

## Auth
- Required header: `X-Api-Key`
- Default demo key in local config: `demo-api-key`
- Legacy tenant header compatibility can be toggled with `Authentication__AllowLegacyTenantHeader`

## Logging
- API and worker emit structured JSON logs via Serilog.
- Correlation is carried in `X-Correlation-Id` and propagated into queue message metadata.
