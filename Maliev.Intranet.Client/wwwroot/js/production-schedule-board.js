window.malievProductionSchedule = window.malievProductionSchedule || {};

window.malievProductionSchedule.scrollCurrentTimeIntoView = function (board) {
    if (!board) {
        return;
    }

    const marker = board.querySelector('.psb-now-line');
    if (!marker) {
        return;
    }

    const boardRect = board.getBoundingClientRect();
    const markerRect = marker.getBoundingClientRect();
    const machineColumn = board.querySelector('.psb-machine-heading, .psb-machine-cell');
    const machineColumnWidth = machineColumn?.getBoundingClientRect().width ?? 0;
    const timelineViewportWidth = Math.max(0, board.clientWidth - machineColumnWidth);
    const timelineOffset = Math.min(Math.max(timelineViewportWidth * 0.16, 56), 180);
    const desiredViewportLeft = machineColumnWidth + timelineOffset;
    const markerContentLeft = board.scrollLeft + markerRect.left - boardRect.left;
    const maxScrollLeft = Math.max(0, board.scrollWidth - board.clientWidth);
    const nextScrollLeft = Math.min(Math.max(markerContentLeft - desiredViewportLeft, 0), maxScrollLeft);

    if (!Number.isFinite(nextScrollLeft)) {
        return;
    }

    board.scrollTo({ left: nextScrollLeft, behavior: 'auto' });
};
