# cage
Backend, editor frontend and libraries of the CArd Game Engine (CAGE).

## Development Environment Setup

### Install Tools

1. Install .NET Framework 10.
2. Install Docker.
3. Install Visual Studio Code.
4. Run build/InstallTools.ps1.
5. Install Visual Studio Code extensions:
    1. Angular Language Service
    2. C# & C# Dev Kit
    3. Postman
    4. Powershell

### GitHub Token

At GitHub, create a "Personal access token (classic)" with the scopes "read:packages" and "write:packages", and store it as environment variable `GITHUB_TOKEN`.

See the following two links for details:

* https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry#authenticating-with-a-personal-access-token-classic
* https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-personal-access-token-classic

Then, set the environment variable `GITHUB_USER` to your GitHub user name.

### Azure Client Secret

Set your environment variable `AZURE_CLIENT_SECRET` to the app registration client secret obtained from Microsoft Entra.

### Azure Static Web App Deployment Token

Set your environment variable `CAGE_EDITOR_SWA_DEPLOYMENT_TOKEN` to the deployment token of the Azure Static Web App for the editor.
