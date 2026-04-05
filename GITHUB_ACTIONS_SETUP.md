# GitHub Actions Setup

This repository now includes a GitHub Actions workflow at `.github/workflows/staffnex-api.yml`.

## What the workflow does

- Restores the solution
- Builds the .NET 8 API in `Release`
- Publishes the API output as a workflow artifact
- Deploys to Azure App Service on `main` or `master` only when Azure settings are configured

## Required GitHub setup for Azure deployment

### Repository variable

Create this repository variable in GitHub:

- `AZURE_WEBAPP_NAME`
  - Value example: `staffnex-api-prod`

### Repository secret

Create this repository secret in GitHub:

- `AZURE_WEBAPP_PUBLISH_PROFILE`
  - Value: paste the full publish profile downloaded from your Azure Web App

## Azure App Service steps

1. Create an Azure App Service for ASP.NET Core.
2. Open the Web App in Azure Portal.
3. Download the publish profile.
4. In GitHub repository settings, add:
   - variable `AZURE_WEBAPP_NAME`
   - secret `AZURE_WEBAPP_PUBLISH_PROFILE`
5. Push this repository to GitHub.
6. Push to `main` or run the workflow manually from Actions.

## Recommended production app settings

Set these in Azure App Service configuration instead of keeping production values in `appsettings.json`:

- `ConnectionStrings__DefaultConnection`
- `JwtSettings__Key`
- `JwtSettings__Issuer`
- `JwtSettings__Audience`
- `JwtSettings__AccessTokenExpirationMinutes`
- `JwtSettings__RefreshTokenExpirationDays`

## Notes

- If Azure variable or secret is missing, the workflow still builds successfully and skips deployment.
- Pull requests run build validation only.
- The deployment job uses the published output from `staffnex.Api`.