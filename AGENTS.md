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

Use TypeScript DTOs for HTTP request bodies and response data; do not send inline object literals or use untyped `HttpClient` responses.

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

For backend endpoint CRUD tests, cover unauthenticated requests and attempts to modify data in foreign organizations whenever applicable. For create and update operations, verify that values exceeding maximum string lengths are rejected; for create operations, verify that maximum entity counts cannot be exceeded.

On the backend, all service methods that create, update or delete entities must log these actions.

## Verifying changes with build commands

The pipeline (`.github/workflows/default.yml`) runs the scripts in `build/` with PowerShell (`pwsh`). Never run
the deploy scripts (`build/Deploy*.ps1`) or `build/PushGitTag.ps1`: they deploy to Azure or push Git tags.

- `build/BuildSimulation.ps1` is safe to run: it only cleans, builds and tests the simulation library.
- `build/BuildEditorBackend.ps1` and `build/BuildGameBackend.ps1` also build and push Docker images to ghcr.io
  (requires `CAGE_GITHUB_USER`/`CAGE_GITHUB_TOKEN` and Docker), so they fail in the agent environment. Do not run them.
  Instead, run their build and test steps directly, e.g. in `source/Cage.Backend.Editor` and
  `source/Cage.Backend.Editor.Tests` (or the `Game` equivalents): `dotnet build`, then `dotnet test`
  (optionally `dotnet publish` for a production build).
- For the editor frontend, run `npm ci` and `ng build` (production configuration by default) in `source/cage-editor`.
  Do not run `build/DeployEditorFrontend.ps1`, which additionally deploys the result with `swa deploy`.
