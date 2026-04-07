window.exportXml = function (fileName, content) {
    const blob = new Blob([content], { type: 'application/xml' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.downloadFile = function (fileName, content, mimeType) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.downloadBase64File = function (fileName, base64, mimeType) {
    const byteChars = atob(base64);
    const byteNums  = new Uint8Array(byteChars.length);
    for (let i = 0; i < byteChars.length; i++) {
        byteNums[i] = byteChars.charCodeAt(i);
    }
    const blob = new Blob([byteNums], { type: mimeType });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.scrollToNode = function (elementId) {
    const el = document.getElementById(elementId);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
};

window.triggerClick = function (elementId) {
    document.getElementById(elementId)?.click();
};

// ── AI-generated script execution ────────────────────────────────────────────
window.executeAiScript = function (jsCode) {
    // Inject debugger as the very first statement inside the function body
    var instrumented = jsCode.replace(
        /(function\s+\w+\s*\([^)]*\)\s*\{)/,
        '$1\n    debugger;'
    );

    // Indirect eval – runs in global scope so the function declaration
    // is added to window and can be looked up by name below.
    (0, eval)(instrumented);  // eslint-disable-line no-eval

    // Snapshot which .xml-node elements are currently hidden
    var hiddenBefore = new Set(
        Array.from(document.querySelectorAll('.xml-node'))
            .filter(function (el) { return el.style.display === 'none'; })
            .map(function (el) { return el.id; })
    );

    // Find the first declared function name and call it
    var match = jsCode.match(/function\s+(\w+)\s*\(/);
    if (match) {
        var fn = window[match[1]];
        if (typeof fn === 'function') {
            debugger;   // breakpoint just before invoking the AI function
            fn();
        }
    }

    // Return IDs of elements that were newly hidden (for undo)
    return Array.from(document.querySelectorAll('.xml-node'))
        .filter(function (el) {
            return el.style.display === 'none' && el.id && !hiddenBefore.has(el.id);
        })
        .map(function (el) { return el.id; });
};

window.undoAiScript = function (elementIds) {
    var count = 0;
    (elementIds || []).forEach(function (id) {
        var el = document.getElementById(id);
        if (el) { el.style.display = ''; count++; }
    });
    return count;
};


window.initChatPanelDrag = function (handleId, panelId, dotNetRef) {
    var handle = document.getElementById(handleId);
    var panel  = document.getElementById(panelId);
    if (!handle || !panel) return;

    var startY = 0;
    var startH = 0;

    function onMouseMove(e) {
        var delta = startY - e.clientY;
        var newH  = Math.max(140, Math.min(700, startH + delta));
        panel.style.height = newH + 'px';
    }

    function onMouseUp(e) {
        var delta = startY - e.clientY;
        var newH  = Math.max(140, Math.min(700, startH + delta));
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup',   onMouseUp);
        document.body.style.userSelect  = '';
        document.body.style.cursor      = '';
        dotNetRef.invokeMethodAsync('SetPanelHeight', Math.round(newH));
    }

    handle.addEventListener('mousedown', function (e) {
        e.preventDefault();
        startY = e.clientY;
        startH = panel.offsetHeight;
        document.body.style.userSelect = 'none';
        document.body.style.cursor     = 'ns-resize';
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup',   onMouseUp);
    });
};

// ── Chat input history (↑ / ↓ arrow recall) ─────────────────────────────────
window.initChatInputHistory = function (textareaId, dotNetRef) {
    var textarea = document.getElementById(textareaId);
    if (!textarea) return;

    textarea.addEventListener('keydown', function (e) {
        if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return;
        if (e.shiftKey || e.ctrlKey || e.altKey) return;

        var pos = textarea.selectionStart;
        var val = textarea.value;

        // ArrowUp: only intercept when cursor is on the first line
        if (e.key === 'ArrowUp' && val.substring(0, pos).indexOf('\n') !== -1) return;
        // ArrowDown: only intercept when cursor is on the last line
        if (e.key === 'ArrowDown' && val.substring(pos).indexOf('\n') !== -1) return;

        e.preventDefault();

        dotNetRef.invokeMethodAsync('HandleHistoryKey', e.key)
            .then(function (newValue) {
                if (newValue !== null && newValue !== undefined) {
                    textarea.value = newValue;
                    textarea.setSelectionRange(newValue.length, newValue.length);
                    // Keep Blazor's binding in sync
                    textarea.dispatchEvent(new Event('input', { bubbles: true }));
                }
                textarea.focus();
            });
    });
};

// ── Shipper import: column drag-hover highlight (pure JS, no Blazor round trip) ──
;(function () {
    var _hovered = null;
    function clearHover() {
        if (_hovered) { _hovered.classList.remove('isd-th--dragover'); _hovered = null; }
    }
    document.addEventListener('dragover', function (e) {
        var th = e.target && e.target.closest && e.target.closest('.isd-th-col');
        if (th !== _hovered) { clearHover(); _hovered = th; if (th) th.classList.add('isd-th--dragover'); }
    }, true);
    document.addEventListener('drop',    clearHover, true);
    document.addEventListener('dragend', clearHover, true);
}());
