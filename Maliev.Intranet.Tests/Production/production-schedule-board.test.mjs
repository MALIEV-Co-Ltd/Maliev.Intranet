import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

function loadScheduleBoardContext() {
    const context = {
        window: {},
    };
    context.globalThis = context;
    vm.createContext(context);

    const helperPath = new URL('../../Maliev.Intranet.Client/wwwroot/js/production-schedule-board.js', import.meta.url);
    const code = fs.readFileSync(helperPath, 'utf8');
    vm.runInContext(code, context);

    return context;
}

function createBoard() {
    const boardRect = { left: 20 };
    const markerRect = { left: 760 };
    const machineRect = { width: 220 };
    const timeHeadingRect = { width: 80 };
    const board = {
        clientWidth: 500,
        scrollWidth: 1600,
        scrollLeft: 0,
        scrollToArgs: null,
        getBoundingClientRect: () => boardRect,
        querySelector: selector => {
            if (selector === '.psb-now-line') {
                return { getBoundingClientRect: () => markerRect };
            }

            if (selector === '.psb-machine-heading, .psb-machine-cell') {
                return { getBoundingClientRect: () => machineRect };
            }

            if (selector === '.psb-time-heading') {
                return { getBoundingClientRect: () => timeHeadingRect };
            }

            return null;
        },
        scrollTo: args => {
            board.scrollToArgs = args;
        },
    };

    return board;
}

test('current time auto-scroll leaves about one hour of timeline before the now marker', () => {
    const context = loadScheduleBoardContext();
    const board = createBoard();

    context.window.malievProductionSchedule.scrollCurrentTimeIntoView(board);

    assert.equal(board.scrollToArgs?.left, 360);
    assert.equal(board.scrollToArgs?.behavior, 'auto');
});
