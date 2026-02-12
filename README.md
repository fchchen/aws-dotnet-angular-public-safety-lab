# AWS Public Safety Lab (.NET + Angular + DynamoDB/S3/SQS)

A TDD-first portfolio project for interview preparation around:
- C# / .NET backend services
- Angular frontend workflows
- AWS cloud-native integration (DynamoDB, S3, SQS)
- Containerized deployment and CI/CD

This project models an incident workflow for first-responder/public-safety scenarios.

## Tech Stack
- Backend: .NET 10 Web API + Worker Service
- Frontend: Angular 17 standalone components
- AWS: DynamoDB, S3, SQS, CloudWatch, EC2
- Infra as Code: Terraform
- Tests: xUnit + FluentAssertions + Moq + Angular unit tests

## Architecture at a Glance
- `web/`: operator UI to create incidents, request evidence upload URLs, and queue processing.
- `src/PublicSafetyLab.Api`: REST API.
- `src/PublicSafetyLab.Worker`: background queue consumer.
- `src/PublicSafetyLab.Infrastructure`: AWS and in-memory adapters.
- `infra/terraform`: free-tier-focused AWS provisioning.

See `docs/architecture.md` for details.

## TDD Workflow
Each feature follows red/green/refactor:
1. Add failing test.
2. Add minimal code to pass.
3. Refactor with tests still green.

Current backend tests:
- Domain validation and status transitions
- API happy-path integration
- SQS adapter behavior

## Quick Start (Local, No AWS Required)
### 1) Backend
```bash
dotnet restore PublicSafetyLab.sln
dotnet test PublicSafetyLab.sln
dotnet run --project src/PublicSafetyLab.Api --urls http://localhost:5200
```
API base URL is set to `http://localhost:5200` in local dev commands.
Swagger UI is available in development at `http://localhost:5200/swagger`.

### 2) Frontend
```bash
cd web
npm install
npm start
```
`npm start` now uses `web/proxy.conf.json` so `/api/*` routes to `http://localhost:5200`.
Use `npm run start:plain` if you want Angular dev server without proxy.

### 3) Run Worker (optional, in-memory queue by default)
```bash
dotnet run --project src/PublicSafetyLab.Worker
```

### Evidence Upload from UI
- Open an incident detail page.
- Select an image and click `Upload Selected Image`.
- In local mode (`AwsResources__UseAws=false`), upload is simulated (metadata only).
- In AWS mode, the browser uploads directly to S3 using the pre-signed URL.

## AWS Mode (Use Real AWS Services)
Set these values in `src/PublicSafetyLab.Api/appsettings.json` and `src/PublicSafetyLab.Worker/appsettings.json` or environment variables:
- `AwsResources__UseAws=true`
- `AwsResources__Region=us-east-1`
- `AwsResources__IncidentTableName=<table>`
- `AwsResources__EvidenceBucketName=<bucket>`
- `AwsResources__IncidentQueueUrl=<queue-url>`

Then run API + worker.

### One-Command AWS Dev Run
After Terraform apply is complete, run all three services (API + worker + web) with:
```bash
./scripts/run-aws-dev.sh
```

## Provision AWS (Terraform)
```bash
cd infra/terraform
cp terraform.tfvars.example terraform.tfvars
# edit terraform.tfvars
terraform init
terraform plan
terraform apply
```

Outputs include table name, bucket name, queue URL, and EC2 endpoint.

## Free-Tier Cost Guardrails
- Single `t3.micro` EC2 instance
- DynamoDB on-demand table
- S3 lifecycle expiration (7 days)
- SQS standard queue + DLQ
- Billing alarms at `$0.25` and `$0.50`
- One-command teardown: `scripts/destroy-dev.sh`

## Deployment
- Docker files for API and worker are included.
- `docker-compose.ec2.yml` runs API + worker on EC2.
- CI workflows:
  - `.github/workflows/ci.yml`
  - `.github/workflows/deploy-ec2.yml`

## Interview Readiness Docs
- `docs/architecture.md`
- `docs/runbook.md`
- `docs/interview-talk-track.md`
- `docs/tdd-guidelines.md`

## Notes
- Local mode defaults to in-memory adapters so the project runs without cloud dependencies.
- AWS adapters are production-oriented and activated via `AwsResources__UseAws=true`.
