# Agent Instructions

## Frontend testing

The `cage-editor` Angular frontend does not yet have a fleshed-out test strategy. Do not add new
frontend unit/integration tests (e.g. `*.spec.ts` files) at this time, even for new components,
services, or interceptors. Existing tests may still be run and updated as needed. This restriction
will be lifted once a test strategy has been established.

## Angular conventions

In `cage-editor`, prefer `@Service()` over `@Injectable()` for services (the default in our Angular
version).

Reuse data cached in frontend services rather than issuing duplicate HTTP requests.

## Angular styling

- Put recurring patterns (page/section layout, headers, lists, form actions, inputs, button spacing) in shared partials under `src/styles/` and import them from `src/styles.scss`. Do not copy them into component styles.
- Use Angular Material theming for color, typography, shape and density: configure it via `mat.theme()` in `styles.scss` and consume `--mat-sys-*` tokens. No hard-coded colors or font sizes.
- Use the shared spacing tokens (`--app-space-*`) instead of ad-hoc rem values where one fits.
- Component `.scss` files contain only rules specific to that component's markup.

## C# conventions

Do not use the "Base" suffix for base class names (e.g. EndpointTest, not EndpointTestBase), as per Microsoft's [Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/).

In tests, use service constants for configured data limits instead of repeating their numeric values.

Structure backend tests with `GIVEN`, `WHEN`, and `THEN` comments.

In endpoint tests, use `EndpointTestDao` for all `GIVEN` setup and `THEN` verification; use HTTP requests only for the `WHEN` action being tested.

On the backend, all service methods that create, update or delete entities must log these actions.
