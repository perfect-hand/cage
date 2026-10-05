# Agent Instructions

## Frontend testing

The `cage-editor` Angular frontend does not yet have a fleshed-out test strategy. Do not add new
frontend unit/integration tests (e.g. `*.spec.ts` files) at this time, even for new components,
services, or interceptors. Existing tests may still be run and updated as needed. This restriction
will be lifted once a test strategy has been established.

## Angular conventions

In `cage-editor`, prefer `@Service()` over `@Injectable()` for services (the default in our Angular
version).
