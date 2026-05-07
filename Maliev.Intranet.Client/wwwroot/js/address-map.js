window.malievAddressMap = (function () {
    const maps = new Map();
    const leafletCss = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css";
    const leafletJs = "https://unpkg.com/leaflet@1.9.4/dist/leaflet.js";
    let leafletPromise = null;

    function loadLeaflet() {
        if (window.L) {
            return Promise.resolve(window.L);
        }

        if (leafletPromise) {
            return leafletPromise;
        }

        leafletPromise = new Promise(function (resolve, reject) {
            if (!document.querySelector(`link[href="${leafletCss}"]`)) {
                const link = document.createElement("link");
                link.rel = "stylesheet";
                link.href = leafletCss;
                document.head.appendChild(link);
            }

            const script = document.createElement("script");
            script.src = leafletJs;
            script.async = true;
            script.onload = function () { resolve(window.L); };
            script.onerror = reject;
            document.head.appendChild(script);
        });

        return leafletPromise;
    }

    function initialize(elementId) {
        const element = document.getElementById(elementId);
        if (!element || maps.has(elementId)) {
            return;
        }

        loadLeaflet().then(function (L) {
            if (!document.getElementById(elementId) || maps.has(elementId)) {
                return;
            }

            const map = L.map(elementId, {
                zoomControl: true,
                attributionControl: true
            }).setView([13.7563, 100.5018], 6);

            L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
                maxZoom: 19,
                attribution: "&copy; OpenStreetMap contributors"
            }).addTo(map);

            maps.set(elementId, { map: map, marker: null });
            setTimeout(function () { map.invalidateSize(); }, 0);
        });
    }

    function update(elementId, latitude, longitude, label) {
        loadLeaflet().then(function (L) {
            const state = maps.get(elementId);
            if (!state) {
                initialize(elementId);
                setTimeout(function () { update(elementId, latitude, longitude, label); }, 200);
                return;
            }

            const position = [latitude, longitude];
            state.map.setView(position, 15);

            if (!state.marker) {
                state.marker = L.marker(position).addTo(state.map);
            } else {
                state.marker.setLatLng(position);
            }

            if (label) {
                state.marker.bindPopup(label);
            }

            state.map.invalidateSize();
        });
    }

    function dispose(elementId) {
        const state = maps.get(elementId);
        if (!state) {
            return;
        }

        state.map.remove();
        maps.delete(elementId);
    }

    return {
        initialize: initialize,
        update: update,
        dispose: dispose
    };
})();
