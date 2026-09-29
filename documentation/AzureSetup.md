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
* Role "Storage Table Data Contributor" at storage account
* Environment variable CAGE_EDITOR_FRONTEND_URL set (without trailing slash)
* Environment variable BLOB_STORAGE_URI set (e.g.https://cagegamedevst.blob.core.windows.net/)
* Environment variable TABLE_STORAGE_URI set (e.g. https://cageeditordevst.table.core.windows.net/)

## Entra App Registration (Backend)

* Supported account types: Single tenant only
* Expose an API - Scope: e.g. user_impersonation
* Expose an API - Authorized client applications: Entra App Registration (Frontend), see below

## Entra App Registration (Frontend)

* Supported account types: Single tenant only
* Redirect URI: Single page application (both localhost and Static Web App URL)
* API permissions - Configured permissions: My APIs - Entra App Registration (Frontend) - delegated permissions - user_impersonation

## Static Web App

* Deployment details - Source: Other
* Deployment configuration - Deployment authorization policy: Deployment token

## References
### General

* [Define your naming convention](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming#choose-naming-components)
* [Define your tagging strategy](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-tagging)

### Azure Container Apps

* [Set up GitHub Actions with Azure CLI in Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/github-actions-cli?tabs=bash)
* [Manage revisions in Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/revisions-manage?tabs=bash)

### Azure Static Web Apps

* [Deploy a static web app with Azure Static Web Apps CLI](https://learn.microsoft.com/en-us/azure/static-web-apps/static-web-apps-cli-deploy)
* [Deploy your web app to Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/deploy-web-framework?tabs=pwsh&pivots=angular)

### Microsoft Entra

* [Learn: Single-page application: Code configuration](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-app-configuration?tabs=javascript2)
* [Learn: Register an application in Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
* [GitHub: Vanilla JavaScript single-page application (SPA) using MSAL.js to authorize users for calling a protected web API on Microsoft Entra ID](https://github.com/Azure-Samples/ms-identity-javascript-tutorial/blob/main/3-Authorization-II/1-call-api/README.md)
* [GitHub: Microsoft Authentication Library for JavaScript (MSAL.js) for Browser-Based Single-Page Applications](https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/README.md)
