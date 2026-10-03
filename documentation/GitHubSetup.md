# GitHub Setup

Repository settings:

1. Set CAGE_GITHUB_TOKEN secret to a personal access token with the scopes "read:packages" and "write:packages".
2. Set CAGE_AZURE_CLIENT_SECRET secret to the client secret of the app registration for updating Azure Container Apps (obtained from Microsoft Entra).
3. Set CAGE_EDITOR_SWA_DEPLOYMENT_TOKEN secret to the deployment token of the Azure Static Web App for the editor.

Package settings:

1. Link package to repository
2. Change package visibility to public
