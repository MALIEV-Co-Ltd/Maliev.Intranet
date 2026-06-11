import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { chromium } from 'playwright';

const baseUrl = process.env.MALIEV_INTRANET_E2E_BASE_URL ?? 'http://localhost:5071';

const storageStatePath = process.env.MALIEV_INTRANET_E2E_STORAGE_STATE;

test('Project New accepts STL upload through the hidden file input', async t => {
    if (!storageStatePath) {
        t.skip('Set MALIEV_INTRANET_E2E_STORAGE_STATE to an authenticated Playwright storage-state JSON file.');
        return;
    }

    const { dir, filePath: stlPath } = await writeTinyAsciiStlAsync();
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ storageState: storageStatePath });
    const page = await context.newPage();

    try {
        const sessionId = randomUUID();
        await page.goto(`${baseUrl}/sales/projects/new?session=${sessionId}`, {
            waitUntil: 'domcontentloaded'
        });

        if (/\/auth\/login/i.test(page.url())) {
            t.skip('Authenticated storage state was not accepted; refresh MALIEV_INTRANET_E2E_STORAGE_STATE.');
            return;
        }

        const fileInput = page.locator('#project-new-file-upload input[type=file]');
        await expectVisibleInDomAsync(fileInput);
        await fileInput.setInputFiles(stlPath);

        const partRow = page.getByText('codex-upload-probe.stl', { exact: false });
        await partRow.waitFor({ state: 'visible', timeout: 15_000 });

        const rowText = await page
            .locator('.plp-list, .plr-wrapper')
            .filter({ hasText: 'codex-upload-probe.stl' })
            .first()
            .innerText({ timeout: 15_000 });

        assert.match(rowText, /codex-upload-probe\.stl/i);
        assert.match(rowText, /\b1\b/);
    } finally {
        await browser.close();
        await fs.rm(dir, { force: true, recursive: true });
    }
});

async function expectVisibleInDomAsync(locator) {
    await locator.waitFor({ state: 'attached', timeout: 15_000 });
    const count = await locator.count();
    assert.equal(count, 1, 'Project New should expose exactly one hidden file input for E2E upload automation.');
}

async function writeTinyAsciiStlAsync() {
    const dir = await fs.mkdtemp(path.join(os.tmpdir(), 'maliev-project-new-upload-'));
    const filePath = path.join(dir, 'codex-upload-probe.stl');
    await fs.writeFile(filePath, `solid codex_upload_probe
  facet normal 0 0 1
    outer loop
      vertex 0 0 0
      vertex 10 0 0
      vertex 0 10 0
    endloop
  endfacet
  facet normal 0 1 0
    outer loop
      vertex 0 0 0
      vertex 0 0 10
      vertex 10 0 0
    endloop
  endfacet
  facet normal 1 0 0
    outer loop
      vertex 0 0 0
      vertex 0 10 0
      vertex 0 0 10
    endloop
  endfacet
  facet normal 1 1 1
    outer loop
      vertex 10 0 0
      vertex 0 0 10
      vertex 0 10 0
    endloop
  endfacet
endsolid codex_upload_probe
`, 'utf8');
    return { dir, filePath };
}
