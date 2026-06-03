import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

function loadUploadContext(input) {
    const inputs = Array.isArray(input) ? input : [input];
    const objectUrls = [];
    const revokedObjectUrls = [];
    const container = {
        querySelector(selector) {
            return selector === 'input[type=file]' ? inputs[0] : null;
        },
        querySelectorAll(selector) {
            return selector === 'input[type=file]' ? inputs : [];
        }
    };

    const context = {
        Blob,
        console,
        document: {
            getElementById(id) {
                return id === 'project-new-file-upload' ? container : null;
            }
        },
        Event: class {
            constructor(type) {
                this.type = type;
            }
        },
        URL: {
            createObjectURL(file) {
                const url = `blob:project/${file.name}/${objectUrls.length}`;
                objectUrls.push({ file, url });
                return url;
            },
            revokeObjectURL(url) {
                revokedObjectUrls.push(url);
            }
        },
        setTimeout: () => 1,
        clearTimeout: () => {},
        window: {}
    };

    const source = fs.readFileSync('Maliev.Intranet.Client/wwwroot/js/uploadWithProgress.js', 'utf8');
    vm.runInNewContext(source, context);
    context.objectUrls = objectUrls;
    context.revokedObjectUrls = revokedObjectUrls;
    return context;
}

test('captureFiles clears the hidden input while retaining browser files for upload', async () => {
    const selectedFile = new File(['solid'], 'bracket.stl', { type: 'model/stl' });
    const input = {
        files: [selectedFile],
        value: 'C:\\fakepath\\bracket.stl'
    };
    const context = loadUploadContext(input);
    const uploads = context.window.projectNewUploads;

    uploads.captureFiles('project-new-file-upload', [{
        clientUploadId: 'upload-1',
        index: 0,
        fileName: 'bracket.stl',
        fileSize: selectedFile.size
    }]);

    assert.equal(input.value, '');

    let sentBody;
    context.XMLHttpRequest = class {
        upload = {};
        status = 200;
        responseText = '';

        open(method, url) {
            this.method = method;
            this.url = url;
        }

        setRequestHeader() {}

        send(body) {
            sentBody = body;
            this.onload();
        }
    };

    const result = await uploads.uploadFile(
        'upload-1',
        'https://storage.example/session',
        '/api/v1/uploads/resumable/upload-1',
        'model/stl',
        selectedFile.size,
        null);

    assert.equal(result.status, 200);
    assert.equal(sentBody, selectedFile);
});

test('getObjectUrl returns a reusable browser file object URL and revokes it on clear', () => {
    const selectedFile = new File(['solid'], 'local-viewer.stl', { type: 'model/stl' });
    const input = {
        files: [selectedFile],
        value: 'C:\\fakepath\\local-viewer.stl'
    };
    const context = loadUploadContext(input);
    const uploads = context.window.projectNewUploads;

    uploads.captureFiles('project-new-file-upload', [{
        clientUploadId: 'upload-local-viewer',
        index: 0,
        fileName: 'local-viewer.stl',
        fileSize: selectedFile.size
    }]);

    const firstUrl = uploads.getObjectUrl('upload-local-viewer');
    const secondUrl = uploads.getObjectUrl('upload-local-viewer');

    assert.equal(firstUrl, 'blob:project/local-viewer.stl/0');
    assert.equal(secondUrl, firstUrl);
    assert.equal(context.objectUrls.length, 1);
    assert.equal(context.objectUrls[0].file, selectedFile);

    uploads.clearFile('upload-local-viewer');

    assert.deepEqual(context.revokedObjectUrls, [firstUrl]);
    assert.equal(uploads.getObjectUrl('upload-local-viewer'), null);
});

test('captureFiles uses the input containing the current selection when stale file inputs remain', async () => {
    const staleInput = {
        files: [],
        value: ''
    };
    const selectedFile = new File(['solid'], 'Aerosport+(modified)+Outlet+Manifold.stl', { type: 'model/stl' });
    const currentInput = {
        files: [selectedFile],
        value: 'C:\\fakepath\\Aerosport+(modified)+Outlet+Manifold.stl'
    };
    const context = loadUploadContext([staleInput, currentInput]);
    const uploads = context.window.projectNewUploads;

    uploads.captureFiles('project-new-file-upload', [{
        clientUploadId: 'upload-2',
        index: 0,
        fileName: 'Aerosport+(modified)+Outlet+Manifold.stl',
        fileSize: selectedFile.size
    }]);

    assert.equal(currentInput.value, '');

    let sentBody;
    context.XMLHttpRequest = class {
        upload = {};
        status = 200;
        responseText = '';

        open(method, url) {
            this.method = method;
            this.url = url;
        }

        setRequestHeader() {}

        send(body) {
            sentBody = body;
            this.onload();
        }
    };

    const result = await uploads.uploadFile(
        'upload-2',
        'https://storage.example/session',
        '/api/v1/uploads/resumable/upload-2',
        'model/stl',
        selectedFile.size,
        null);

    assert.equal(result.status, 200);
    assert.equal(sentBody, selectedFile);
});
