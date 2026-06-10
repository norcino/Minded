## Summary

- Describe the feature/fix.

## Architecture Checklist

- [ ] Controllers are thin and delegate through `IRestMediator`.
- [ ] No controller injects `IMindedExampleContext` or performs direct EF data access.
- [ ] Endpoint logic is implemented through CQRS commands/queries and handlers.
- [ ] Validation is implemented via validators/decorators (not inline in controllers).
- [ ] Authorization is policy/decorator/handler driven.
- [ ] Added/updated tests to prevent architecture regressions.

## Verification

- [ ] `dotnet build` succeeds.
- [ ] Relevant tests pass.
