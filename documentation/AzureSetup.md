# Azure Setup

## Overview

* Microsoft Entra External Id tenant (+ 2 app registrations)
* Function App (+ storage account, application insights)

## Naming Conventions

All resource names are following the naming convention

application-environment-resourcetype

with:

* application being `cageeditor` or `cagegame`
* environment being `dev` or `prod`
* resourcetype according to [official Microsoft recommendations](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations)

Example:

`cageeditor-dev-rg`

## Resource Tags

All resources and resource groups need to have the following tags:

* application
* environment

These tags are enforced for resource groups and resources through Azure Policy:

* Require a tag on resource groups
* Require a tag on resources

## Regions

All of our resources are created in the Germany West Central region, if possible.

## Entra App Registration (GitHub Workflow)

* created in the _home_ tenant
* created a "Client Secret" for use in GitHub workflows
* assigned "Container Apps Contributor" role via IAM for all container apps (note you have to explicitly start typing the name in the "Select members" filter!)

## Container Apps

* System-assigned managed identity
* Role "Storage Blob Data Contributor" at storage account
* Environment variable BLOB_STORAGE_URI set (e.g. https://cageeditordevst.blob.core.windows.net/)

## Entra App Registration (Backend)

* Supported account types: Single tenant only
* Expose an API - Scope: e.g. user_impersonation
* Expose an API - Authorized client applications: Entra App Registration (Frontend), see below

## Entra App Registration (Frontend)

* Supported account types: Single tenant only
* Redirect URI: Single page application
* API permissions - Configured permissions: My APIs - Entra App Registration (Frontend) - delegated permissions - user_impersonation

## Functions App

* Operating System: Windows
* Stack: .NET 10 Isolated
* Networking - Enable public access

> Function Apps hosted on Windows seem to provide more logging options.

* Authentication - App Service authentication: Enabled
* Authentication - Restrict access: Require authentication
* Authentication - Unauthenticated requests: HTTP 302 Found redirect: recommended for websites
* Authentication - Token store: Enabled
* Authentication - Identity Provider: Microsoft
* Authentication - Identity Provider - Application (client) ID: from Entra App Registration (Frontend), see above (matches access token azd claim (= Authorized party (the party to which this token was issued)))
* Authentication - Identity Provider - Issuer URL: https://{tenantid}.ciamlogin.com/{tenantid}/v2.0 (matches access token iss claim (= Issuer (who created and signed this token)))
* Authentication - Identity Provider - Allowed token audiences: application ID from Entra App Registration (Backend), see above (matches access token aud claim (= Audience (who or what the token is intended for)))
* Authentication - Identity Provider - Additional checks - Client application requirement: Allow requests only from this application itself
* Authentication - Identity Provider - Additional checks - Identity requirement: Allow requests from any identity
* Authentication - Identity Provider - Additional checks - Tenant requirement: Allow requests only from the issuer tenant

> Changes to authentication settings in the Functions App can take a few minutes. Make sure to wait after saving them before verifying your changes.

* Monitoring - Diagnostic Settings: all categories, Send to Log Analytics workspace

## References

* [Define your naming convention](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming#choose-naming-components)
* [Define your tagging strategy](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-tagging)
* [Set up GitHub Actions with Azure CLI in Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/github-actions-cli?tabs=bash)
* [Manage revisions in Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/revisions-manage?tabs=bash)

### Azure Functions

* [Learn: Enable diagnostic logs for apps in Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/troubleshoot-diagnostic-logs)
* [Learn: Authentication and authorization in Azure App Service and Azure Functions](https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization)
* [Learn: Authentication scenarios and recommendations](https://learn.microsoft.com/en-us/azure/app-service/identity-scenarios)
* [Learn: Configure your App Service or Azure Functions app to use Microsoft Entra sign-in](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad?tabs=external-configuration)

### Microsoft Entra

* [Learn: Single-page application: Code configuration](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-app-configuration?tabs=javascript2)
* [Learn: Register an application in Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
* [GitHub: Vanilla JavaScript single-page application (SPA) using MSAL.js to authorize users for calling a protected web API on Microsoft Entra ID](https://github.com/Azure-Samples/ms-identity-javascript-tutorial/blob/main/3-Authorization-II/1-call-api/README.md)
* [GitHub: Microsoft Authentication Library for JavaScript (MSAL.js) for Browser-Based Single-Page Applications](https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/README.md)
