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
