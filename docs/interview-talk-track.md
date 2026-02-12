# Interview Talk Track

## Problem Framing
I built a public-safety incident pipeline that balances low-latency intake with asynchronous reliability.

## Why This Architecture
- API handles command/query quickly.
- SQS decouples expensive or failure-prone processing.
- Worker handles retries and marks final state.
- DynamoDB supports predictable key-based access and flexible payload iteration.
- S3 avoids binary payload bloat in primary data store.

## Leadership Signals You Can Explain
- Chose free-tier-safe architecture while preserving migration path to ECS/EKS.
- Defined clear contracts across API, domain, and queue message schemas.
- Introduced TDD guardrails and layered tests for fast iteration with confidence.
- Added operational safeguards (billing alarms, lifecycle cleanup, DLQ).

## Tradeoffs
- Single EC2 is not highly available, but cost-effective for skill-building.
- Snapshot payload in DynamoDB increases agility but requires schema discipline.
- Demo tenant model keeps focus on architecture; production auth should move to OIDC/Cognito.

## If Asked “How would you scale this?”
1. Move API/worker to ECS or EKS.
2. Add ALB and multi-AZ deployment.
3. Replace header tenant model with JWT claims + policy auth.
4. Add idempotency keys and outbox pattern for stronger delivery guarantees.
5. Add full observability stack (traces, SLOs, structured dashboards).
