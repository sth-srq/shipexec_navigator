/**
 * Shipping View Initialization — SetCurrentViewModel, InitElements
 * (consolidated orders button), InitializeCustomElements, and
 * FocusOnLoadInput.
 *
 * These utilities are part of the "Tools" namespace in the CBR 1.5 pattern
 * and are called during NewShipment and PostLoad to set up the shipping UI.
 */

// ---------------------------------------------------------------------------
// View model binding
// ---------------------------------------------------------------------------

/**
 * Stores the current view model reference, optionally initialises the
 * user context, and invokes a callback.
 *
 * @param {object}   viewModel       - The AngularJS view model.
 * @param {boolean}  setUserContext   - If true, calls SetCurrentUserContext.
 * @param {boolean}  includeUser      - If true, fetches the UserId.
 * @param {function} [callback]      - Optional callback after binding.
 */
function setCurrentViewModel(viewModel, setUserContext, includeUser, callback) {
  if (setUserContext) {
    setCurrentUserContext(viewModel, includeUser);
  }

  /* Store for later use by other Tools methods. */
  window._currentViewModel = viewModel;

  if (typeof callback === 'function') callback();
}

// ---------------------------------------------------------------------------
// Consolidated orders button
// ---------------------------------------------------------------------------

/**
 * Creates the "Consolidate Orders" button in the shipping toolbar and
 * optionally enables server-side debug logging.
 *
 * @param {boolean} logToServer - Whether to enable server-side logging.
 */
function initConsolidatedOrdersButton(logToServer) {
  $('div.top-tool-bar > div.pull-left')
    .append(
      $('<button />')
        .addClass('btn btn-xs btn-primary')
        .attr('id', 'buttonConsolidateOrders')
        .text('Consolidate Orders')
        .fadeIn()
    );

  if (typeof setServerDebugMode === 'function') {
    setServerDebugMode(logToServer);
  }
}

// ---------------------------------------------------------------------------
// Focus and custom element initialisation
// ---------------------------------------------------------------------------

/**
 * Waits for the load-value input to appear, then focuses it and
 * initialises any custom UI elements.
 */
function focusOnLoadInput() {
  waitForElement(
    'input[type=text][ng-model="vm.loadValue"]',
    true,    /* focus */
    null,    /* no default value */
    null,    /* no timeout override */
    initializeCustomElements
  );
}

/**
 * Resets custom UI elements after a new shipment is loaded.
 * Currently resets the rate-shopping checkbox.
 */
function initializeCustomElements() {
  $('#chkDoNotRateShop').prop('checked', false);
}

// ---------------------------------------------------------------------------
// Importer of Record "Same as Consignee" checkbox (AlstonBird)
// ---------------------------------------------------------------------------

/**
 * Adds an "Importer of Record: Same As Consignee" checkbox before the
 * IOR search button. Waits for the button to appear in the DOM.
 *
 * @param {string}   iorBtnSelector - jQuery selector for the IOR search button.
 * @param {function} onChecked      - Callback receiving (isChecked: boolean).
 * @returns {jQuery} The checkbox element (once created).
 */
function addIorSameAsConsigneeCheckbox(iorBtnSelector, onChecked) {
  var $checkbox;
  var interval = setInterval(function () {
    var $target = $(iorBtnSelector);
    if (!$target.length) return;
    clearInterval(interval);

    $checkbox = $('<input type="checkbox">');
    $checkbox.click(function (e) { onChecked(e.target.checked); });
    $target.before($checkbox, '<label style="padding-left: 6px;">Same As Consignee</label>');
  }, 10);

  return $checkbox;
}

// ---------------------------------------------------------------------------
// Commodity modal tab hiding (AlstonBird)
// ---------------------------------------------------------------------------

/**
 * Hides specific tabs in the commodity modal by their tab index.
 *
 * @param {jQuery}   $commodityModal - The commodity modal element.
 * @param {number[]} hideIndexes     - Array of tab indexes to hide.
 */
function hideCommodityModalTabs($commodityModal, hideIndexes) {
  $commodityModal.find('li.uib-tab.nav-item').each(function () {
    if (hideIndexes.indexOf(parseInt(this.getAttribute('index'))) > -1) {
      $(this).hide();
    }
  });
}
