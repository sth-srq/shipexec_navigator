/**
 * Service Filtering — D2M shipper/user authorization checks and
 * weight-based service auto-selection.
 *
 * Patterns found:
 *   - D2M: Check if shipper is authorized for D2M shipping
 *   - D2M: Check if user has D2M enabled in custom data
 *   - D2M: Combine checks for current shipment and session
 *   - Asia Waterjet: Auto-select UPS Express Saver vs Express Freight
 *     based on package weight (70 lb threshold)
 *
 * See also: d2m.js for the full D2M workflow.
 */

// ---------------------------------------------------------------------------
// D2M authorization checks
// ---------------------------------------------------------------------------

/**
 * Checks whether the currently selected shipper is authorized for D2M.
 *
 * @param {object[]} authorizedShippers - Array of { ShipperSymbol } objects.
 * @returns {boolean} True if the current shipper is in the authorized list.
 */
function isCurrentShipperD2MEnabled(authorizedShippers) {
  var currentSymbol = $('select[ng-change="vm.shipperChange()"]')
    .val().substring(7).toUpperCase();

  return authorizedShippers.some(function (s) {
    return s.ShipperSymbol === currentSymbol;
  });
}

/**
 * Checks whether the current user has D2M enabled via their custom data.
 *
 * @param {object[]} customData        - The user's CustomData array.
 * @param {string}   d2mCustomDataKey  - The key to check (e.g. "D2M").
 * @param {string}   enabledValue      - The value that indicates enabled (e.g. "YES").
 * @returns {boolean}
 */
function isCurrentUserD2MEnabled(customData, d2mCustomDataKey, enabledValue) {
  if (!customData) return false;

  for (var i = 0; i < customData.length; i++) {
    if (customData[i].Key === d2mCustomDataKey) {
      return customData[i].Value.toUpperCase() === enabledValue.toUpperCase();
    }
  }
  return false;
}

/**
 * Checks whether the current shipment qualifies as a D2M shipment
 * (user enabled + checkbox checked + shipper authorized).
 *
 * @param {object}   config - { customData, d2mKey, enabledValue, authorizedShippers, checkboxId }
 * @returns {boolean}
 */
function isCurrentShipmentD2M(config) {
  var userOk    = isCurrentUserD2MEnabled(config.customData, config.d2mKey, config.enabledValue);
  var checked   = $('#' + config.checkboxId).is(':checked');
  var shipperOk = isCurrentShipperD2MEnabled(config.authorizedShippers);
  return userOk && checked && shipperOk;
}

// ---------------------------------------------------------------------------
// Weight-based service auto-selection (Asia Waterjet)
// ---------------------------------------------------------------------------

/**
 * Automatically selects a service based on package weights. If any package
 * is >= the freight threshold, UPS Express Freight is selected; otherwise
 * UPS Express Saver. Throws if packages have mixed eligibility.
 *
 * @param {object} shipmentRequest       - The current shipment request.
 * @param {number} freightThresholdLbs   - Weight threshold in pounds (e.g. 70).
 * @param {string} expressServiceSymbol  - Service symbol for small packages.
 * @param {string} freightServiceSymbol  - Service symbol for freight.
 */
function autoSelectServiceByWeight(shipmentRequest, freightThresholdLbs, expressServiceSymbol, freightServiceSymbol) {
  var hasFreight   = false;
  var hasMixed     = false;

  shipmentRequest.Packages.forEach(function (pkg) {
    var weight = parseInt(pkg.Weight.Amount);
    if (weight >= freightThresholdLbs) {
      if (hasFreight === false && hasMixed === false) hasFreight = true;
    } else if (hasFreight) {
      hasMixed = true;
    }
  });

  if (hasMixed) {
    alert('Cannot create one shipment with 2 different service levels.');
    return;
  }

  shipmentRequest.PackageDefaults.Service = {
    Symbol: hasFreight ? freightServiceSymbol : expressServiceSymbol
  };

  if (hasFreight) {
    shipmentRequest.Packages.forEach(function (pkg) {
      pkg.Packaging = 'CUSTOMER_PALLET';
    });
  }
}

// ---------------------------------------------------------------------------
// Custom batch processing trigger (ASEA)
// ---------------------------------------------------------------------------

/**
 * Binds a custom batch-processing button click to a UserMethod call.
 *
 * @param {object} client - The thin-client service with config, userContext, getAuthorizationToken.
 */
function bindCustomBatchProcessing(client) {
  $('#btnCustomBatchProcessing').on('click', function () {
    var params = { ActionMessage: 'CustomBatchProcessing' };
    var data   = { Data: JSON.stringify(params), UserContext: client.userContext };
    $.post({
      url:     client.config.ShipExecServiceUrl + '/UserMethod',
      data:    data,
      async:   false,
      headers: client.getAuthorizationToken()
    });
  });
}
