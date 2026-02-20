# Interview Talk Track

## Problem Framing
I built a public-safety incident pipeline that balances low-latency intake with asynchronous reliability.

## Why This Architecture
- API handles command/query quickly.
- SQS decouples expensive or failure-prone processing.
- Worker handles retries and marks final state.
- Storage is provider-switched: DynamoDB or PostgreSQL from config.
- PostgreSQL schema uses query-aligned composite indexes for list/filter patterns.
- S3 avoids binary payload bloat in primary data store.

## Leadership Signals You Can Explain
- Chose free-tier-safe architecture while preserving migration path to ECS/EKS.
- Defined clear contracts across API, domain, and queue message schemas.
- Introduced TDD guardrails and layered tests for fast iteration with confidence.
- Added operational safeguards (billing alarms, lifecycle cleanup, DLQ).
- Added dependency health endpoints (`/healthz/live`, `/healthz/ready`) and provider-aware checks.
- Added API-key auth with tenant claims and migration-friendly legacy fallback.
- Added Kubernetes manifests (Kustomize base + overlays) with probes and HPA.
- Added structured JSON logging (Serilog) with correlation propagation API -> SQS -> worker.

## Tradeoffs
- Single EC2 is not highly available, but cost-effective for skill-building.
- Snapshot payload in DynamoDB increases agility but requires schema discipline.
- API-key auth is intentionally simple for demo speed; production auth should move to OIDC/Cognito.

## If Asked “How would you scale this?”
1. Move API/worker to ECS or EKS.
2. Add ALB and multi-AZ deployment.
3. Replace API key auth with JWT claims + policy auth.
4. Add idempotency keys and outbox pattern for stronger delivery guarantees.
5. Add full observability stack (traces, SLOs, structured dashboards).
