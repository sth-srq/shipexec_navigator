/**
 * D2M (Direct-to-Mobile) — Manifest close-out for all shippers / carriers,
 * and D2M checkbox visibility management.
 */

/**
 * Closes manifests for all shippers under a single selected carrier.
 *
 * @param {object}   clientService - Service with config.ApiUrl, authorizationToken().
 * @param {object[]} allShippers - Array of shipper objects with Id, Symbol.
 * @param {string}   selectedCarrierSymbol - The selected carrier symbol.
 * @param {string}   companyId - The current company ID.
 * @param {function} showAlert - Function to display a result modal.
 */
function closeAllShippers(clientService, allShippers, selectedCarrierSymbol, companyId, showAlert) {
  var authToken = clientService.authorizationToken();
  var lastResponse;

  allShippers.forEach(function (shipper) {
    var carrierParams = { ShipperId: shipper.Id, CompanyId: companyId };
    var carrierResult = $.post({ url: clientService.config.ApiUrl + '/api/ShippingService/GetShipperCarriers', data: carrierParams, async: false, headers: authToken }).responseJSON;

    if (!carrierResult.Carriers || carrierResult.Carriers.length === 0) return;

    var manifestParams = {
      Carrier: selectedCarrierSymbol,
      Shipper: shipper.Symbol,
      SearchCriteria: { OrderByClauses: [{ FieldName: 'ShipDate', Direction: 'DESC' }] },
      IncludeImported: false,
      CompanyId: companyId
    };
    var manifestResult = $.post({ url: clientService.config.ApiUrl + '/api/ShippingService/GetManifestItems', data: manifestParams, async: false, headers: authToken }).responseJSON;

    if (!manifestResult.ManifestItems || manifestResult.ManifestItems.length === 0) return;

    var items = manifestResult.ManifestItems.map(function (m) {
      return { Attributes: m.Attributes, ShipDate: m.ShipDate, Symbol: m.Symbol, Name: m.Name };
    });

    var closeParams = {
      Carrier: selectedCarrierSymbol,
      ManifestItems: items,
      Shipper: shipper.Symbol,
      Print: true,
      UserParams: {},
      CompanyId: companyId
    };
    lastResponse = $.post({ url: clientService.config.ApiUrl + '/api/ShippingService/CloseManifest', data: closeParams, async: false, headers: authToken }).responseJSON;
  });

  displayCloseManifestResult(lastResponse, showAlert, false);
  $('#loaderspinnerImg').hide();
}

/**
 * Closes manifests across all shippers and all carriers.
 * Same workflow as closeAllShippers but iterates carriers as well.
 */
function closeAllShippersAndCarriers(clientService, allShippers, allCarriers, companyId, showAlert) {
  var lastResponse;

  allCarriers.forEach(function (carrier) {
    closeAllShippers(clientService, allShippers, carrier.Symbol, companyId, function () {});
  });

  /* In practice the response from the last iteration is used for the result display */
  displayCloseManifestResult(lastResponse, showAlert, true);
  $('#loaderspinnerImg').hide();
}

/**
 * Displays the appropriate success or error message after closing manifests.
 *
 * @param {object|undefined} response - The server response.
 * @param {function} showAlert - Function to show a named modal.
 * @param {boolean} isAllCarriers - True when closing across all carriers.
 */
function displayCloseManifestResult(response, showAlert, isAllCarriers) {
  var errorSelector   = isAllCarriers ? '#divAllshippersAndCarriersErrorMsg' : '#divAllshippersErrorMsg';
  var failSelector    = isAllCarriers ? '#divAllshipperAndCarrierssunsuccessfulcloseoutMsg' : '#divAllshippersunsuccessfulcloseoutMsg';
  var successSelector = isAllCarriers ? '#divsuccessfulAllShippersAndAllCarriers' : '#divsuccessfulAllShippers';

  /* Hide all */
  $('#divAllshippersErrorMsg, #divAllshippersunsuccessfulcloseoutMsg, #divAllshippersAndCarriersErrorMsg, #divAllshipperAndCarrierssunsuccessfulcloseoutMsg, #divsuccessfulAllShippers, #divsuccessfulAllShippersAndAllCarriers').hide();

  if (!response || response.ErrorCode === -1) {
    $(errorSelector).show();
    showAlert('ErrorModalCloseShippers');
  } else if (response.CloseManifestResults[0].ErrorCode !== 0) {
    $(failSelector).show();
    showAlert('ErrorModalCloseShippers');
  } else {
    $(successSelector).show();
    showAlert('successfulModalCloseShippers');
  }
}
