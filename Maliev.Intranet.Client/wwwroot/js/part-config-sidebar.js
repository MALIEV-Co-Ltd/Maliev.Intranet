(function () {
    window.malievPartConfigSidebar = window.malievPartConfigSidebar || {};

    window.malievPartConfigSidebar.scrollProcessIntoStart = function (row, processCode) {
        if (!row || !processCode) {
            return;
        }

        var cards = row.querySelectorAll("[data-process-code]");
        var target = null;
        for (var i = 0; i < cards.length; i++) {
            if (cards[i].getAttribute("data-process-code") === processCode) {
                target = cards[i];
                break;
            }
        }

        if (!target) {
            return;
        }

        var rowRect = row.getBoundingClientRect();
        var targetRect = target.getBoundingClientRect();
        var top = Math.max(0, row.scrollTop + targetRect.top - rowRect.top);
        var prefersReducedMotion = window.matchMedia
            && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

        if (typeof row.scrollTo === "function") {
            row.scrollTo({
                top: top,
                behavior: prefersReducedMotion ? "auto" : "smooth"
            });
            return;
        }

        row.scrollTop = top;
    };
})();
