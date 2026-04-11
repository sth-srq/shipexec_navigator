/**
 * Package History Anonymization — PostSearchHistory handlers that mask,
 * restrict, or filter history data based on user profile or business rules.
 *
 * Patterns found:
 *   - CMR: Hide apportioned totals for non-admin profiles
 *   - Nike: Mask tracking numbers (replace chars 4-9 with "XXXXXX")
 *   - Harley Davidson: Restrict printing for shipments older than 30 days
 *   - Edward Jones: Filter history by branch number from user CustomData
 *   - General Insulation: Add tracking links (UPS / CH Robinson)
 *   - User-scoped history: Filter by UserId via PreSearchHistory
 */

// ---------------------------------------------------------------------------
// Apportioned-total anonymisation (CMR)
// ---------------------------------------------------------------------------

/**
 * Hides apportioned totals from non-admin users.
 *
 * @param {object[]} packages         - The history package results.
 * @param {string}   currentProfile   - The current profile name.
 * @param {string}   adminProfileName - The admin profile name.
 */
function anonymizeApportionedTotals(packages, currentProfile, adminProfileName) {
  if (currentProfile === adminProfileName) return;
  packages.forEach(function (pkg) {
    pkg.ApportionedTotal = { Amount: null, Currency: null };
  });
}

// ---------------------------------------------------------------------------
// Tracking number masking (Nike)
// ---------------------------------------------------------------------------

/**
 * Watches the DOM for tracking-number elements and replaces characters 4-9
 * with "XXXXXX" to mask the account number portion.
 *
 * Uses a MutationObserver so it works with dynamically loaded content.
 */
function maskTrackingNumbers() {
  var observer = new MutationObserver(function () {
    var selectors = ['div.ng-binding', 'td.ng-binding'];
    selectors.forEach(function (sel) {
      document.querySelectorAll(sel).forEach(function (el) {
        var text = el.textContent.trim();
        if (text.length === 18) {
          el.textContent = text.substring(0, 3) + 'XXXXXX' + text.substring(9);
        }
      });
    });
  });

  observer.observe(document.body, { childList: true, subtree: true });
}

// ---------------------------------------------------------------------------
// Print restriction by age (Harley Davidson)
// ---------------------------------------------------------------------------

/**
 * Sets `CantPrint = true` on packages shipped more than the specified
 * number of days ago, preventing reprinting.
 *
 * @param {object[]} packages - The history package results.
 * @param {number}   maxDays  - Maximum age in days before printing is blocked.
 */
function restrictPrintByAge(packages, maxDays) {
  var now = new Date();
  packages.forEach(function (pkg) {
    var shipDate = new Date(pkg.Shipdate.Year, pkg.Shipdate.Month - 1, pkg.Shipdate.Day);
    var diffDays = Math.floor(Math.abs(shipDate - now) / (1000 * 60 * 60 * 24));
    if (diffDays > maxDays) {
      pkg.CantPrint = true;
    }
  });
}

// ---------------------------------------------------------------------------
// History date-range enforcement (Harley Davidson)
// ---------------------------------------------------------------------------

/**
 * Sets the history start date to N days ago and updates the search criteria
 * to match.
 *
 * @param {object} vm             - The view model (vm.dtstart is updated).
 * @param {object} searchCriteria - The search criteria with WhereClauses.
 * @param {number} daysBack       - Number of days to look back.
 */
function enforceHistoryDateRange(vm, searchCriteria, daysBack) {
  var startDate = new Date();
  startDate.setDate(startDate.getDate() - daysBack);
  vm.dtstart = startDate;

  searchCriteria.WhereClauses.forEach(function (w) {
    if (w.FieldName === 'Shipdate' && w.Operator === 3) {
      w.FieldValue = startDate.toISOString().slice(0, 10);
    }
  });
}

// ---------------------------------------------------------------------------
// Branch-filtered history (Edward Jones)
// ---------------------------------------------------------------------------

/**
 * Adds a branch-number filter to history search criteria based on
 * the user's CustomData.
 *
 * @param {object}   searchCriteria - The search criteria with WhereClauses.
 * @param {string}   customDataKey  - The key to look up (e.g. "Custom3").
 * @param {object[]} customData     - The user's CustomData array.
 * @param {function} getValueByKey  - Lookup function: (key, array) => value.
 */
function filterHistoryByBranch(searchCriteria, customDataKey, customData, getValueByKey) {
  var branchNo = getValueByKey(customDataKey, customData);
  if (branchNo) {
    searchCriteria.WhereClauses.push({
      FieldName: 'MiscReference20',
      FieldValue: branchNo,
      Operator: 5
    });
  }
}

// ---------------------------------------------------------------------------
// User-scoped history
// ---------------------------------------------------------------------------

/**
 * Adds a UserId filter to history search criteria, restricting results to
 * the current user's shipments.
 *
 * @param {object} searchCriteria - The search criteria with WhereClauses.
 * @param {string} userId         - The current user's ID.
 * @param {number} [operator=0]   - The search operator (0 = equals, 5 = contains).
 */
function filterHistoryByUser(searchCriteria, userId, operator) {
  searchCriteria.WhereClauses.push({
    FieldName: 'UserId',
    FieldValue: userId,
    Operator: operator || 0
  });
}

// ---------------------------------------------------------------------------
// Tracking links (General Insulation)
// ---------------------------------------------------------------------------

/**
 * Adds clickable tracking links to the detailed history table.
 * UPS tracking numbers (starting with "1Z") link to ups.com;
 * others link to CH Robinson.
 */
function addTrackingLinks() {
  var interval = setInterval(function () {
    if ($('table[ng-table="vm.tableParamsForDetailed"] tr').length <= 1) return;
    clearInterval(interval);

    $('table[ng-table="vm.tableParamsForDetailed"] tr:gt(1)')
      .find('td:eq(2) > div:not(.ng-hide)')
      .html(function () {
        var trackingNo = $(this).text().trim();
        var baseUrl = trackingNo.startsWith('1Z')
          ? 'https://wwwapps.ups.com/WebTracking/track?track=yes&trackNums='
          : 'https://online.chrobinson.com/tracking/#/?trackingNumber=';
        return '<a href="' + baseUrl + trackingNo + '" target="_new">' + trackingNo + '</a>';
      });
  }, 100);
}
