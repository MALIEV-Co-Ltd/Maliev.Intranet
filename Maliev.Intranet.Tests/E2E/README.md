# Project New E2E Tests

These tests use Playwright so browser file uploads can be automated with
`locator.setInputFiles(...)`. This covers the hidden MudBlazor file input that
the Codex in-app browser automation API can inspect but cannot populate.

## Setup

Install dependencies and the Chromium browser once:

```powershell
npm ci
npm run e2e:install-browsers
```

Provide an authenticated Playwright storage-state file for the local Intranet
BFF. The test intentionally skips when this file is not supplied, so regular
build/test runs do not require Google OAuth.

```powershell
$env:MALIEV_INTRANET_E2E_STORAGE_STATE = "B:\secure\intranet-storage-state.json"
$env:MALIEV_INTRANET_E2E_BASE_URL = "http://localhost:5071"
npm run test:e2e:project-new-upload
```

The storage state should be created from a signed-in browser context for the
same base URL. Keep it out of git; it contains authentication cookies.
