/**
 * Auto Ship — Service auto-selection based on package weight,
 * D2M (Direct-to-Mobile) shipping enablement, and notification email sync.
 *
 * Patterns found:
 *   - Weight-based automatic service selection (express vs freight)
 *   - D2M shipping enablement via UserMethod API call
 *   - Filtered service option lists based on custom shipping codes
 *   - Notification email synced across PreShip/PreRate/PostRate/PostSelectAddressBook
 */

/**
 * Automatically selects a shipping service based on package weights.
 * Packages >= 70 lbs route to freight; otherwise express saver.
 * Throws if a mixed-weight shipment cannot use a single service.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {string} expressSymbol - Symbol for the express service (e.g. "CONNECTSHIP_UPS.UPS.EXPSVR").
 * @param {string} freightSymbol - Symbol for the freight service (e.g. "CONNECTSHIP_UPS.UPS.EXPFRT").
 */
function autoSelectServiceByWeight(shipmentRequest, expressSymbol, freightSymbol) {
  var hasFreight = false;
  var hasMixed   = false;

  for (var i = 0; i < shipmentRequest.Packages.length; i++) {
    var weight = parseInt(shipmentRequest.Packages[i].Weight.Amount, 10);
    if (weight >= 70) {
      hasFreight = true;
    } else if (hasFreight) {
      hasMixed = true;
    }
  }

  if (!hasFreight) {
    shipmentRequest.PackageDefaults.Service = { Symbol: expressSymbol };
  } else if (!hasMixed) {
    shipmentRequest.PackageDefaults.Service = { Symbol: freightSymbol };
  } else {
    alert('Cannot create one shipment with 2 different service levels.');
    return;
  }

  for (var j = 0; j < shipmentRequest.Packages.length; j++) {
    if (shipmentRequest.PackageDefaults.Service.Symbol === freightSymbol) {
      shipmentRequest.Packages[j].Packaging = 'CUSTOMER_PALLET';
    }
    shipmentRequest.Packages[j].ImportDelivery = true;
  }
}

/**
 * Enables Direct-to-Mobile shipping by calling the server UserMethod.
 *
 * @param {function} thinClientAPIRequest - The API request function.
 * @param {object}   userContext - The current user context.
 * @param {function} decodeReturnString - Function to decode base64 server data.
 */
function enableD2MShipping(thinClientAPIRequest, userContext, decodeReturnString) {
  var params   = { ActionMessage: 'EnableD2MShipping' };
  var data     = { Data: JSON.stringify(params), UserContext: userContext };
  var response = thinClientAPIRequest('UserMethod', data, false);

  if (response && response.responseJSON.ErrorCode !== 0) {
    alert('Error while executing UserMethod: ' + response.responseJSON.ErrorCode);
  } else {
    vm.profile = decodeReturnString(response.responseJSON.Data);
  }
}

/**
 * Builds a filtered service list from a comma-separated string of custom
 * shipping codes, mapping each code to a carrier symbol.
 *
 * @param {object}   profile - The user's profile containing Services[].
 * @param {string}   serviceCodesCSV - Comma-separated custom codes (e.g. "2dn,grn,nda").
 * @param {function} getServiceSymbol - Maps a code to a carrier symbol string.
 * @returns {object[]} Filtered service objects.
 */
function buildFilteredServiceList(profile, serviceCodesCSV, getServiceSymbol) {
  var filtered = [];
  var codes    = serviceCodesCSV.split(',');

  codes.forEach(function (code) {
    var symbol = getServiceSymbol(code.trim().toLowerCase());
    if (!symbol) return;

    for (var i = 0; i < profile.Services.length; i++) {
      if (profile.Services[i].Symbol === symbol) {
        filtered.push(profile.Services[i]);
        break;
      }
    }
  });

  return filtered;
}
