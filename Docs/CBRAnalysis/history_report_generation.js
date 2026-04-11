/**
 * History Report Generation — Summary reports, close-all-shippers manifest
 * operations, and history-based shipment duplication.
 *
 * See also: close_manifest_processing.js for shared CloseAllShippers logic,
 *           history_report_filtering.js for PreSearchHistory filters.
 */

// ---------------------------------------------------------------------------
// Summary report
// ---------------------------------------------------------------------------

/**
 * Initialises the history page, restricts reports for non-admin profiles,
 * and handles summary-report generation via a UserMethod call.
 *
 * @param {object}   vm          - The view model.
 * @param {object}   thinClient  - Thin-client service with httpClient, adminProfileName, loadData.
 */
function initHistoryPage(vm, thinClient) {
  vm.history = {
    searchButton:   'button:contains("Search")',
    reportSelector: '#reports'
  };

  $(document).on('click', vm.history.searchButton, onReportChange);
  $(document).on('change', vm.history.reportSelector, onReportChange);

  restrictReportsForNonAdmin();

  function restrictReportsForNonAdmin() {
    if (vm.profile.Name === thinClient.adminProfileName) return;

    vm.hideButtonExport = true;
    var allowedReports = ['Default', 'Detailed Report', 'Void Report', 'Summary Report'];
    vm.reports = vm.reports.filter(function (report) {
      report.TemplateHtml = report.TemplateHtml.replace(/<span.*glyphicon-search.*<\/span>/g, '');
      return allowedReports.includes(report.Name);
    });
  }

  function onReportChange() {
    if (vm.report?.Name !== 'Summary Report') {
      vm.hideButtonExport = false;
      return;
    }

    vm.hideButtonExport = true;
    vm.history.carrierAcceptTableContainer = {
      height:    $('.panel-body').height() + 'px',
      overflowY: 'auto'
    };

    var payload = {
      Action: 'summaryReport',
      Data: {
        startDate: vm.dtstart.toLocaleDateString('en-CA'),
        endDate:   vm.dtend.toLocaleDateString('en-CA'),
        sites:     getSites()
      }
    };
    var data = { UserContext: undefined, Data: JSON.stringify(payload) };

    thinClient.httpClient('UserMethod', data).then(function (ret) {
      vm.history.carrierAcceptReport = JSON.parse(atob(ret.Data));
      thinClient.loadData.show();
    });
  }

  function getSites() {
    var sites = vm.UserInformation.SiteId
      ? vm.sites.filter(function (s) { return s.Id === vm.UserInformation.SiteId; })
      : vm.sites;
    return sites.map(function (s) { return { name: s.Name }; });
  }
}

// ---------------------------------------------------------------------------
// History-based shipment duplication
// ---------------------------------------------------------------------------

/**
 * Enables a "duplicate" action on history rows that clones a previous
 * shipment into the shipping view for re-use.
 *
 * @param {object} cbr - The client business rules context with `historyShipment` and `bulk.create`.
 */
function enableHistoryDuplicate(cbr) {
  $('body').delegate('table a[action-duplicate]', 'click', function () {
    cbr.historyShipment = $(this).attr('action-duplicate');
    document.location = '#!/shipping';
  });
}

/**
 * On NewShipment, restores a duplicated history shipment and configures
 * return-delivery settings for all packages.
 *
 * @param {object} cbr             - The CBR context.
 * @param {object} vm              - The view model.
 * @param {object} shipmentRequest - The current shipment request.
 * @param {string} defaultDescription - Description to set on each package (e.g. "PC Equipment").
 */
function applyHistoryDuplicate(cbr, vm, shipmentRequest, defaultDescription) {
  if (cbr.historyShipment === null) return;

  var historyRequest = JSON.parse(cbr.historyShipment);
  vm.totalPackages = historyRequest.Packages.length;

  shipmentRequest.PackageDefaults = historyRequest.PackageDefaults;
  shipmentRequest.Packages        = historyRequest.Packages;
  shipmentRequest.PackageDefaults.Description = defaultDescription;

  shipmentRequest.Packages.forEach(function (pkg) {
    pkg.ReturnDelivery             = true;
    pkg.ReturnDeliveryMethod       = 4;
    pkg.ReturnDeliveryAddressEmail = historyRequest.PackageDefaults.Consignee.Email;
    pkg.Description                = defaultDescription;
  });

  cbr.historyShipment = null;
  cbr.bulk.create.itemReferences = [];
}
