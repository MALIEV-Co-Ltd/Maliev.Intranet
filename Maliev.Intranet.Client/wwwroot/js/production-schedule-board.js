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
    const timeHeading = board.querySelector('.psb-time-heading');
    const timeSlotWidth = timeHeading?.getBoundingClientRect().width ?? 0;
    const measuredOneHourOffset = timeSlotWidth > 0 ? timeSlotWidth * 2 : 0;
    const fallbackOffset = Math.min(Math.max(timelineViewportWidth * 0.2, 80), 240);
    const maxOffset = Math.max(80, timelineViewportWidth * 0.6);
    const timelineOffset = Math.min(
        measuredOneHourOffset > 0 ? measuredOneHourOffset : fallbackOffset,
        maxOffset);
    const desiredViewportLeft = machineColumnWidth + timelineOffset;
    const markerContentLeft = board.scrollLeft + markerRect.left - boardRect.left;
    const maxScrollLeft = Math.max(0, board.scrollWidth - board.clientWidth);
    const nextScrollLeft = Math.min(Math.max(markerContentLeft - desiredViewportLeft, 0), maxScrollLeft);

    if (!Number.isFinite(nextScrollLeft)) {
        return;
    }

    board.scrollTo({ left: nextScrollLeft, behavior: 'auto' });
};
