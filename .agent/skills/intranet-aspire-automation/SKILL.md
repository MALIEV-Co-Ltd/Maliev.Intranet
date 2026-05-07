---
name: intranet-aspire-automation
description: Use when validating Maliev.Intranet through Aspire, seeding or using the local automation login, browser-testing authenticated Intranet pages, or troubleshooting why the automation principal must stay separate from the IAM system principal.
---

# Intranet Aspire Automation

## Purpose

Use this skill when an agent needs authenticated browser access to `Maliev.Intranet` through the local Aspire apphost. It captures the safe local automation identity and the checks needed to keep first real Google Workspace owner bootstrap working.

## Automation Principal

- Username: `aspire-automation@debug.com`
- Password: read from local Aspire configuration only: `AspireTestAdmin:Password` or `AspireTestAdmin__Password`.
- Never commit the password, screenshots containing it, user-secrets output, cookies, tokens, or generated browser storage.
- The principal is synthetic and Aspire-local. It is tagged by IAM with `LinkedService = AspireTestAdminSeeder` so it can be excluded from first-real-user Platform Owner bootstrap checks.

Do not use `system@maliev.com` for browser automation. That address is the IAM system/service principal, not a human employee login. Reusing it would blur audit trails, conflict with the existing system principal email, and risk making service-owned roles look like real human Platform Owner bootstrap state.

## Source Map

- Aspire defaults and guardrails: `Maliev.Aspire/Maliev.Aspire.DatabaseSeeder/Seeding/Services/Shared/AspireTestAdminSeedOptions.cs`
- IAM seed: `Maliev.Aspire/Maliev.Aspire.DatabaseSeeder/Seeding/Services/IAMService/IAMDatabaseSeeder.cs`
- Employee/password seed: `Maliev.Aspire/Maliev.Aspire.DatabaseSeeder/Seeding/Services/EmployeeService/EmployeeDatabaseSeeder.cs`
- Auth password-login behavior: `Maliev.AuthService`
- Intranet BFF login page and form action: `Maliev.Intranet.Bff\Controllers\LoginPageController.cs`

## Workflow

1. Confirm Aspire is running and find Intranet:
   ```powershell
   aspire describe IntranetBff --apphost ..\Maliev.Aspire\Maliev.Aspire.AppHost\Maliev.Aspire.AppHost.csproj --non-interactive --nologo
   ```

2. If the automation login was changed or the database was reset, rerun the IAM and Employee seeders from Aspire. The deterministic principal and employee IDs let the seeders update the synthetic account by ID on rerun.

3. Navigate to `http://localhost:5071/login?returnUrl=%2F`.

4. Sign in with `aspire-automation@debug.com` and the local-only password from configuration.

5. Validate authenticated pages that match the task. Common smoke routes:
   - `/`
   - `/customers`
   - `/iam`
   - `/admin/system-health`
   - `/sales/projects/new`

6. If login fails:
   - Check that `AspireTestAdmin:Enabled` is true.
   - Check that the password exists in local configuration, not source control.
   - Rerun the Employee and IAM seeders.
   - Inspect AuthService logs for employee validation or token issuance errors.
   - Inspect IAMService only for role/permission resolution issues after AuthService accepts the credential.

## Safety Checks

- The first real `@maliev.com` Google SSO user must still be able to receive Platform Owner.
- Bootstrap queries must exclude the synthetic linked service: `p.LinkedService != AspireTestAdminSeeder`.
- The automation email must not be `system@maliev.com`.
- Keep the automation email outside `maliev.com` unless a test explicitly requires domain behavior.
- Local browser tests can use the automation login; production and public docs must not publish the password.
