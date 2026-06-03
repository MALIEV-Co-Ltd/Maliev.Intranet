import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

function loadUploadContext(input) {
    const inputs = Array.isArray(input) ? input : [input];
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
        window: {}
    };

    const source = fs.readFileSync('Maliev.Intranet.Client/wwwroot/js/uploadWithProgress.js', 'utf8');
    vm.runInNewContext(source, context);
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
