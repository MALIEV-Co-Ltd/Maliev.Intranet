window.noteEditorInterop = {
    getSelection: (elementId) => {
        const el = document.getElementById(elementId);
        if (!el) return { start: 0, end: 0, text: '' };
        return { start: el.selectionStart, end: el.selectionEnd, text: el.value.substring(el.selectionStart, el.selectionEnd) };
    },
    replaceSelection: (elementId, newText) => {
        const el = document.getElementById(elementId);
        if (!el) return;
        const start = el.selectionStart;
        const end = el.selectionEnd;
        el.setRangeText(newText, start, end, 'end');
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.focus();
    },
    setupEditor: (elementId) => {
        const el = document.getElementById(elementId);
        if (!el || el.dataset.setup === 'true') return;
        el.dataset.setup = 'true';

        el.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey && !e.ctrlKey && !e.altKey) {
                const start = el.selectionStart;
                const end = el.selectionEnd;
                if (start !== end) return; // Don't handle if there's a selection

                const value = el.value;
                const lineStart = value.lastIndexOf('\n', start - 1) + 1;
                const line = value.substring(lineStart, start);

                // Check for Numbered List
                const numberedMatch = line.match(/^(\s*)(\d+)\.\s+(.*)$/);
                if (numberedMatch) {
                    const indent = numberedMatch[1];
                    const num = parseInt(numberedMatch[2]);
                    const content = numberedMatch[3];

                    if (content.trim() === "") {
                        // Empty item - remove the list prefix
                        e.preventDefault();
                        el.setRangeText("\n", lineStart, start, 'end');
                        el.dispatchEvent(new Event('input', { bubbles: true }));
                    } else {
                        // Regular item - add next number
                        e.preventDefault();
                        const nextNum = num + 1;
                        const prefix = `\n${indent}${nextNum}. `;
                        el.setRangeText(prefix, start, start, 'end');
                        el.dispatchEvent(new Event('input', { bubbles: true }));
                    }
                    return;
                }

                // Check for Bulleted List or Checklist
                const bulletMatch = line.match(/^(\s*)([-*+])\s+(.*)$/);
                if (bulletMatch) {
                    const indent = bulletMatch[1];
                    const bullet = bulletMatch[2];
                    const content = bulletMatch[3];

                    // Check for checklist
                    const checklistMatch = content.match(/^\[([ xX]?)\]\s+(.*)$/);
                    if (checklistMatch) {
                        const status = checklistMatch[1];
                        const rest = checklistMatch[2];
                        if (rest.trim() === "") {
                            e.preventDefault();
                            el.setRangeText("\n", lineStart, start, 'end');
                            el.dispatchEvent(new Event('input', { bubbles: true }));
                        } else {
                            e.preventDefault();
                            const prefix = `\n${indent}${bullet} [ ] `;
                            el.setRangeText(prefix, start, start, 'end');
                            el.dispatchEvent(new Event('input', { bubbles: true }));
                        }
                    } else {
                        if (content.trim() === "") {
                            e.preventDefault();
                            el.setRangeText("\n", lineStart, start, 'end');
                            el.dispatchEvent(new Event('input', { bubbles: true }));
                        } else {
                            e.preventDefault();
                            const prefix = `\n${indent}${bullet} `;
                            el.setRangeText(prefix, start, start, 'end');
                            el.dispatchEvent(new Event('input', { bubbles: true }));
                        }
                    }
                    return;
                }
            }
        });
    }
};
