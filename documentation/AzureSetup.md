# Azure Setup

## Overview

* Microsoft Entra External Id tenant (+ 2 app registrations)
* Function App (+ storage account, application insights)

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
* Authentication - Identity Provider - Application (client) ID: from Entra App Registration (Frontend), see above
* Authentication - Identity Provider - Issuer URL: https://{tenantid}.ciamlogin.com/{tenantid}/v2.0
* Authentication - Identity Provider - Allowed token audiences: application ID from Entra App Registration (Backend), see above (matches access token aud claim)
* Authentication - Identity Provider - Additional checks - Client application requirement: Allow requests only from this application itself
* Authentication - Identity Provider - Additional checks - Identity requirement: Allow requests from any identity
* Authentication - Identity Provider - Additional checks - Tenant requirement: Allow requests only from the issuer tenant

> Changes to authentication settings in the Functions App can take a few minutes. Make sure to wait after saving them before verifying your changes.

* Monitoring - Diagnostic Settings: all categories, Send to Log Analytics workspace

## References
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
