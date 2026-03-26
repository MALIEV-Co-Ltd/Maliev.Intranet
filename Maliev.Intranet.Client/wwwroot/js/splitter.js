(function () {
    let _container = null, _bar = null;
    let _dragging = false, _startX = 0, _startPct = 60;

    function onMouseDown(e) {
        e.preventDefault();
        _dragging = true;
        _startX = e.clientX;
        const leftW = _container.querySelector('.detail-left').getBoundingClientRect().width;
        _startPct = leftW / _container.getBoundingClientRect().width * 100;
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    }

    function onMouseMove(e) {
        if (!_dragging) return;
        const dx = e.clientX - _startX;
        const newPct = Math.min(80, Math.max(20, _startPct + dx / _container.getBoundingClientRect().width * 100));
        _container.style.setProperty('--split-left', newPct + '%');
    }

    function onMouseUp() {
        _dragging = false;
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
    }

    function onTouchStart(e) {
        _dragging = true;
        _startX = e.touches[0].clientX;
        const leftW = _container.querySelector('.detail-left').getBoundingClientRect().width;
        _startPct = leftW / _container.getBoundingClientRect().width * 100;
    }

    function onTouchMove(e) {
        if (!_dragging) return;
        e.preventDefault();
        const dx = e.touches[0].clientX - _startX;
        const newPct = Math.min(80, Math.max(20, _startPct + dx / _container.getBoundingClientRect().width * 100));
        _container.style.setProperty('--split-left', newPct + '%');
    }

    function onTouchEnd() { _dragging = false; }

    window.splitter = {
        init: function (containerId, barId) {
            _container = document.getElementById(containerId);
            _bar = document.getElementById(barId);
            if (!_bar || !_container) return;
            _bar.addEventListener('mousedown', onMouseDown);
            _bar.addEventListener('touchstart', onTouchStart, { passive: true });
            _bar.addEventListener('touchmove', onTouchMove, { passive: false });
            _bar.addEventListener('touchend', onTouchEnd);
        },
        dispose: function () {
            if (_bar) {
                _bar.removeEventListener('mousedown', onMouseDown);
                _bar.removeEventListener('touchstart', onTouchStart);
                _bar.removeEventListener('touchmove', onTouchMove);
                _bar.removeEventListener('touchend', onTouchEnd);
            }
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            _container = null;
            _bar = null;
        }
    };
})();
