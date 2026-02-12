# TDD Guidelines for This Repo

## Rule
No production change is complete unless it starts with a failing test and ends with all tests green.

## Red-Green-Refactor Checklist
1. Add a failing test in the correct layer.
2. Implement minimal code to pass.
3. Refactor without changing behavior.
4. Re-run the relevant test set.

## Test Layers
- Domain tests: validation and state transitions.
- API tests: endpoint behavior and contracts.
- Infrastructure tests: adapter behavior (SQS request mapping).

## Pull Request Expectation
- Include which tests were added first.
- Include command output for `dotnet test PublicSafetyLab.sln`.
