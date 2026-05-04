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

window.downloadFileFromStream = async function (fileName, dotnetStreamRef) {
    const arrayBuffer = await dotnetStreamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
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
    if (!el) return;
    el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    // Re-trigger the flash animation after scrolling completes so it plays while visible
    var nodeLine = el.querySelector('.xml-node-line');
    if (nodeLine) {
        nodeLine.style.animation = 'none';
        // Force a reflow so the animation restarts
        void nodeLine.offsetWidth;
        nodeLine.style.animation = '';
    }
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


window.initChatPanelResize = function (handleId, panelId, dotNetRef) {
    var handle = document.getElementById(handleId);
    var panel  = document.getElementById(panelId);
    if (!handle || !panel) return;

    var startX = 0;
    var startWidth = 0;

    function onMouseMove(e) {
        var newWidth = Math.max(260, Math.min(700, startWidth + (e.clientX - startX)));
        panel.style.width = newWidth + 'px';
    }

    function onMouseUp(e) {
        var newWidth = Math.max(260, Math.min(700, startWidth + (e.clientX - startX)));
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        document.body.style.userSelect = '';
        document.body.style.cursor = '';
        dotNetRef.invokeMethodAsync('SetPanelSize', Math.round(newWidth));
    }

    handle.addEventListener('mousedown', function (e) {
        e.preventDefault();
        startX = e.clientX;
        startWidth = panel.offsetWidth;
        document.body.style.userSelect = 'none';
        document.body.style.cursor = 'ew-resize';
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });
};

// ── JWT localStorage management
window.storeJwt = function (jwtJson, adminUrl) {
    try {
        var data = { jwtJson: jwtJson, adminUrl: adminUrl, storedAt: Date.now() };
        // Parse expires_in from the JWT payload to determine TTL
        try {
            var parsed = JSON.parse(jwtJson);
            if (parsed && parsed.expires_in) {
                data.expiresIn = parsed.expires_in; // seconds
            }
        } catch (_) { /* not valid JSON — store anyway */ }
        localStorage.setItem('shipexec_jwt', JSON.stringify(data));
    } catch (_) { /* localStorage unavailable */ }
};

window.loadJwt = function () {
    try {
        var raw = localStorage.getItem('shipexec_jwt');
        if (!raw) return null;
        var data = JSON.parse(raw);
        // Check expiry: storedAt (ms) + expiresIn (seconds → ms)
        if (data.expiresIn && data.storedAt) {
            var expiresAtMs = data.storedAt + (data.expiresIn * 1000);
            if (Date.now() >= expiresAtMs) {
                localStorage.removeItem('shipexec_jwt');
                return null; // expired
            }
        }
        return { jwtJson: data.jwtJson || '', adminUrl: data.adminUrl || '' };
    } catch (_) { return null; }
};

window.clearJwt = function () {
    try { localStorage.removeItem('shipexec_jwt'); } catch (_) { }
};

window.isJwtExpired = function () {
    try {
        var raw = localStorage.getItem('shipexec_jwt');
        if (!raw) return true;
        var data = JSON.parse(raw);
        if (data.expiresIn && data.storedAt) {
            return Date.now() >= (data.storedAt + (data.expiresIn * 1000));
        }
        return false; // no expires_in means we can't tell — assume valid
    } catch (_) { return true; }
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

// ── CBR code viewer: syntax-highlighted JS with line numbers ─────────────────
window.renderCbrCode = function (containerId, code) {
    var container = document.getElementById(containerId);
    if (!container) return;

    var lines = (code || '').split('\n');
    var gutterHtml = '';
    var codeHtml = '';
    for (var i = 0; i < lines.length; i++) {
        gutterHtml += '<span class="cbr-ln">' + (i + 1) + '</span>\n';
        codeHtml += highlightJsLine(escapeHtml(lines[i])) + '\n';
    }
    container.innerHTML =
        '<div class="cbr-gutter" aria-hidden="true">' + gutterHtml + '</div>' +
        '<pre class="cbr-code-pre"><code>' + codeHtml + '</code></pre>';
};

// ── SBR code viewer: syntax-highlighted C# / XML / generic with line numbers ─
window.renderSbrCode = function (containerId, code, fileName) {
    var container = document.getElementById(containerId);
    if (!container) return;

    var ext = (fileName || '').split('.').pop().toLowerCase();
    var highlighter = ext === 'cs' ? highlightCsLine
                    : ext === 'xml' || ext === 'csproj' || ext === 'config' ? highlightXmlLine
                    : highlightGenericLine;

    var lines = (code || '').split('\n');
    var gutterHtml = '';
    var codeHtml = '';
    for (var i = 0; i < lines.length; i++) {
        gutterHtml += '<span class="cbr-ln">' + (i + 1) + '</span>\n';
        codeHtml += highlighter(escapeHtml(lines[i])) + '\n';
    }
    container.innerHTML =
        '<div class="cbr-gutter" aria-hidden="true">' + gutterHtml + '</div>' +
        '<pre class="cbr-code-pre"><code>' + codeHtml + '</code></pre>';
};

function escapeHtml(s) {
    return s
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

function highlightJsLine(escapedLine) {
    // Order matters: comments first, then strings, then regex, then numbers, then keywords
    // 1) Single-line comments
    // Fixed formatting
    escapedLine = escapedLine.replace(
        /(\/\/.*)$/,
        '<span class="js-comment">$1</span>'
    );
    // 2) Strings (double-quoted, single-quoted, template literals)
    // Replaced with method
    escapedLine = escapedLine.replace(
        /(&quot;(?:[^&]|&(?!quot;))*?&quot;)/g,
        '<span class="js-string">$1</span>'
    );
    escapedLine = escapedLine.replace(
        /(&#39;(?:[^&]|&(?!#39;))*?&#39;|'[^']*?')/g,
        '<span class="js-string">$1</span>'
    );
    escapedLine = escapedLine.replace(
        /(`[^`]*?`)/g,
        '<span class="js-string">$1</span>'
    );
    // 3) Numbers
    // Fixed formatting
    escapedLine = escapedLine.replace(
        /\b(\d+\.?\d*)\b/g,
        '<span class="js-number">$1</span>'
    );
    // 4) Keywords
    // Replaced with method
    var kwPattern = /\b(var|let|const|function|return|if|else|for|while|do|switch|case|break|continue|new|this|typeof|instanceof|in|of|try|catch|finally|throw|class|extends|super|import|export|default|from|async|await|yield|void|delete|true|false|null|undefined)\b/g;
    escapedLine = escapedLine.replace(
        kwPattern,
        '<span class="js-keyword">$1</span>'
    );
    return escapedLine;
}

// ── C# syntax highlighting ───────────────────────────────────────────────────
function highlightCsLine(escapedLine) {
    // Single-line comments
    escapedLine = escapedLine.replace(
        /(\/\/.*)$/,
        '<span class="cs-comment">$1</span>'
    );
    // Strings (double-quoted)
    escapedLine = escapedLine.replace(
        /(&quot;(?:[^&]|&(?!quot;))*?&quot;)/g,
        '<span class="cs-string">$1</span>'
    );
    // Char literals
    escapedLine = escapedLine.replace(
        /('\\?.')/g,
        '<span class="cs-string">$1</span>'
    );
    // Numbers
    escapedLine = escapedLine.replace(
        /\b(\d+\.?\d*[fFdDmMlLuU]?)\b/g,
        '<span class="cs-number">$1</span>'
    );
    // C# keywords
    var kwPattern = /\b(abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|async|await|dynamic|nameof|when|where|yield|record|init|required|file|scoped|global|get|set|add|remove|value|nint|nuint)\b/g;
    escapedLine = escapedLine.replace(
        kwPattern,
        '<span class="cs-keyword">$1</span>'
    );
    // Preprocessor directives
    escapedLine = escapedLine.replace(
        /^(\s*#\w+.*)/,
        '<span class="cs-preprocessor">$1</span>'
    );
    // Attributes in brackets
    escapedLine = escapedLine.replace(
        /(\[[\w.]+\])/g,
        '<span class="cs-attribute">$1</span>'
    );
    return escapedLine;
}

// ── XML syntax highlighting ──────────────────────────────────────────────────
function highlightXmlLine(escapedLine) {
    // Comments
    escapedLine = escapedLine.replace(
        /(&lt;!--.*?--&gt;)/g,
        '<span class="cs-comment">$1</span>'
    );
    // Attribute values
    escapedLine = escapedLine.replace(
        /(&quot;[^&]*?&quot;)/g,
        '<span class="cs-string">$1</span>'
    );
    // Tag names
    escapedLine = escapedLine.replace(
        /(&lt;\/?)([\w:.-]+)/g,
        '$1<span class="cs-keyword">$2</span>'
    );
    // Attribute names
    escapedLine = escapedLine.replace(
        /\b([\w:-]+)(=)/g,
        '<span class="cs-attribute">$1</span>$2'
    );
    return escapedLine;
}

function highlightGenericLine(escapedLine) {
    return escapedLine;
}

// ── CBR diff viewer: side-by-side original vs output with diff highlighting ─────
window.renderCbrDiff = function (containerId, originalCode, newCode) {
    var container = document.getElementById(containerId);
    if (!container) return;

    var oldLines = (originalCode || '').split('\n');
    var newLines = (newCode || '').split('\n');
    var diff = computeLineDiff(oldLines, newLines);

    // Build left (original) and right (output) panels
    // Fixed formatting
    var leftGutter = '', leftCode = '';
    var rightGutter = '', rightCode = '';
    var leftNum = 0, rightNum = 0;

    for (var i = 0; i < diff.length; i++) {
        var entry = diff[i];
        if (entry.type === 'equal') {
            leftNum++; rightNum++;
            // Replaced with method
            var hl = highlightJsLine(escapeHtml(entry.oldLine));
            leftGutter  += '<span class="cbr-ln">' + leftNum + '</span>\n';
            leftCode    += '<div class="cbr-diff-line">' + hl + '</div>';
            rightGutter += '<span class="cbr-ln">' + rightNum + '</span>\n';
            rightCode   += '<div class="cbr-diff-line">' + hl + '</div>';
        } else if (entry.type === 'removed') {
            leftNum++;
            // Replaced with method
            leftGutter += '<span class="cbr-ln cbr-ln--removed">' + leftNum + '</span>\n';
            leftCode   += '<div class="cbr-diff-line cbr-diff-removed">' + highlightJsLine(escapeHtml(entry.oldLine)) + '</div>';
            rightGutter += '<span class="cbr-ln cbr-ln--blank"></span>\n';
            rightCode   += '<div class="cbr-diff-line cbr-diff-blank"></div>';
        } else if (entry.type === 'added') {
            rightNum++;
            // Replaced with method
            leftGutter  += '<span class="cbr-ln cbr-ln--blank"></span>\n';
            leftCode    += '<div class="cbr-diff-line cbr-diff-blank"></div>';
            rightGutter += '<span class="cbr-ln cbr-ln--added">' + rightNum + '</span>\n';
            rightCode   += '<div class="cbr-diff-line cbr-diff-added">' + highlightJsLine(escapeHtml(entry.newLine)) + '</div>';
        } else if (entry.type === 'modified') {
            leftNum++; rightNum++;
            // Show old version in red (removed) and new version in green (added)
            leftGutter  += '<span class="cbr-ln cbr-ln--removed">' + leftNum + '</span>\n';
            leftCode    += '<div class="cbr-diff-line cbr-diff-removed">' + highlightJsLine(escapeHtml(entry.oldLine)) + '</div>';
            rightGutter += '<span class="cbr-ln cbr-ln--added">' + rightNum + '</span>\n';
            rightCode   += '<div class="cbr-diff-line cbr-diff-added">' + highlightJsLine(escapeHtml(entry.newLine)) + '</div>';
        }
    }

    // Fixed formatting
    container.innerHTML =
        '<div class="cbr-diff-side cbr-diff-original">' +
            '<div class="cbr-diff-label">Original</div>' +
            '<div class="cbr-diff-content">' +
                '<div class="cbr-gutter" aria-hidden="true">' + leftGutter + '</div>' +
                '<pre class="cbr-code-pre"><code>' + leftCode + '</code></pre>' +
            '</div>' +
        '</div>' +
        '<div class="cbr-diff-divider"></div>' +
        '<div class="cbr-diff-side cbr-diff-output">' +
            '<div class="cbr-diff-label">Output</div>' +
            '<div class="cbr-diff-content">' +
                '<div class="cbr-gutter" aria-hidden="true">' + rightGutter + '</div>' +
                '<pre class="cbr-code-pre"><code>' + rightCode + '</code></pre>' +
            '</div>' +
        '</div>';

    // Synchronize scrolling between left and right panels
    // Replaced with method
    var leftPanel  = container.querySelector('.cbr-diff-original .cbr-diff-content');
    var rightPanel = container.querySelector('.cbr-diff-output .cbr-diff-content');
    if (leftPanel && rightPanel) {
        var syncing = false;
        leftPanel.addEventListener('scroll', function () {
            if (syncing) return;
            syncing = true;
            rightPanel.scrollTop = leftPanel.scrollTop;
            rightPanel.scrollLeft = leftPanel.scrollLeft;
            syncing = false;
        });
        rightPanel.addEventListener('scroll', function () {
            if (syncing) return;
            syncing = true;
            leftPanel.scrollTop = rightPanel.scrollTop;
            leftPanel.scrollLeft = rightPanel.scrollLeft;
            syncing = false;
        });
    }
};

// Compute a line-level diff producing equal / removed / added / modified entries
// Replaced with method
function computeLineDiff(oldLines, newLines) {
    // Build LCS table
    var m = oldLines.length, n = newLines.length;
    // For very large files, fall back to a simple line-by-line comparison
    // Fixed formatting
    if (m > 2000 || n > 2000) {
        return computeSimpleDiff(oldLines, newLines);
    }

    var dp = new Array(m + 1);
    for (var i = 0; i <= m; i++) {
        dp[i] = new Array(n + 1);
        dp[i][0] = 0;
    }
    for (var j = 0; j <= n; j++) dp[0][j] = 0;

    for (var i = 1; i <= m; i++) {
        for (var j = 1; j <= n; j++) {
            if (oldLines[i - 1] === newLines[j - 1]) {
                dp[i][j] = dp[i - 1][j - 1] + 1;
            } else {
                dp[i][j] = Math.max(dp[i - 1][j], dp[i][j - 1]);
            }
        }
    }

    // Backtrack to produce diff entries
    // Replaced with method
    var result = [];
    var i = m, j = n;
    while (i > 0 || j > 0) {
        if (i > 0 && j > 0 && oldLines[i - 1] === newLines[j - 1]) {
            result.push({ type: 'equal', oldLine: oldLines[i - 1], newLine: newLines[j - 1] });
            i--; j--;
        } else if (j > 0 && (i === 0 || dp[i][j - 1] >= dp[i - 1][j])) {
            result.push({ type: 'added', newLine: newLines[j - 1] });
            j--;
        } else {
            result.push({ type: 'removed', oldLine: oldLines[i - 1] });
            i--;
        }
    }
    result.reverse();

    // Post-process: merge adjacent removed+added pairs into 'modified'
    // Replaced with method
    var merged = [];
    var idx = 0;
    while (idx < result.length) {
        if (idx + 1 < result.length &&
            result[idx].type === 'removed' && result[idx + 1].type === 'added') {
            merged.push({
                type: 'modified',
                oldLine: result[idx].oldLine,
                newLine: result[idx + 1].newLine
            });
            idx += 2;
        } else {
            merged.push(result[idx]);
            idx++;
        }
    }
    return merged;
}

// Simple fallback diff for very large files
// Fixed formatting
function computeSimpleDiff(oldLines, newLines) {
    var result = [];
    var max = Math.max(oldLines.length, newLines.length);
    for (var i = 0; i < max; i++) {
        var ol = i < oldLines.length ? oldLines[i] : null;
        var nl = i < newLines.length ? newLines[i] : null;
        if (ol === nl) {
            result.push({ type: 'equal', oldLine: ol, newLine: nl });
        } else if (ol === null) {
            result.push({ type: 'added', newLine: nl });
        } else if (nl === null) {
            result.push({ type: 'removed', oldLine: ol });
        } else {
            result.push({ type: 'modified', oldLine: ol, newLine: nl });
        }
    }
    return result;
}

// ── Download HTML page as styled PDF via browser print dialog ─────────────────
window.downloadAsPdf = function (url) {
    var w = window.open(url, '_blank');
    if (!w) return;
    // Wait for the page to load, then trigger the browser print dialog
    // (the user can choose "Save as PDF" as the destination)
    w.addEventListener('load', function () {
        setTimeout(function () { w.print(); }, 600);
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

window.saveProjectZip = async function (fileName, base64) {
    const byteChars = atob(base64);
    const byteNums = new Uint8Array(byteChars.length);
    for (let i = 0; i < byteChars.length; i++) {
        byteNums[i] = byteChars.charCodeAt(i);
    }
    const blob = new Blob([byteNums], { type: 'application/zip' });

    // Try File System Access API (folder picker) if available
    if (window.showSaveFilePicker) {
        try {
            const handle = await window.showSaveFilePicker({
                suggestedName: fileName,
                types: [{
                    description: 'ZIP Archive',
                    accept: { 'application/zip': ['.zip'] }
                }]
            });
            const writable = await handle.createWritable();
            await writable.write(blob);
            await writable.close();
            return;
        } catch (err) {
            if (err.name === 'AbortError') return; // user cancelled
            // Fall through to regular download
        }
    }

    // Fallback: regular download
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
