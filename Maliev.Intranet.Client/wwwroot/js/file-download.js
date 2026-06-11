// Maliev.Intranet.Client/wwwroot/js/file-download.js
// Browser file download helper. Signed storage URLs serve objects under hashed
// names — downloading them directly produces a random-looking filename without
// an extension. This fetches the bytes and re-triggers the download under the
// caller-provided filename instead.
window.malievFiles = (function () {
    async function downloadFromUrl(url, fileName) {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Download failed: ${response.status}`);
        }
        const blob = await response.blob();
        const objectUrl = URL.createObjectURL(blob);
        try {
            const anchor = document.createElement('a');
            anchor.href = objectUrl;
            anchor.download = fileName || 'download';
            anchor.rel = 'noopener';
            document.body.appendChild(anchor);
            anchor.click();
            anchor.remove();
        } finally {
            // Give the browser a moment to start the download before revoking.
            setTimeout(() => URL.revokeObjectURL(objectUrl), 30000);
        }
    }

    return { downloadFromUrl };
})();

// Client-side mesh rescaling. Scales every vertex coordinate by a constant
// factor (unit conversion / resize) and registers the result with the shared
// upload pipeline so it can replace the original file. Mesh formats only —
// B-Rep formats (STEP/IGES) cannot be scaled from tessellated data.
window.malievPartScaling = (function () {
    function isBinaryStl(bytes) {
        if (bytes.length < 84) return false;
        const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
        const triangleCount = view.getUint32(80, true);
        return 84 + triangleCount * 50 === bytes.length;
    }

    function scaleBinaryStl(bytes, factor) {
        const out = new Uint8Array(bytes); // copy
        const view = new DataView(out.buffer, out.byteOffset, out.byteLength);
        const triangleCount = view.getUint32(80, true);
        let offset = 84;
        for (let triangle = 0; triangle < triangleCount; triangle += 1) {
            offset += 12; // normal stays unit-length
            for (let component = 0; component < 9; component += 1) {
                view.setFloat32(offset, view.getFloat32(offset, true) * factor, true);
                offset += 4;
            }
            offset += 2; // attribute byte count
        }
        return out;
    }

    function scaleNumberTriplet(line, prefix, factor) {
        const parts = line.trim().split(/\s+/);
        const scaled = parts.slice(1, 4).map(value => String(Number(value) * factor));
        return `${prefix} ${scaled.join(' ')}${parts.length > 4 ? ' ' + parts.slice(4).join(' ') : ''}`;
    }

    function scaleAsciiStl(text, factor) {
        return text.split(/\r?\n/).map(line =>
            /^\s*vertex\s/i.test(line)
                ? line.replace(/^(\s*)vertex\s+(\S+)\s+(\S+)\s+(\S+)/i, (_, indent, x, y, z) =>
                    `${indent}vertex ${Number(x) * factor} ${Number(y) * factor} ${Number(z) * factor}`)
                : line
        ).join('\n');
    }

    function scaleObj(text, factor) {
        return text.split(/\r?\n/).map(line =>
            /^\s*v\s/.test(line) ? scaleNumberTriplet(line, 'v', factor) : line
        ).join('\n');
    }

    async function scaleAndRegister(url, fileName, factor, clientUploadId) {
        const response = await fetch(url);
        if (!response.ok) throw new Error(`Download failed: ${response.status}`);
        const bytes = new Uint8Array(await response.arrayBuffer());
        const lowerName = String(fileName || '').toLowerCase();

        let outBytes;
        if (lowerName.endsWith('.stl')) {
            outBytes = isBinaryStl(bytes)
                ? scaleBinaryStl(bytes, factor)
                : new TextEncoder().encode(scaleAsciiStl(new TextDecoder().decode(bytes), factor));
        } else if (lowerName.endsWith('.obj')) {
            outBytes = new TextEncoder().encode(scaleObj(new TextDecoder().decode(bytes), factor));
        } else {
            throw new Error(`Rescaling is not supported for ${lowerName.split('.').pop()} files.`);
        }

        const file = new File([outBytes], fileName, { type: 'application/octet-stream' });
        window.projectNewUploads.registerGeneratedFile(clientUploadId, file);
        return { fileName, size: file.size };
    }

    return { scaleAndRegister };
})();
