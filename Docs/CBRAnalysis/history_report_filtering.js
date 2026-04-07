/**
 * History Report Filtering — Pre-search filters and bulk filter UI for
 * batch detail items.
 */

/**
 * Filters history results to the current user's records.
 * See batch_voiding_and_history_operations.js: filterHistoryByUser()
 */

/**
 * Applies a bulk filter to batch detail table rows, hiding rows that
 * don't match the filter criteria.
 *
 * @param {object} filterCriteria - Key/value pairs where keys are column titles.
 */
function applyBulkFilter(filterCriteria) {
  $('#tableBatchDetailItems tr:gt(0)').each(function () {
    var $tr = $(this);
    for (var key in filterCriteria) {
      var filterValue = filterCriteria[key].trim().toUpperCase();
      if (filterValue === '') continue;

      var cellText = $tr.find('td[title="' + key + '"]').text().trim().toUpperCase();
      if (filterValue !== cellText) {
        $tr.fadeOut('fast');
      }
    }
  });
}

/**
 * Clears any active bulk filter, showing all hidden rows and resetting
 * filter input fields.
 */
function clearBulkFilter() {
  $('#tableBatchDetailItems tr:hidden').fadeIn('fast');
  $('div[title="Filter"] input[type="text"]').each(function () {
    $(this).val('').change();
  });
}

/**
 * Sets up the history page for a summary report with custom search
 * and report-change handlers.
 *
 * @param {object}   vmInstance - The view model.
 * @param {function} httpClient - The HTTP client function.
 */
function initHistoryReports(vmInstance, httpClient) {
  vmInstance.history = {
    searchButton:   'button:contains("Search")',
    reportSelector: '#reports'
  };

  $(document).on('click', vmInstance.history.searchButton, handleReportChange);
  $(document).on('change', vmInstance.history.reportSelector, handleReportChange);

  function handleReportChange() {
    if (vmInstance.report?.Name !== 'Summary Report') {
      vmInstance.hideButtonExport = false;
      return;
    }

    vmInstance.hideButtonExport = true;

    var payload = {
      Action: 'summaryReport',
      Data: {
        startDate: vmInstance.dtstart.toLocaleDateString('en-CA'),
        endDate:   vmInstance.dtend.toLocaleDateString('en-CA'),
        sites:     getSites()
      }
    };
    var data = { UserContext: undefined, Data: JSON.stringify(payload) };

    httpClient('UserMethod', data).then(function (ret) {
      vmInstance.history.carrierAcceptReport = JSON.parse(atob(ret.Data));
    });
  }

  function getSites() {
    var sites = vmInstance.UserInformation.SiteId
      ? vmInstance.sites.filter(function (s) { return s.Id === vmInstance.UserInformation.SiteId; })
      : vmInstance.sites;
    return sites.map(function (s) { return { name: s.Name }; });
  }
}
