namespace ShipExecNavigator.Shared.AI;

/// <summary>
/// Authoritative DOM / CSS reference injected into the AI system prompt whenever
/// the user has an XML tree loaded.  Keep in sync with XmlNodeTree.razor,
/// XmlViewer.razor, StudioChatPanel.razor and their associated .css files.
/// </summary>
public static class NavigatorDomCheatSheet
{
    public const string Content = """

        ---
        ## ShipExec Navigator – DOM / CSS Reference (authoritative – do NOT guess class names)

        ### XML Tree – node anatomy
        Every configuration element is a recursive `div.xml-node`:

        ```html
        <div id="node-{guid}"
             class="xml-node [node--highlighted] [node--readonly]"
             data-nodepath="Company.Section.ElementName">

          <!-- ── Line (always visible) ── -->
          <div class="xml-node-line [clickable]
                                    [xml-node-line--changed]
                                    [xml-node-line--has-change]">

            <span class="expand-icon">▶ | ▼</span>
            <span class="node-type-icon">emoji</span>
            <span class="node-label">ElementName</span>
            <span class="readonly-badge">🔒 read-only</span>   <!-- only when read-only -->

            <!-- each XML attribute: -->
            <span class="attr-dot"> · </span>
            <span class="xml-attr-name">attrName</span>
            <span class="attr-eq">=</span>
            <span class="xml-attr-val [editable] [xml-attr-val--changed]">value</span>

            <!-- leaf element value: -->
            <span class="node-sep">: </span>
            <span class="xml-val [editable] [xml-val--ref] [xml-val--changed]">value</span>

            <!-- empty leaf (no value): -->
            <span class="node-empty">: —</span>

            <!-- collapsed / lazy hint: -->
            <span class="node-info">: EntityName  (N children)</span>
            <span class="node-hint"> — click to load</span>   <!-- lazy-loadable -->
          </div>

          <!-- ── Original-value row (only when the node has been edited) ── -->
          <div class="xml-orig-line">
            <span class="xml-val xml-val--orig">old value</span>
            <span class="xml-attr-val xml-attr-val--orig">old attr value</span>
          </div>

          <!-- ── Children (only when expanded) ── -->
          <div class="xml-children" style="--depth-color: #3b82f6">
            <!-- more div.xml-node … -->
          </div>

        </div>
        ```

        ### Key selectors / patterns

        | Goal | Selector / snippet |
        |---|---|
        | Node by GUID | `document.getElementById('node-' + guid)` |
        | Nodes by exact path | `document.querySelectorAll('[data-nodepath="Company.Shippers"]')` |
        | Nodes whose path starts with prefix | `[data-nodepath^="Company.Shippers"]` |
        | All tree nodes | `document.querySelectorAll('.xml-node')` |
        | Node label text (XML element name) | `el.querySelector('.node-label').textContent.trim()` |
        | Entity / group summary text | `el.querySelector('.node-info')?.textContent.trim()` |
        | Element value text | `el.querySelector('.xml-val')?.textContent.trim()` |
        | All attribute names on a node line | `el.querySelectorAll('.xml-attr-name')` |
        | Hide a node (and its subtree) | `el.style.display = 'none'` |
        | Show again | `el.style.display = ''` |
        | Direct child nodes only | `el.querySelectorAll(':scope > .xml-children > .xml-node')` |
        | **Shipper table rows** (Shippers tab) | `document.querySelectorAll('.shippers-table tbody tr')` |
        | Shipper name cell text (Shippers tab) | `row.querySelector('.shipper-name').textContent.trim()` |
        | Shipper symbol cell text (Shippers tab) | `row.querySelector('.shipper-symbol').textContent.trim()` |
        | Hide a shipper table row | `row.style.display = 'none'` |

        **Important:** hiding a `div.xml-node` visually hides its entire subtree without recursion.  
        **Important:** `data-nodepath` uses dot-separated XML element names matching the document hierarchy.  
        The root element is `Company`, so paths look like `"Company.Shippers.Shipper"`, `"Company.Name"`,  
        `"Company.Profiles.Profile"`, etc. There is NO `ShipExec` prefix in the path.  
        **Important:** Multiple sibling nodes of the same type share the same `data-nodepath`  
        (e.g. every Shipper node has `data-nodepath="Company.Shippers.Shipper"`).  
        To distinguish individual items, read the `.node-info` span which contains  
        `": <Name>  (<childCount>)"` for collapsed parent nodes (the Name comes from the first  
        child element named Name, Symbol, or DisplayName).  
        **Important:** node `id` attributes are GUIDs — `id="node-550e8400-e29b-41d4-a716-446655440000"`.  
        **Important:** Shippers appear in **two places**:  
        1. **Company tab (XML tree)** — as `div.xml-node` elements with `data-nodepath="Company.Shippers.Shipper"`.  
           Read `.node-info` text to get the shipper name.  
        2. **Shippers tab (table)** — as rows in `table.shippers-table`.  
           Read `.shipper-name` (or `.shipper-symbol`, `.shipper-id`) cell text.  
        Target whichever view is currently visible, or target both if the user does not specify.

        ### Depth colour palette (`--depth-color` on `.xml-children`)
        depth 0 → `#3b82f6` · depth 1 → `#8b5cf6` · depth 2 → `#06b6d4` · depth 3 → `#10b981` · depth 4+ → `#f59e0b`

        ---
        ### Page layout  `/studio`

        ```
        div.viewer-page
          div.page-tab-bar
            a.page-tab | span.page-tab.page-tab--active

          div.load-panel [load-panel--compact when tree loaded]

          div.viewer-content-row
            div.viewer-main-area
              div.viewer-tabs
                button.viewer-tab [viewer-tab--active]   ← "company" | "shippers" | "users" | "logs"
              div.viewer-body-row
                <!-- VariancePanel (left) – see below -->
                div.viewer-main
                  <!--  Company tab (display:none when inactive) -->
                  div.viewer-workspace
                    div.tree-panel
                      div.tree-toolbar
                        div.toolbar-info
                          span.root-tag          ← XML root element name
                          span.company-name      ← loaded company name
                          span.stat-pill         ← "N nodes" / "N sections"
                          span.dirty-pill        ← "● unsaved" (only when dirty)
                          span.source-badge.source-badge--live
                        div.toolbar-actions
                          button.btn-export | button.btn-action | button.btn-action.btn-action--chat
                      div.tree-body              ← XmlNodeTree root renders here

                  <!--  Shippers tab -->
                  div.shippers-panel
                    div.shippers-toolbar > button.btn-action
                    div.shippers-table-wrap
                      table.shippers-table
                        thead > tr > th
                        tbody > tr > td  (.shipper-id | .shipper-symbol | .shipper-name)

                  <!--  Users tab -->
                  div.um-tab-content

                  <!--  Logs tab -->
                  div.logs-panel
                    div.logs-toolbar
                      div.log-mode-toggle
                        button.log-mode-btn [log-mode-btn--active]  ← "application" | "security" | "both"
                      div.logs-search-bar
                        input[type=date].logs-date-input
                        button.btn-load.logs-search-btn
                    div.logs-table-wrap
                      table.logs-table
                        thead > tr > th
                        tbody > tr
                          td.log-date
                          span.log-level.log-level--{info|warn|error|debug|trace|fatal}
                          td.log-logger | td.log-message | td.log-server | td.log-txid
                    <!-- security log: td.log-logger for user/action/entity/entityName -->
                    <!-- combined row: tr.comb-row.comb-row--{app|security} + span.log-source.log-source--{app|security} -->

          div.error-banner[role=alert] > span.error-icon + span
        ```

        ---
        ### Variance panel (left sidebar)

        ```
        <!-- collapsed -->
        div.vp-strip > button.vp-toggle-btn > span.vp-toggle-icon + span.vp-badge

        <!-- expanded -->
        div.vp-panel
          div.vp-header
            span.vp-title
            div.vp-header-actions
              span.vp-count | button.vp-clear-btn | button.vp-history-btn
              button.vp-toggle-btn.vp-toggle-btn--in-header
          div.vp-body
            div.vp-history-error
            div.vp-empty
            div.vp-entry
                .vp-entry--{modified|added|deleted}
                [.vp-entry--expandable] [.vp-entry--historical]
                [.vp-entry--undone] [.vp-entry--reverted]
              div.vp-entry-icon
              div.vp-entry-content > div.vp-entry-path + div.vp-entry-desc
              button.vp-undo-btn
        ```

        ---
        ### Context menu

        ```
        div.ctx-backdrop
        div.ctx-menu   (inline style="left:Xpx; top:Ypx")
          button.ctx-item [ctx-item--danger] > span.ctx-icon + text
          div.ctx-sep
        ```

        ---
        ### AI Chat panel (fixed bottom bar)

        ```
        div#studio-chat-panel .scp-panel [scp-panel--hidden] [scp-panel--minimized]
          div#studio-chat-resize-handle .scp-resize-handle
          div.scp-header
            span.scp-header-title
            div.scp-header-actions
              button.scp-btn-undo
              button.scp-btn-rag [scp-btn-rag--on]
              button.scp-btn-clear | button.scp-btn-minimize | button.scp-btn-collapse
          div#scp-messages-scroll .scp-messages
            div.scp-bubble .scp-bubble--user | .scp-bubble--assistant
              span.scp-bubble-label
              div.scp-bubble-body > pre.scp-code | code.scp-inline-code
          div.scp-input-row
            textarea#scp-textarea .scp-textarea
            button.scp-send-btn
          div.scp-error
        ```

        ---
        ### Inline-edit controls (active only while editing a value or attribute)

        ```
        input.inline-edit [inline-edit--attr]
        select.inline-edit .inline-edit--select [inline-edit--attr] > option
        ```

        ---
        ### Rules for JavaScript you write
        1. Use the exact class names above — do NOT invent or guess class names.
        2. Never target Blazor scope attributes (`b-xxxxxxxxxx`).
        3. The tree is recursive; to act on all nodes under a subtree, query within that node's element.
        4. Prefer `[data-nodepath]` attribute selectors to locate specific configuration paths.
        5. Read `.node-label` text content to identify an element by name at runtime.
        6. Always place `debugger;` as the very first statement inside the function body.
        7. Functions must be named (not arrow / IIFE) so the runner can call them by name.
        8. Return nothing — side-effects on `style.display` are automatically tracked for Undo.

        ---
        ### Deleting shippers (model-level removal)
        When the user asks to **delete** or **remove** shippers (not just hide), call the
        `delete_shippers` plugin function. It returns a JSON list of all shippers with
        `id`, `symbol`, and `name`. Filter the list to match the user's condition, then
        respond with a ```shipper-delete code block containing ONLY the matching entries
        as a JSON array. Example:

        ```shipper-delete
        [
          { "id": "123", "symbol": "TST", "name": "Test Shipper" }
        ]
        ```

        The Navigator will remove those shippers from the XML tree, create trackable
        variance entries (visible in the Variance panel), and stage them as pending
        Remove operations that can be pushed to the live server via "View Changes".
        Do NOT include JavaScript when performing a deletion — the ```shipper-delete
        block is all that is needed.
        ---
        """;
}
