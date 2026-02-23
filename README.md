# Public Safety Lab

Full-stack incident management system built as a portfolio project to demonstrate production-level architecture across the .NET, Angular, and AWS ecosystem. Models a first-responder workflow — operators create incidents, upload evidence to S3, and dispatch async processing through SQS — all backed by DynamoDB (or Postgres), Terraform IaC, Kubernetes manifests, and a CI/CD pipeline. Every feature starts with a failing test.

## Architecture

```
┌─────────────┐       ┌──────────────────┐       ┌────────────┐
│  Angular UI  │──────▶│  .NET 10 API     │──────▶│  DynamoDB   │
│  (port 4200) │       │  (port 5200)     │       │  or Postgres│
└─────────────┘       └────────┬─────────┘       └────────────┘
                               │
                          POST /process
                               │
                        ┌──────▼──────┐       ┌────────────┐
                        │     SQS     │──────▶│  .NET 10   │
                        │   Queue     │       │  Worker    │
                        └─────────────┘       └────────────┘
                               │
                        ┌──────▼──────┐
                        │     DLQ     │
                        └─────────────┘

                        ┌─────────────┐
                        │     S3      │  ◀── pre-signed upload URLs
                        │  (evidence) │
                        └─────────────┘
```

The API handles CRUD and dispatches work via SQS. The Worker polls the queue and transitions incident state. In local mode, in-memory adapters replace all AWS services so the app runs with zero cloud dependencies.

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| API | ASP.NET Core Web API | .NET 10 |
| Worker | .NET Background Service | .NET 10 |
| Frontend | Angular (standalone components) | 17 |
| Database | DynamoDB (AWS) / PostgreSQL (local) | — |
| Object Storage | S3 | — |
| Messaging | SQS + Dead-Letter Queue | — |
| IaC | Terraform | 1.x |
| Containers | Docker, Docker Compose | — |
| Orchestration | Kubernetes (Kustomize overlays) | — |
| CI/CD | GitHub Actions | — |
| Testing | xUnit, FluentAssertions, Moq, Karma | — |

## Quick Start

### Local (no dependencies)

Runs entirely in-memory — no Docker, no AWS, no database.

```bash
# Backend
dotnet restore PublicSafetyLab.sln
dotnet test PublicSafetyLab.sln
dotnet run --project src/PublicSafetyLab.Api --urls http://localhost:5200

# Worker (optional — in-memory queue by default)
dotnet run --project src/PublicSafetyLab.Worker

# Frontend
cd web && npm install && npm start
```

API: `http://localhost:5200` | Swagger: `http://localhost:5200/swagger` | UI: `http://localhost:4200`

### Local with Docker + LocalStack

Runs the full stack with real AWS-compatible services locally via LocalStack and PostgreSQL.

```bash
docker compose -f docker-compose.local.yml up --build
```

This starts PostgreSQL, LocalStack (DynamoDB + S3 + SQS), the API, and the Worker — all wired together with health checks.

### Local with Kubernetes

Deploys to a local Kubernetes cluster (e.g. Docker Desktop, minikube, kind) using Kustomize overlays. Includes LocalStack, PostgreSQL, HPA autoscaling, and dev/prod overlay separation.

```bash
# Build images locally
docker build -f src/PublicSafetyLab.Api/Dockerfile -t public-safety-lab-api:local .
docker build -f src/PublicSafetyLab.Worker/Dockerfile -t public-safety-lab-worker:local .

# Apply dev overlay
kubectl apply -k k8s/overlays/dev
```

K8s manifests include: namespace isolation, ConfigMap/Secret management, HPA (2–6 replicas, 70% CPU target), and Kustomize base/overlay structure for dev vs prod. Health probes hit `/healthz/live` and `/healthz/ready`.

> **Note:** HPA requires `metrics-server`, which isn't installed by default on Docker Desktop. Autoscaling config is correct but stays inactive locally. Install via `kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml` if needed.

### AWS Mode

Set environment variables or update `appsettings.json` in both the API and Worker:

```
AwsResources__UseAws=true
AwsResources__Region=us-east-1
AwsResources__IncidentTableName=<table>
AwsResources__EvidenceBucketName=<bucket>
AwsResources__IncidentQueueUrl=<queue-url>
```

After Terraform provisioning, run all services with one command:

```bash
./scripts/run-aws-dev.sh
```

## Provision AWS (Terraform)

```bash
cd infra/terraform
cp terraform.tfvars.example terraform.tfvars   # edit with your values
terraform init && terraform plan && terraform apply
```

Outputs: DynamoDB table name, S3 bucket name, SQS queue URL, EC2 endpoint.

## Free-Tier Cost Guardrails

| Resource | Config |
|---|---|
| EC2 | Single `t3.micro` |
| DynamoDB | On-demand (pay-per-request) |
| S3 | 7-day lifecycle expiration |
| SQS | Standard queue + DLQ |
| Billing | Alarms at $0.25 and $0.50 |
| Teardown | `scripts/destroy-dev.sh` |

## CI/CD

Two GitHub Actions workflows:

- **CI** (`.github/workflows/ci.yml`) — runs on every push and PR: .NET restore/build/test + Angular install/build/test (parallel jobs)
- **Deploy EC2** (`.github/workflows/deploy-ec2.yml`) — manual trigger: builds both stacks, rsync to EC2, restarts Docker Compose

## Testing

TDD workflow — every feature follows red/green/refactor.

```bash
dotnet test PublicSafetyLab.sln          # all backend tests
cd web && npm test -- --watch=false      # Angular unit tests
```

Test projects:
- `tests/PublicSafetyLab.Domain.Tests` — domain validation, status transitions
- `tests/PublicSafetyLab.Api.Tests` — API integration (happy path + error cases)
- `tests/PublicSafetyLab.Infrastructure.IntegrationTests` — SQS adapter behavior
- `web/` — Angular component and service specs

## Project Structure

```
src/
  PublicSafetyLab.Api/            REST API + Swagger
  PublicSafetyLab.Worker/         SQS background consumer
  PublicSafetyLab.Application/    Use cases + interfaces
  PublicSafetyLab.Domain/         Entities + value objects
  PublicSafetyLab.Infrastructure/ AWS + in-memory adapters
  PublicSafetyLab.Contracts/      Shared DTOs
tests/
  PublicSafetyLab.Domain.Tests/
  PublicSafetyLab.Api.Tests/
  PublicSafetyLab.Infrastructure.IntegrationTests/
web/                              Angular 17 SPA
k8s/
  base/                           Deployments, services, HPA
  overlays/dev/                   LocalStack + Postgres
  overlays/prod/                  AWS-targeted config
infra/terraform/                  Free-tier AWS provisioning
scripts/                          Dev helpers (run, deploy, teardown)
docs/                             Architecture, runbook, interview prep
```

## Interview Prep Docs

- [`docs/architecture.md`](docs/architecture.md) — component breakdown, data model, reliability patterns
- [`docs/runbook.md`](docs/runbook.md) — operational procedures
- [`docs/interview-talk-track.md`](docs/interview-talk-track.md) — how to walk through this project in an interview
- [`docs/tdd-guidelines.md`](docs/tdd-guidelines.md) — TDD conventions used in this repo
