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

For this project, the currently used runtime keys are:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__AccessTokenMinutes`
- `Jwt__RefreshTokenDays`
- `SeedData__Enabled`
- `Cors__AllowedOrigins__0`
- `Cors__AllowedOrigins__1`

## Exact Azure App Service checklist

Use this exact setup in Azure Portal for production:

1. Create an App Service using the `.NET 8 (LTS)` runtime.
2. Create or attach the SQL Server database the API should use.
3. In Azure Portal, open `Configuration` and add these application settings:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ConnectionStrings__DefaultConnection=Server=<server>;Database=staffnexAttendanceDb;User Id=<user>;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=True`
  - `Jwt__Key=<strong-random-secret-at-least-32-characters>`
  - `Jwt__Issuer=staffnex`
  - `Jwt__Audience=staffnex-clients`
  - `Jwt__AccessTokenMinutes=60`
  - `Jwt__RefreshTokenDays=7`
  - `SeedData__Enabled=false`
  - `Cors__AllowedOrigins__0=https://your-frontend-domain.com`
  - `Cors__AllowedOrigins__1=https://www.your-frontend-domain.com`
4. Keep `ConnectionStrings__DefaultConnection` in `Application settings` unless you also change the code to read Azure connection-string providers differently.
5. Download the publish profile from the App Service `Overview` page.
6. In GitHub repository settings, add:
  - variable `AZURE_WEBAPP_NAME` with the exact Web App name
  - secret `AZURE_WEBAPP_PUBLISH_PROFILE` with the full publish profile XML
7. Push to `main` or run the Actions workflow manually.
8. After deployment, confirm login, database connectivity, and browser access from only the configured frontend origins.

## Notes

- If Azure variable or secret is missing, the workflow still builds successfully and skips deployment.
- Pull requests run build validation only.
- The deployment job uses the published output from `staffnex.Api`.
- Seed data is disabled by default outside development. Enable `SeedData__Enabled=true` only when you intentionally want demo users inserted.
- CORS is configuration-driven. In Azure App Service, add allowed browser origins with indexed keys like `Cors__AllowedOrigins__0`.