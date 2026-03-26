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
