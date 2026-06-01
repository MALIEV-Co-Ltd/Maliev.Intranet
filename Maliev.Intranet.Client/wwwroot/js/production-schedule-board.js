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

window.malievProductionSchedule.scrollFocusedMachineIntoView = function (board, machineId, projectPartId) {
    if (!board || !machineId) {
        return;
    }

    const machineRow = board.querySelector(`.psb-machine-row[data-machine-id="${machineId}"]`);
    if (!machineRow) {
        return;
    }

    const header = board.querySelector('.psb-grid-header');
    const headerHeight = header?.getBoundingClientRect().height ?? 0;
    const nextScrollTop = Math.max(0, machineRow.offsetTop - headerHeight - 8);
    let nextScrollLeft = board.scrollLeft;

    if (projectPartId) {
        const targetSlot = Array.from(board.querySelectorAll('[data-project-part-id]'))
            .find(slot => slot.getAttribute('data-project-part-id') === projectPartId);

        if (targetSlot) {
            const boardRect = board.getBoundingClientRect();
            const slotRect = targetSlot.getBoundingClientRect();
            const machineColumn = board.querySelector('.psb-machine-heading, .psb-machine-cell');
            const machineColumnWidth = machineColumn?.getBoundingClientRect().width ?? 220;
            const desiredViewportLeft = Math.min(Math.max(machineColumnWidth, 160), 240);
            const slotContentLeft = board.scrollLeft + slotRect.left - boardRect.left;
            const maxScrollLeft = Math.max(0, board.scrollWidth - board.clientWidth);
            nextScrollLeft = Math.min(Math.max(slotContentLeft - desiredViewportLeft, 0), maxScrollLeft);
        }
    }

    board.scrollTo({ top: nextScrollTop, left: nextScrollLeft, behavior: 'smooth' });
};
