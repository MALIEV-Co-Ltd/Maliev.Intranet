window.qrScanner = null;

window.requestCameraPermission = async function () {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        return false;
    }
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        // Stop the stream immediately - we only needed to confirm permission
        stream.getTracks().forEach(track => track.stop());
        return true;
    } catch (err) {
        console.warn('Camera permission denied or unavailable:', err);
        return false;
    }
};

window.startQrScanner = function (elementId, dotNetRef) {
    if (window.qrScanner) {
        window.qrScanner.stop().catch(() => {});
        window.qrScanner = null;
    }

    const element = document.getElementById(elementId);
    if (!element) {
        console.error('QR scanner element not found:', elementId);
        return;
    }

    if (typeof Html5Qrcode === 'undefined') {
        console.error('Html5Qrcode library not loaded');
        element.innerHTML = '<div style="padding: 20px; text-align: center; color: #f44336;">QR Scanner library not loaded. Please include html5-qrcode library.</div>';
        return;
    }

    element.innerHTML = '';

    const config = {
        fps: 10,
        qrbox: { width: 250, height: 250 },
        aspectRatio: 1.0
    };

    window.qrScanner = new Html5Qrcode(elementId);

    window.qrScanner.start(
        { facingMode: "environment" },
        config,
        (decodedText) => {
            window.qrScanner.stop().then(() => {
                dotNetRef.invokeMethodAsync('OnQrCodeScanned', decodedText);
            }).catch((err) => {
                console.error('Error stopping scanner:', err);
                dotNetRef.invokeMethodAsync('OnQrCodeScanned', decodedText);
            });
        },
        (errorMessage) => {
            // Ignore scan errors (common during continuous scanning)
        }
    ).catch((err) => {
        console.error('Error starting QR scanner:', err);
        element.innerHTML = '<div style="padding: 20px; text-align: center; color: #f44336;"><p>Camera access denied or unavailable.</p><p>Please ensure camera permissions are granted.</p></div>';
    });
};

window.stopQrScanner = function () {
    if (window.qrScanner) {
        return window.qrScanner.stop().then(() => {
            window.qrScanner.clear();
            window.qrScanner = null;
        }).catch((err) => {
            console.error('Error stopping scanner:', err);
            window.qrScanner = null;
        });
    }
    return Promise.resolve();
};
