/**
 * Loading — PageLoaded routing, NewShipment initialisation, D2M element
 * hiding, and shipping profile CRUD (save / delete / change / EOD).
 *
 * See also: shipping_view_initialization.js for SetCurrentViewModel,
 *           shipping_profiles.js (if separated) for AllCare-style CRUD.
 */

// ---------------------------------------------------------------------------
// PageLoaded routing
// ---------------------------------------------------------------------------

/**
 * Routes PageLoaded to the correct initialisation function based on the
 * current hash location.
 *
 * @param {string} location - The current route (e.g. "/shipping", "/history", "/batchdetail").
 * @param {object} vm       - The view model.
 * @param {object} [options] - Optional overrides: { sortShippers, initD2M, initShipping }.
 */
function onPageLoaded(location, vm, options) {
  options = options || {};

  if (location === '/shipping') {
    if (options.sortShippers) {
      vm.profile.Shippers.sort(compareByKey('Name'));
    }
    if (typeof options.initShipping === 'function') {
      options.initShipping();
    }
  }
}

/**
 * Hides the D2M template checkbox once the shipping date field has loaded.
 *
 * @param {string} d2mTemplateId - The element ID of the D2M template shipment checkbox.
 */
function hideD2MCheckbox(d2mTemplateId) {
  var interval = setInterval(function () {
    var dateIsLoaded = $("[ng-model='vm.startshipdate.value']").val();
    if (dateIsLoaded) {
      clearInterval(interval);
      $('#' + d2mTemplateId).hide();
    }
  }, 50);
}

// ---------------------------------------------------------------------------
// NewShipment initialisation
// ---------------------------------------------------------------------------

/**
 * Binds the Tab key on the Consignee Name Address field to trigger an
 * address-book search instead of the default tab behaviour.
 */
function bindConsigneeTabSearch() {
  $(document).ready(function () {
    $('[id="Consignee Name Address"]').on('keydown', 'input[name="code"]', function (e) {
      var keyCode = e.keyCode || e.which;
      if (keyCode === 9) {
        e.preventDefault();
        $('[caption="Consignee Name Address"]')
          .find('button[ng-click="search(nameaddress)"]')
          .click();
      }
    });
  });
}

// ---------------------------------------------------------------------------
// Shipping Profile CRUD (AllCare / generic pattern)
// ---------------------------------------------------------------------------

/**
 * Loads shipping profiles from the server via UserMethod.
 *
 * @param {object}   httpClient - The thin-client HTTP client function.
 * @param {function} callback   - Called with the parsed profile list.
 */
function loadShippingProfiles(httpClient, callback) {
  var payload = { Action: 'L', ShippingProfileName: '' };
  var data = { Data: JSON.stringify(payload) };

  httpClient('UserMethod', data).then(function (ret) {
    var profiles = JSON.parse(atob(ret.Data));
    callback(profiles);
  });
}

/**
 * Saves or deletes a shipping profile.
 *
 * @param {string}   action          - "S" for save, "D" for delete.
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {string}   profileName     - The shipping profile name.
 * @param {string}   [selService]    - The selected service symbol (only needed for save).
 * @param {object}   httpClient      - The thin-client HTTP client function.
 * @param {function} callback        - Called with the updated profile list.
 */
function saveOrDeleteShippingProfile(action, shipmentRequest, profileName, selService, httpClient, callback) {
  shipmentRequest.Action = action;
  shipmentRequest.ShippingProfileName = profileName;
  if (action === 'S') shipmentRequest.SelService = selService;

  var data = { Data: JSON.stringify(shipmentRequest) };
  httpClient('UserMethod', data).then(function (ret) {
    var profiles = JSON.parse(atob(ret.Data));
    callback(profiles);
  });
}

/**
 * Applies a saved shipping profile to the current shipment.
 *
 * @param {object}   vm              - The view model.
 * @param {object[]} shippingProfiles - The list of saved profiles.
 * @param {string}   profileName     - The name of the profile to apply.
 */
function applyShippingProfile(vm, shippingProfiles, profileName) {
  var template = shippingProfiles.find(function (p) {
    return p.ShippingProfileName === profileName;
  });
  if (!template) return;

  var profile = structuredClone(template);
  var service = { Symbol: profile.SelService };

  if (profile.Packages[0].ProactiveRecovery === true) {
    profile.Packages[0].SelectedProactiveRecoveryInstructions = [4096, 2048, 32];
  }

  var today   = new Date();
  var curdate = { Year: today.getFullYear(), Month: today.getMonth() + 1, Day: today.getDate() };
  profile.PackageDefaults.Shipdate = curdate;

  vm.currentShipment    = profile;
  vm.selectedServices   = vm.selectedServices || [];
  vm.selectedServices[0] = service;
}

/**
 * Creates a return label from the previous shipment by cloning it and
 * setting return-delivery options.
 *
 * @param {object} vm - The view model (must have lastShipmentRequest).
 */
function createReturnLabelFromPrevious(vm) {
  vm.currentShipment = structuredClone(vm.lastShipmentRequest);

  var pkg = vm.currentShipment.Packages[0];
  pkg.ReturnDelivery        = true;
  pkg.ReturnDeliveryMethod  = 0;
  pkg.ProactiveRecovery     = false;
  pkg.DirectDelivery        = false;
  pkg.Proof                 = false;
  pkg.ProofRequireSignature = false;

  vm.currentShipment.PackageDefaults.Service = { Symbol: 'CONNECTSHIP_UPS.UPS.2DA' };
  if (!vm.selectedServices) vm.selectedServices = [];
  vm.selectedServices.push(vm.currentShipment.PackageDefaults.Service);
}

/**
 * Processes End-of-Day via UserMethod.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {object}   httpClient      - The thin-client HTTP client function.
 * @param {jQuery}   $modal          - The EOD modal to hide on completion.
 */
function processEOD(shipmentRequest, httpClient, $modal) {
  shipmentRequest.Action = 'EOD';
  var data = { Data: JSON.stringify(shipmentRequest) };
  httpClient('UserMethod', data).then(function () {
    $modal.modal('hide');
  });
}
