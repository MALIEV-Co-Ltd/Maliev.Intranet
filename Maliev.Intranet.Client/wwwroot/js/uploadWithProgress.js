/**
 * Wires a visual drop zone element to the hidden MudFileUpload <input type="file">.
 * Without this, files dropped on the visual zone don't reach the Blazor FilesChanged callback.
 * @param {string} dropZoneId - id of the visual drop target element
 * @param {string} inputId    - id of the hidden file input rendered by MudFileUpload
 */
/**
 * Wires a visual drop zone element to the hidden MudFileUpload <input type="file">.
 * Without this, files dropped on the visual zone don't reach the Blazor FilesChanged callback.
 * @param {string} dropZoneId - id attribute of the visual drop target element
 */
window.initFileDropZone = function (dropZoneId) {
    const zone = document.getElementById(dropZoneId);
    if (!zone) return;

    zone.addEventListener('drop', function (e) {
        e.preventDefault();
        const files = e.dataTransfer?.files;
        if (!files || files.length === 0) return;

        // MudFileUpload renders a hidden input; find it in the nearest ancestor container
        const container = zone.closest('.mud-file-upload') || zone.parentElement?.parentElement;
        const input = container?.querySelector('input[type=file]');
        if (!input) return;

        // Relay dropped files to the hidden input via DataTransfer API
        const dt = new DataTransfer();
        for (const f of files) dt.items.add(f);
        input.files = dt.files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }, false);
};

window.uploadWithProgress = function (url, fileBytes, fileName, dotNetHelper) {
    return new Promise(function (resolve, reject) {
        var blob = new Blob([fileBytes]);
        var formData = new FormData();
        formData.append('file', blob, fileName);

        var xhr = new XMLHttpRequest();
        xhr.open('POST', url);
        xhr.withCredentials = true;

        xhr.upload.onprogress = function (e) {
            if (e.lengthComputable) {
                var pct = Math.round((e.loaded / e.total) * 100);
                dotNetHelper.invokeMethodAsync('OnUploadProgress', pct);
            }
        };

        xhr.onload = function () {
            resolve({ status: xhr.status, body: xhr.responseText });
        };

        xhr.onerror = function () {
            reject('Network error during upload');
        };

        xhr.send(formData);
    });
};

/**
 * Uploads multiple files in a single multipart/form-data request with aggregate progress.
 * @param {string} url - The batch upload endpoint URL.
 * @param {Array<{bytes: Uint8Array, name: string}>} files - Array of file objects.
 * @param {object} dotNetHelper - .NET interop reference for progress callbacks.
 * @returns {Promise<{status: number, body: string}>}
 */
window.uploadBatchWithProgress = function (url, files, dotNetHelper) {
    return new Promise(function (resolve, reject) {
        var formData = new FormData();
        for (var i = 0; i < files.length; i++) {
            var blob = new Blob([files[i].bytes]);
            formData.append('files', blob, files[i].name);
        }

        var xhr = new XMLHttpRequest();
        xhr.open('POST', url);
        xhr.withCredentials = true;

        xhr.upload.onprogress = function (e) {
            if (e.lengthComputable) {
                var pct = Math.round((e.loaded / e.total) * 100);
                dotNetHelper.invokeMethodAsync('OnUploadProgress', pct);
            }
        };

        xhr.onload = function () {
            resolve({ status: xhr.status, body: xhr.responseText });
        };

        xhr.onerror = function () {
            reject('Network error during batch upload');
        };

        xhr.send(formData);
    });
};

window.projectNewUploads = (function () {
    const filesByClientId = new Map();
    const objectUrlsByClientId = new Map();
    const clearTimersByClientId = new Map();
    const fileRetentionMs = 10 * 60 * 1000;

    function findUploadInput(containerId, mappings) {
        const container = document.getElementById(containerId);
        if (!container) return null;
        const inputs = Array.from(container.querySelectorAll('input[type=file]'));
        if (inputs.length === 0) return null;

        const expectedMappings = mappings || [];
        const inputWithExpectedFile = inputs.find(function (input) {
            if (!input.files || input.files.length === 0) return false;

            return expectedMappings.some(function (mapping) {
                const expectedName = mapping.fileName || '';
                const expectedSize = Number(mapping.fileSize || 0);
                const indexedFile = input.files[mapping.index];

                return (indexedFile && indexedFile.name === expectedName && indexedFile.size === expectedSize)
                    || Boolean(findMatchingFile(input.files, expectedName, expectedSize));
            });
        });

        return inputWithExpectedFile
            || inputs.find(input => input.files && input.files.length > 0)
            || inputs[inputs.length - 1];
    }

    function findMatchingFile(files, expectedName, expectedSize) {
        if (!files) return null;

        for (const file of files) {
            if (file.name === expectedName && file.size === expectedSize) {
                return file;
            }
        }

        return null;
    }

    function captureFiles(containerId, mappings) {
        const input = findUploadInput(containerId, mappings);
        if (!input || !input.files) return;

        for (const mapping of mappings || []) {
            const expectedName = mapping.fileName || '';
            const expectedSize = Number(mapping.fileSize || 0);
            let file = input.files[mapping.index];

            if (!file || file.name !== expectedName || file.size !== expectedSize) {
                file = findMatchingFile(input.files, expectedName, expectedSize);
            }

            if (file) {
                const existingTimer = clearTimersByClientId.get(mapping.clientUploadId);
                if (existingTimer) clearTimeout(existingTimer);
                clearTimersByClientId.delete(mapping.clientUploadId);
                filesByClientId.set(mapping.clientUploadId, file);
            }
        }

        try {
            input.value = '';
        } catch (_) {
            // Some browser file inputs can refuse programmatic clearing.
        }
    }

    function sendXhr(url, file, contentType, includeCredentials, dotNetHelper) {
        return new Promise(function (resolve, reject) {
            const xhr = new XMLHttpRequest();
            xhr.open('PUT', url);
            xhr.withCredentials = includeCredentials;
            xhr.setRequestHeader('Content-Type', contentType || file.type || 'application/octet-stream');
            xhr.setRequestHeader('Content-Range', `bytes 0-${file.size - 1}/${file.size}`);

            xhr.upload.onprogress = function (e) {
                if (e.lengthComputable && dotNetHelper) {
                    const pct = Math.round((e.loaded / e.total) * 100);
                    dotNetHelper.invokeMethodAsync('OnUploadProgress', pct);
                }
            };

            xhr.onload = function () {
                resolve({ status: xhr.status, body: xhr.responseText || '' });
            };

            xhr.onerror = function () {
                reject({ status: xhr.status || 0, body: xhr.responseText || 'Network error during upload' });
            };

            xhr.send(file);
        });
    }

    async function uploadFile(clientUploadId, sessionUri, fallbackUrl, contentType, fileSize, dotNetHelper) {
        const file = filesByClientId.get(clientUploadId);
        if (!file) {
            return { status: 0, body: 'Selected browser file was not found.' };
        }

        const expectedFileSize = Number(fileSize || 0);
        if (expectedFileSize > 0 && file.size !== expectedFileSize) {
            return {
                status: 0,
                body: `Selected browser file size (${file.size}) does not match the initiated upload size (${expectedFileSize}).`
            };
        }

        const resolvedContentType = contentType || file.type || 'application/octet-stream';

        try {
            return await sendXhr(fallbackUrl, file, resolvedContentType, true, dotNetHelper);
        } catch {
            // Fall back to the direct GCS session only if the same-origin stream proxy is unavailable.
        }

        return await sendXhr(sessionUri, file, resolvedContentType, false, dotNetHelper);
    }

    function clearFile(clientUploadId) {
        const existingTimer = clearTimersByClientId.get(clientUploadId);
        if (existingTimer) clearTimeout(existingTimer);
        clearTimersByClientId.delete(clientUploadId);
        const objectUrl = objectUrlsByClientId.get(clientUploadId);
        if (objectUrl && typeof URL !== 'undefined' && typeof URL.revokeObjectURL === 'function') {
            URL.revokeObjectURL(objectUrl);
        }
        objectUrlsByClientId.delete(clientUploadId);
        filesByClientId.delete(clientUploadId);
    }

    function scheduleClearFile(clientUploadId, delayMs) {
        if (!filesByClientId.has(clientUploadId)) return;

        const existingTimer = clearTimersByClientId.get(clientUploadId);
        if (existingTimer) clearTimeout(existingTimer);

        const timer = setTimeout(function () {
            clearFile(clientUploadId);
        }, Number(delayMs) > 0 ? Number(delayMs) : fileRetentionMs);
        clearTimersByClientId.set(clientUploadId, timer);
    }

    async function getFileBytes(clientUploadId) {
        const file = filesByClientId.get(clientUploadId);
        if (!file || typeof file.arrayBuffer !== 'function') return null;

        return new Uint8Array(await file.arrayBuffer());
    }

    function getObjectUrl(clientUploadId) {
        const file = filesByClientId.get(clientUploadId);
        if (!file || typeof URL === 'undefined' || typeof URL.createObjectURL !== 'function') return null;

        let objectUrl = objectUrlsByClientId.get(clientUploadId);
        if (!objectUrl) {
            objectUrl = URL.createObjectURL(file);
            objectUrlsByClientId.set(clientUploadId, objectUrl);
        }

        return objectUrl;
    }

    function registerGeneratedFile(clientUploadId, file) {
        if (!clientUploadId || !file) return false;
        const existingTimer = clearTimersByClientId.get(clientUploadId);
        if (existingTimer) clearTimeout(existingTimer);
        clearTimersByClientId.delete(clientUploadId);
        filesByClientId.set(clientUploadId, file);
        return true;
    }

    return {
        captureFiles,
        uploadFile,
        clearFile,
        scheduleClearFile,
        getFileBytes,
        getObjectUrl,
        registerGeneratedFile
    };
})();
