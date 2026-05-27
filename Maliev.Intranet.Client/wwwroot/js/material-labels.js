window.malievMaterialLabels = (function () {
    const printStyleId = "maliev-material-label-print-style";
    const printTargetClass = "material-print-label--printing";
    const printRootClass = "material-label-printing";
    const printCss = `
@media print {
    @page {
        size: 50mm 30mm;
        margin: 0;
    }

    html,
    body {
        width: 50mm !important;
        height: 30mm !important;
        margin: 0 !important;
        padding: 0 !important;
        background: #fff !important;
        overflow: hidden !important;
    }

    body * {
        visibility: hidden !important;
    }

    .material-print-label.${printTargetClass},
    .material-print-label.${printTargetClass} * {
        visibility: visible !important;
    }

    .material-print-label:not(.${printTargetClass}) {
        display: none !important;
    }
}`;

    function ensurePrintStyle() {
        let style = document.getElementById(printStyleId);

        if (!style) {
            style = document.createElement("style");
            style.id = printStyleId;
            document.head.appendChild(style);
        }

        style.textContent = printCss;
    }

    function clearPrintTargets() {
        document
            .querySelectorAll(`.material-print-label.${printTargetClass}`)
            .forEach((label) => label.classList.remove(printTargetClass));
    }

    function cleanup() {
        clearPrintTargets();
        document.documentElement.classList.remove(printRootClass);

        const style = document.getElementById(printStyleId);
        if (style) {
            style.remove();
        }
    }

    function print(labelElementId) {
        const label = document.getElementById(labelElementId);

        if (!label) {
            window.print();
            return;
        }

        cleanup();
        ensurePrintStyle();
        label.classList.add(printTargetClass);
        document.documentElement.classList.add(printRootClass);

        let cleaned = false;
        const cleanupOnce = function () {
            if (cleaned) {
                return;
            }

            cleaned = true;
            cleanup();
        };

        window.addEventListener("afterprint", cleanupOnce, { once: true });
        window.requestAnimationFrame(function () {
            window.requestAnimationFrame(function () {
                window.print();
                window.setTimeout(cleanupOnce, 30000);
            });
        });
    }

    return {
        print
    };
})();
