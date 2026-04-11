// ============================================================================
// All.js - Combined CBR Analysis JavaScript
// Generated: 2026-04-07 08:56:14
// Files: 48
// ============================================================================

// ============================================================================
// FILE: address_book_logic.js
// ============================================================================

/**
 * Address Book Logic — Consignee matching, address book search triggers,
 * and reference updates after an address book selection.
 *
 * Patterns found:
 *   - Tab-key triggers address book search on the Consignee code field (AAFES variants)
 *   - Consignee address comparison for consolidated order validation
 *   - Updating ShipperReference / MiscReference7 with order numbers
 */

// ---------------------------------------------------------------------------
// Tab-key address book search
// ---------------------------------------------------------------------------

/**
 * On NewShipment, binds a Tab-key handler to the Consignee address code
 * input so that pressing Tab triggers the address book search button.
 */
function bindConsigneeTabSearch() {
  $(document).ready(function () {
    var $consignee = $('[id="Consignee Name Address"]');
    $consignee.on('keydown', $consignee.find("input[name='code']"), function (e) {
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
// Consignee address comparison
// ---------------------------------------------------------------------------

/**
 * Compares the current shipment consignee address against an incoming
 * customer order object.  Used to validate that consolidated orders are
 * going to the same destination.
 *
 * @param {object} customerOrder - Order with shipAddress1, shipCity, shipState, shipZipCode.
 * @returns {boolean} True when the addresses match.
 */
function checkForMatchingConsignee(customerOrder) {
  try {
    var $con = $('name-address[nameaddress="vm.currentShipment.PackageDefaults.Consignee"]');

    var currentAddress = {
      address1: $con.find('input[name="address1"]').val().trim().toUpperCase(),
      city:     $con.find('input[name="city"]').val().trim().toUpperCase(),
      state:    $con.find('input[name="stateProvince"]').val().trim().toUpperCase(),
      zip:      $con.find('input[name="postalCode"]').val().trim().substring(0, 5)
    };

    var orderAddress = {
      address1: customerOrder.shipAddress1.trim().toUpperCase(),
      city:     customerOrder.shipCity.trim().toUpperCase(),
      state:    customerOrder.shipState.trim().toUpperCase(),
      zip:      customerOrder.shipZipCode.trim().substring(0, 5)
    };

    var currentJson = JSON.stringify(currentAddress).replaceAll(' ', '');
    var orderJson   = JSON.stringify(orderAddress).replaceAll(' ', '');

    if (currentJson !== orderJson) {
      Logger.Log({ Source: 'checkForMatchingConsignee()', Message: 'Address mismatch for order ' + customerOrder.orderNumber, Data: { currentAddress: currentAddress, orderAddress: orderAddress } });
    }

    return currentJson === orderJson;
  } catch (error) {
    Logger.Log({ Source: 'checkForMatchingConsignee()', Error: error });
    return false;
  }
}

// ---------------------------------------------------------------------------
// Reference field updates
// ---------------------------------------------------------------------------

/**
 * Appends an order number to the ShipperReference and MiscReference7 fields,
 * keeping only unique comma-separated values.
 *
 * @param {string} orderNumber - The order number to append.
 */
function updateReferencesWithOrderNumber(orderNumber) {
  var $shipperRef = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]');
  var $miscRef7   = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].MiscReference7"]');

  var combined    = $shipperRef.val() + ',' + orderNumber;
  var uniqueCSV   = Tools.GetUniqueCSVStringFromString(combined);

  $shipperRef.val(uniqueCSV);
  $miscRef7.val(uniqueCSV);
}


// ============================================================================
// FILE: address_book_lookup_and_population.js
// ============================================================================

/**
 * Address Book Lookup and Population — Triggers address lookups via keyboard
 * events and populates the consignee fields from the address book.
 *
 * Patterns found:
 *   - Enter/focusout on code input triggers the consignee search button
 *   - Tab-key on Consignee code input triggers address lookup (AAFES)
 *   - CopyOriginAddressToReturnAddress from user profile
 *   - Populating ship notification email from the consignee email input
 */

/**
 * Binds Enter-key and focusout events to the consignee code input
 * to trigger the address book search.
 *
 * @param {string} codeInputSelector - jQuery selector for the code input.
 */
function bindCodeInputLookup(codeInputSelector) {
  $('body').delegate(codeInputSelector, 'keyup focusout', function (e) {
    var keycode = e.keyCode || e.which;
    if (keycode === 13 || e.type === 'focusout') {
      var conSection   = '[nameaddress="vm.currentShipment.PackageDefaults.Consignee"] ';
      var btnSearchAdd = conSection + 'button[ng-click="search(nameaddress)"]';
      $(btnSearchAdd).trigger('click');
    }
  });
}

/**
 * Copies the current user's origin address to the return address
 * on the shipment defaults.
 */
function copyOriginAddressToReturnAddress() {
  var originAddress = vm?.profile?.UserInformation?.Address;

  if (originAddress && !$.isEmptyObject(originAddress)) {
    vm.currentShipment.PackageDefaults.ReturnAddress = originAddress;
    console.log('User Origin Address copied to Return Address.');
  } else {
    console.log('Error: Origin Address not found.');
  }
}

/**
 * Reads the ship-notification email from the DOM input and writes it
 * to the first package in the shipment request.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function syncShipNotificationEmail(shipmentRequest) {
  var emailSelector = 'input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipNotificationAddressEmail"]';
  shipmentRequest.Packages[0].ShipNotificationAddressEmail = $(emailSelector).val();
}


// ============================================================================
// FILE: alerts.js
// ============================================================================

/**
 * Alerts — Display error and informational alerts to the user.
 *
 * Patterns found:
 *   - Simple lifecycle-hook alerts (PageLoaded, PreShip, PreRate, RemovePackage)
 *   - Bootstrap-style dismissible error banners (ShowErrorAlert)
 *   - Modal-based alerts with a title and message (ShowAlert)
 *   - PostLoad error display from server response
 */

// ---------------------------------------------------------------------------
// Bootstrap dismissible error banner
// ---------------------------------------------------------------------------

/**
 * Creates a Bootstrap-style dismissible error alert at the bottom of the page.
 *
 * @param {string} errorMessage - The message to display.
 * @param {function} getRandomId - A function that returns a unique element id.
 */
function showErrorAlert(errorMessage, getRandomId) {
  var divAlert = $('<div />')
    .attr('id', getRandomId())
    .addClass('alert alert-dismissible alert-bottom alert-danger ng-scope')
    .css({ 'z-index': 1550, cursor: 'pointer' })
    .attr('role', 'alert')
    .append(
      $('<button />')
        .attr('type', 'button')
        .addClass('close')
        .append($('<span />').attr('aria-hidden', 'false').text('x'))
        .append($('<span />').addClass('sr-only').text('Close'))
    )
    .append(
      $('<div />').append(
        $('<div />')
          .addClass('ng-scope alert-message')
          .text(errorMessage)
          .prepend(
            $('<span />')
              .css('padding-right', '10px')
              .addClass('glyphicon glyphicon-alert custom-error-icon')
          )
      )
    );

  $('body').append(divAlert.show());
}

// ---------------------------------------------------------------------------
// Modal-based alert
// ---------------------------------------------------------------------------

/**
 * Shows a modal alert with a title and message.
 *
 * @param {string} title - The alert title.
 * @param {string} message - The alert body text.
 * @param {object} $alertModal - A jQuery reference to the modal element.
 */
function showModalAlert(title, message, $alertModal) {
  vm.currentShipment.AlertTitle = title;
  vm.currentShipment.AlertMessage = message;
  $alertModal.modal('show');
}

// ---------------------------------------------------------------------------
// PostLoad error display
// ---------------------------------------------------------------------------

/**
 * After loading a shipment, checks for a server-side error code and alerts
 * the user if one is present.
 *
 * @param {object} shipmentRequest - The current shipment request object.
 */
function showPostLoadError(shipmentRequest) {
  if (shipmentRequest.PackageDefaults.ErrorCode === 1) {
    alert(shipmentRequest.PackageDefaults.ErrorMessage);
    shipmentRequest.PackageDefaults.ErrorCode = 0;
  }
}


// ============================================================================
// FILE: auto_ship.js
// ============================================================================

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


// ============================================================================
// FILE: batch_lookup.js
// ============================================================================

/**
 * Batch Lookup — Retrieve and select batches, assign custom batch events,
 * and pre-populate batch-related references.
 *
 * Patterns found:
 *   - GetBatches via thinClientAPIRequest
 *   - Selected batch name from a dropdown
 *   - Custom batch processing button click handler
 *   - Shipping profile change with batch/service selection
 */

/**
 * Retrieves the list of batches from the server.
 *
 * @param {function} apiRequest - The thinClientAPIRequest function.
 * @param {string}   companyId - The company ID to filter batches.
 * @returns {object[]} The list of batch objects (synchronous).
 */
function getBatches(apiRequest, companyId) {
  var batches;
  var data = { SearchCriteria: null, CompanyId: companyId };

  apiRequest('GetBatches', data, false).done(function (response) {
    batches = response.Batches;
  });

  return batches;
}

/**
 * Reads the currently selected batch name from the custom dropdown.
 *
 * @returns {string} The selected batch name with the "string:" prefix removed.
 */
function getSelectedBatchName() {
  return $('#cboBatches select option:selected').val().replace('string:', '');
}

/**
 * Binds a click handler to the custom batch processing button that
 * calls the server-side UserMethod.
 *
 * @param {object} clientConfig - Object with ShipExecServiceUrl property.
 * @param {object} userContext - The current user context.
 * @param {function} getAuthToken - Returns the authorization token header object.
 */
function assignCustomBatchProcessingEvent(clientConfig, userContext, getAuthToken) {
  $('#btnCustomBatchProcessing').on('click', function () {
    var params = { ActionMessage: 'CustomBatchProcessing' };
    var data   = { Data: JSON.stringify(params), UserContext: userContext };

    $.post({
      url:     clientConfig.ShipExecServiceUrl + '/UserMethod',
      data:    data,
      async:   false,
      headers: getAuthToken()
    });
  });
}


// ============================================================================
// FILE: batch_voiding_and_history_operations.js
// ============================================================================

/**
 * Batch Voiding and History Operations — PreSearchHistory filters and
 * history-only data access.
 *
 * The vast majority of implementations are empty stubs.  The only meaningful
 * patterns are user-scoped history filtering.
 */

/**
 * Filters the history search so only the current user's shipments are shown.
 *
 * @param {object} searchCriteria - The search criteria object with a WhereClauses array.
 * @param {string} userId - The current user's ID.
 * @param {number} [operator=0] - The comparison operator (0 = equals, 5 = contains).
 */
function filterHistoryByUser(searchCriteria, userId, operator) {
  searchCriteria.WhereClauses.push({
    FieldName: 'UserId',
    FieldValue: userId,
    Operator: operator || 0
  });
}

/**
 * Returns the user context from whichever property is populated on the view model.
 *
 * @param {object} vmInstance - The current view model.
 * @returns {object|undefined} The user context object.
 */
function getUserContext(vmInstance) {
  return vmInstance.userContext || vmInstance.UserInformation;
}

/**
 * Generic comparator factory for sorting arrays of objects by a given key.
 *
 * @param {string} key - The property name to sort by.
 * @returns {function} A comparison function for Array.sort().
 */
function compareByKey(key) {
  return function (a, b) {
    if (a[key] > b[key]) return 1;
    if (a[key] < b[key]) return -1;
    return 0;
  };
}


// ============================================================================
// FILE: camera_capture.js
// ============================================================================

/**
 * Camera Capture — Use the device camera to capture a shipping label image.
 *
 * Patterns found:
 *   - Start a video stream with rear-facing camera at QHD resolution
 *   - Capture a frame as a base64 PNG
 *   - Stop the video stream
 *   - Zoom level management via localStorage
 */

/**
 * Starts the device camera video stream and attaches it to the given element.
 *
 * @param {string} videoElementId - The DOM id of the <video> element.
 * @param {object} videoModal - Object to store zoom/track state.
 */
function startVideoStream(videoElementId, videoModal) {
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    console.error('Your browser does not support the getUserMedia API.');
    return;
  }

  var constraints = {
    audio: false,
    video: {
      zoom: true,
      facingMode: 'environment',
      width:  { ideal: 2560 },
      height: { ideal: 1440 }
    }
  };

  navigator.mediaDevices.getUserMedia(constraints).then(function (stream) {
    var video        = document.getElementById(videoElementId);
    video.srcObject  = stream;

    var track        = stream.getVideoTracks()[0];
    var capabilities = track.getCapabilities();

    videoModal.zoom = videoModal.zoom || {
      min:     capabilities.zoom.min,
      max:     capabilities.zoom.max,
      current: track.getSettings().zoom
    };

    var savedZoom = localStorage.getItem('zoomLevel') || track.getSettings().zoom;
    localStorage.setItem('zoomLevel', savedZoom);
    track.applyConstraints({ advanced: [{ zoom: savedZoom }] });
    videoModal.track = track;
  }).catch(function (error) {
    console.error('Error accessing the camera:', error);
  });
}

/**
 * Captures the current video frame as a base64 PNG string.
 *
 * @param {string} videoElementId - The DOM id of the <video> element.
 * @returns {Promise<string>} Resolves with the base64-encoded image data (no prefix).
 */
function captureImage(videoElementId) {
  var video        = document.getElementById(videoElementId);
  var track        = video.srcObject.getVideoTracks()[0];
  var imageCapture = new ImageCapture(track);

  return imageCapture.grabFrame().then(function (imageBitmap) {
    var canvas = document.createElement('canvas');
    var ctx    = canvas.getContext('2d');
    canvas.width  = imageBitmap.width;
    canvas.height = imageBitmap.height;
    ctx.drawImage(imageBitmap, 0, 0);

    var dataUrl = canvas.toDataURL('image/png', 1.0);
    return dataUrl.replace(/^data:.+;base64,/, '');
  });
}

/**
 * Stops all tracks on the camera stream and releases the video element.
 *
 * @param {string}  videoElementId - The DOM id of the <video> element.
 * @param {boolean} isCanceled - When true, clears the captured label image.
 */
function stopVideoStream(videoElementId, isCanceled) {
  var video = document.getElementById(videoElementId);
  if (video.srcObject) {
    video.srcObject.getTracks().forEach(function (track) { track.stop(); });
    video.srcObject = null;
  }
}


// ============================================================================
// FILE: close_manifest_processing.js
// ============================================================================

/**
 * Close Manifest Processing — Consolidated order saving, EOD processing,
 * and close-manifest workflows.
 *
 * All individual PreCloseManifest / PostCloseManifest stubs were empty.
 * The meaningful code is consolidated order saving and EOD processing.
 */

/**
 * Gathers unprocessed order numbers from the consolidated orders dialog
 * and submits them to the server for processing.
 *
 * @param {function} makeUserMethodRequest - The Tools.MakeUserMethodRequest function.
 * @param {function} processCallback - Callback to handle the returned order data.
 */
function saveConsolidatedOrderNumbers(makeUserMethodRequest, processCallback) {
  try {
    var ordersToQuery = '';
    $('#selectConsolidatedOrders > option.not-processed').each(function () {
      ordersToQuery += $(this).text() + ',';
    });

    $('#divModalConsolidateShipments').hide();

    if (ordersToQuery.length > 0) {
      makeUserMethodRequest('GetOrderInformation', ordersToQuery, true, processCallback);
    } else {
      Tools.ShowErrorAlert('ERROR: No unique orders to pull from the server.');
    }
  } catch (error) {
    Logger.Log({ Source: 'saveConsolidatedOrderNumbers()', Error: error });
  }
}

/**
 * Processes order data returned from the server after a consolidated order request.
 * Validates each order's consignee matches the current shipment.
 *
 * @param {object[]} orders - Array of order objects from the server.
 */
function processConsolidatedOrderData(orders) {
  try {
    $(orders).each(function (index, order) {
      if (ConsolidatedOrders.CheckForMatchingConsignee(order)) {
        var currentPackage = Tools.GetCurrentPackage();
        if (!currentPackage) {
          Tools.ShowErrorAlert('ERROR: Could not add commodities for order ' + order.orderNumber);
          return false;
        }
        ConsolidatedOrders.UpdateReferencesWithOrderNumbers(order.orderNumber);
      } else {
        Tools.ShowErrorAlert('ERROR 500: Address mismatch for order ' + order.orderNumber);
      }
    });
  } catch (error) {
    console.log(error);
  }
}

/**
 * Closes the consolidated orders dialog without saving and hides the loader.
 */
function closeDialogWithoutSaving() {
  $('#selectConsolidatedOrders').empty();
  $('#divModalConsolidateShipments').hide();
  Tools.HideLoader();
}

/**
 * Submits an End-of-Day (EOD) processing request to the server.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {function} httpClient - The HTTP client function (e.g. client.httpClient).
 * @param {object}   $modal - jQuery reference to the EOD modal to hide on success.
 */
function processEndOfDay(shipmentRequest, httpClient, $modal) {
  shipmentRequest.Action = 'EOD';
  var data = { Data: JSON.stringify(shipmentRequest) };

  httpClient('UserMethod', data).then(function () {
    $modal.modal('hide');
  });
}


// ============================================================================
// FILE: commodity_assignment.js
// ============================================================================

/**
 * Commodity Assignment — Third-party billing based on origin/consignee
 * country, EU membership checks, and international commodity setup.
 */

/**
 * Determines whether third-party billing should be enabled based on the
 * origin and consignee countries, including EU intra-union logic.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {function} isMemberOfEu - Returns true if the country code is in the EU.
 */
function applyThirdPartyBillingRules(shipmentRequest, isMemberOfEu) {
  var shipFrom = shipmentRequest.PackageDefaults.OriginAddress.Country;
  var shipTo   = shipmentRequest.PackageDefaults.Consignee.Country;

  if (!shipTo || shipFrom === 'US' || shipFrom === 'CA') {
    disableThirdPartyBillingElements();
    return;
  }

  var shouldEnable = (shipFrom !== shipTo) ||
                     (isMemberOfEu(shipFrom) && isMemberOfEu(shipTo));

  if (shouldEnable) {
    setThirdPartyBilling(shipmentRequest);
  }

  if (shipFrom !== 'US') {
    $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].Weight.Units"]').val('string:KG').change();
    $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].Dimensions.Units"]').val('number:1').change();
  }
}

/**
 * Enables third-party billing and pre-fills the billing address.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function setThirdPartyBilling(shipmentRequest) {
  $('input[type=button][ng-disabled="!vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('disabled', false).attr('disabled', false);
  $('input[type=checkbox][ng-model="vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('checked', true).attr('checked', true).change();

  shipmentRequest.PackageDefaults.ThirdPartyBillingAddress = {
    Company:       'PRA Global',
    Address1:      '995 Research Park Blvd',
    Address2:      'Suite 300',
    City:          'Charlottesville',
    StateProvince: 'VA',
    PostalCode:    '22911',
    Account:       '883F79',
    Country:       'US'
  };
}

/**
 * Disables and un-checks the third-party billing checkbox and button.
 */
function disableThirdPartyBillingElements() {
  $('input[type=checkbox][ng-model="vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('checked', false).attr('checked', false);
  $('input[type=button][ng-disabled="!vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('disabled', true).attr('disabled', true);
}


// ============================================================================
// FILE: commodity_handling_and_international_shipment_preparation.js
// ============================================================================

/**
 * Commodity Handling and International Shipment Preparation —
 * Refreshing commodities in the UI, translating units of measurement,
 * and preparing D2M return delivery configurations.
 */

/**
 * Refreshes the commodity list in the UI by clicking the active tab
 * and toggling the ng-table page-size buttons.
 */
function refreshCommodityDisplay() {
  $('div.ui-tab-container > div.ng-isolate-scope > ul > li.active > a').click();
  $('#goods').find('div.ng-table-counts.btn-group').find('button:not(.active):first').click();
}

/**
 * Translates verbose unit-of-measurement strings to standard abbreviations.
 *
 * @param {string} value - The unit string (e.g. "EACH", "YARDS", "PAIR").
 * @returns {string} The abbreviated code (e.g. "EA", "YD", "PR").
 */
function translateUnitOfMeasurement(value) {
  var map = {
    'EACH':  'EA',
    'YARDS': 'YD',
    'YARD':  'YD',
    'METER': 'M',
    'SF':    'SFT',
    'PAIR':  'PR',
    'SR':    'ROL'
  };
  return map[value.toUpperCase()] || value;
}


// ============================================================================
// FILE: commodity_mapping.js
// ============================================================================

/**
 * Commodity Mapping — Build commodity content objects, display an
 * assign-goods slideout panel, and drag commodities between packages.
 */

/**
 * Creates a properly formatted commodity content object from raw order data.
 *
 * @param {object} data - Raw commodity data with skuId, quantity, unitPrice, weightLbs.
 * @returns {object} A CommodityContents-compatible object.
 */
function buildCommodityContentObject(data) {
  return {
    Quantity:             data.quantity,
    ProductCode:          data.skuId,
    QuantityUnitMeasure:  'EA',
    UnitValue:            { Currency: 'USD', Amount: +data.unitPrice },
    UnitWeight:           { Units: 'LB', Amount: +data.weightLbs },
    UniqueId:             data.UniqueId,
    PkgIndex:             data.PkgIndex || 1,
    PVTotalWeight:        +data.quantity * +data.weightLbs,
    PVTotalValue:         +data.quantity * +data.unitPrice
  };
}

/**
 * Shows the assign-goods slideout panel, populates it with commodity items,
 * and assigns all goods to the first package by default.
 *
 * @param {object}   context - The class instance with `packages`, `goods`, `viewModel`.
 * @param {object[]} rawGoods - Array of raw commodity data objects.
 */
function showAssignGoodsPane(context, rawGoods) {
  $('#listItemContainer li:gt(0)').remove();
  $('div.ui-tab-container').toggleClass('col-md-8 col-md-6')
    .siblings('div').toggleClass('col-md-4 col-md-6');

  context.packages = context.viewModel.currentShipment.Packages;
  context.goods    = rawGoods.sort(function (a, b) { return a.skuId - b.skuId; });

  $(context.goods).each(function (index, item) {
    var uniqueKey = 'e' + (Math.random() + 1).toString(36).substring(2);
    item.UniqueId = uniqueKey;

    var $item = $('#liClone').clone(true).data(item).attr('id', uniqueKey);
    $item.find('.goods-sku').html('SKU: <strong>' + item.skuId + '</strong>');
    $item.find('.goods-quantity').html('Quantity: <strong>' + item.quantity + '</strong>');
    $item.find('.goods-declared-value').html('Unit Cost: <strong>$' + item.unitPrice + '</strong>');

    $('#divAssignCommodities ul.list-group').append($item);

    var commodity = buildCommodityContentObject(item);
    context.packages[0].CommodityContents.push(commodity);

    var pkg = context.packages[0];
    pkg.Weight.Amount            = (pkg.Weight.Amount || 0) + commodity.PVTotalWeight;
    pkg.DeclaredValueAmount.Amount = (pkg.DeclaredValueAmount.Amount || 0) +
                                     Math.round(commodity.PVTotalValue * 100) / 100;
    $item.show();
  });

  $('#divAssignCommodities').show().animate({ left: 0 }, 400);
  $('.scan-input:visible').first().focus();
  $('#divAssignCommodities').find('[data-toggle="tooltip"]').tooltip();
}

/**
 * Moves a commodity from its current package to a destination package,
 * updating weights and declared values for both.
 *
 * @param {object}   context - The class instance with `packages`.
 * @param {jQuery}   $li - The list-item element representing the commodity.
 * @param {number}   destBox - 1-based destination package index.
 */
function moveGoodToBox(context, $li, destBox) {
  var data     = $li.data();
  var allGoods = context.packages.map(function (p) { return p.CommodityContents; }).flat();
  var item     = allGoods.find(function (cc) { return cc.UniqueId === data.UniqueId; });

  if (+item.PkgIndex === +destBox) {
    console.log('Item already in box.');
    return;
  }

  /* Remove from current box */
  var srcIndex = item.PkgIndex - 1;
  context.packages[srcIndex].CommodityContents =
    context.packages[srcIndex].CommodityContents.filter(function (p) { return p.UniqueId !== data.UniqueId; });

  /* Add to destination box */
  item.PkgIndex = destBox;
  (context.packages[destBox - 1].CommodityContents || []).push(item);

  $li.find('.pkg-index-text').fadeOut().promise().done(function () {
    $(this).text(destBox).fadeIn();
  });

  /* Recalculate weights and values */
  allGoods = context.packages.map(function (p) { return p.CommodityContents; }).flat();
  allGoods.sort(function (a, b) { return a.PkgIndex - b.PkgIndex; });

  $(allGoods).each(function (idx, cc) {
    var pkg = context.packages[cc.PkgIndex - 1];
    if (idx === 0 || cc.PkgIndex !== allGoods[idx - 1].PkgIndex) {
      pkg.Weight.Amount = 0;
      pkg.DeclaredValueAmount.Amount = 0;
    }
    pkg.Weight.Amount += +cc.PVTotalWeight;
    pkg.DeclaredValueAmount.Amount += +cc.PVTotalValue;
  });
}


// ============================================================================
// FILE: commodity_splitting.js
// ============================================================================

/**
 * Commodity Splitting — Add/remove commodities from specific packages
 * and refresh the commodity table.
 */

/**
 * Adds a commodity to a specific package's CommodityContents array
 * and refreshes the UI.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {number}   packageIndex - The 0-based package index.
 * @param {object}   commodity - The commodity object to add.
 * @param {function} refreshFn - Function to refresh the commodity display.
 */
function addCommodityToPackage(shipmentRequest, packageIndex, commodity, refreshFn) {
  try {
    if (!shipmentRequest.Packages[packageIndex].CommodityContents) {
      shipmentRequest.Packages[packageIndex].CommodityContents = [];
    }
    shipmentRequest.Packages[packageIndex].CommodityContents.push(commodity);
    refreshFn(packageIndex);
  } catch (error) {
    Logger.Log({ Source: 'addCommodityToPackage()', Error: error });
  }
}

/**
 * Removes the last commodity from a specific package's CommodityContents
 * array and refreshes the UI.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {number}   packageIndex - The 0-based package index.
 * @param {function} refreshFn - Function to refresh the commodity display.
 */
function removeCommodityFromPackage(shipmentRequest, packageIndex, refreshFn) {
  try {
    if (!shipmentRequest.Packages[packageIndex].CommodityContents) {
      shipmentRequest.Packages[packageIndex].CommodityContents = [];
    }
    shipmentRequest.Packages[packageIndex].CommodityContents.pop();
    refreshFn(packageIndex);
  } catch (error) {
    Logger.Log({ Source: 'removeCommodityFromPackage()', Error: error });
  }
}


// ============================================================================
// FILE: custom_batch_processing.js
// ============================================================================

/**
 * Custom Batch Processing — Consolidated order workflows and batch retrieval.
 * See also: close_manifest_processing.js and batch_lookup.js for shared logic.
 *
 * The unique code here is the consolidated order save/process flow,
 * already covered in close_manifest_processing.js.
 * This file re-exports references for clarity.
 */

// See close_manifest_processing.js:
//   saveConsolidatedOrderNumbers()
//   processConsolidatedOrderData()

// See batch_lookup.js:
//   getBatches()
//   getSelectedBatchName()

// See address_book_logic.js:
//   updateReferencesWithOrderNumber()


// ============================================================================
// FILE: customer_selection.js
// ============================================================================

/**
 * Customer Selection — Consolidated order dialog for selecting and adding
 * multiple orders to a single shipment.
 */

/**
 * Opens the consolidated order dialog, pre-populating with any existing
 * order numbers from the ShipperReference field.
 */
function showConsolidatedOrderDialog() {
  Tools.ShowOverlay();
  $('#selectConsolidatedOrders').empty();

  var shipperRefVal = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]').val();
  var existingOrders = Tools.GetUniqueArrayFromString(shipperRefVal);

  $(existingOrders).each(function (i, orderNum) {
    $('#selectConsolidatedOrders').append(
      $('<option />').val(orderNum).text(orderNum)
        .removeClass('not-processed').addClass('processed')
        .css('color', '#265ca1')
    );
  });

  $('#divModalConsolidateShipments').fadeIn('fast').promise().done(function () {
    $('#textOrderNumber').val([]).focus();
  });
}

/**
 * Adds the typed order number to the consolidated orders list, preventing
 * duplicates.
 *
 * @returns {boolean} false if the order already exists.
 */
function addOrderNumberToList() {
  var value   = $('#textOrderNumber').val();
  var exists  = false;

  $('#selectConsolidatedOrders option').each(function () {
    if (this.text === value) {
      exists = true;
      return false;
    }
  });

  if (exists || value.length === 0) {
    $('#textOrderNumber').val([]).focus();
    return false;
  }

  $('#selectConsolidatedOrders').append(
    $('<option />').val(value).text(value).addClass('not-processed').trigger('change')
  ).promise().done(function () {
    $('#textOrderNumber').val([]).focus();
  });
}


// ============================================================================
// FILE: d2m.js
// ============================================================================

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


// ============================================================================
// FILE: debugging.js
// ============================================================================

/**
 * Debugging — Logger utility with configurable server-side logging
 * and console output by log level.
 */

var LogLevel = { Error: 'Error', Info: 'Info', Debug: 'Debug', Trace: 'Trace', Fatal: 'Fatal' };
var serverLogging = false;

/**
 * Enables or disables server-side debug logging.
 *
 * @param {boolean} enabled - True to write log entries to the server.
 */
function setServerDebugMode(enabled) {
  console.log('Logging to server:', enabled);
  serverLogging = enabled;
}

/**
 * Logs a message to the console and optionally to the server.
 *
 * Usage examples:
 *   log({ Source: 'fn()', Error: error })
 *   log({ Source: 'fn()', Message: 'info text', Data: someObject })
 *   log({ Source: 'fn()', LogLevel: 'Debug', Message: 'debug text' })
 *
 * @param {object} entry - Log entry with Source, and optionally Error, Message, Data, LogLevel.
 */
function log(entry) {
  try {
    if (entry.Error) {
      entry.LogLevel = LogLevel.Error;
      entry.Error    = { name: entry.Error.name, message: entry.Error.message };
    } else if (!entry.LogLevel) {
      entry.LogLevel = LogLevel.Info;
    }

    /* Console output */
    if (entry.LogLevel === LogLevel.Error) {
      console.error('Exception in', entry.Source);
      console.error(entry.Error.name);
      console.error(entry.Error.message);
      if (entry.Data) console.log(entry.Data);
    } else {
      console.log('Output from', entry.Source);
      console.log(entry.Message);
      if (entry.Data) console.log(entry.Data);
    }

    /* Server-side logging */
    if (serverLogging) {
      var ajaxData = {
        UserContext: Tools.GetCurrentUserContext(),
        Data: JSON.stringify({ ServerMethod: 'AddClientEntry', MessageObject: entry })
      };

      $.ajax({
        url:         'api/ShippingService/UserMethod',
        method:      'POST',
        contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
        dataType:    'json',
        data:        ajaxData,
        async:       true
      }).fail(function (jqXHR, textStatus) {
        console.log('Unable to log message to server.', jqXHR, textStatus);
      });
    }

    Tools.HideLoader();
  } catch (error) {
    console.log(error.message);
  }
}


// ============================================================================
// FILE: email.js
// ============================================================================

/**
 * Email — D2M return delivery email management and notification email defaults.
 */

/**
 * Sets the return delivery address email field.
 *
 * @param {string} email - The email address to set.
 */
function setReturnDeliveryAddressEmail(email) {
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ReturnDeliveryAddressEmail"]').val(email);
}

/**
 * Gets the current return delivery address email.
 *
 * @returns {string} The email address value.
 */
function getReturnDeliveryAddressEmail() {
  return $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ReturnDeliveryAddressEmail"]').val();
}

/**
 * Sets default email notification fields on a shipment to the given address.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {number} packageIndex - The 0-based package index.
 * @param {string} email - The email address for all notifications.
 */
function setAllNotificationEmails(shipmentRequest, packageIndex, email) {
  var pkg = shipmentRequest.Packages[packageIndex];
  pkg.ShipNotificationEmail                      = true;
  pkg.ShipNotificationAddressEmail               = email;
  pkg.DeliveryNotificationEmail                  = true;
  pkg.DeliveryNotificationAddressEmail           = email;
  pkg.DeliveryExceptionNotificationEmail         = true;
  pkg.DeliveryExceptionNotificationAddressEmail  = email;
}


// ============================================================================
// FILE: event_handling.js
// ============================================================================

/**
 * Event Handling — Keyboard event detection and element event inspection.
 */

/**
 * Checks whether a specific event handler is already attached to a jQuery element.
 *
 * @param {jQuery}   $element - The jQuery-wrapped element.
 * @param {string}   eventName - The event type (e.g. "click", "change").
 * @param {function} handler - The handler function to look for.
 * @returns {boolean} True if the handler is already attached.
 */
function isEventAttached($element, eventName, handler) {
  var events = $._data($element.get(0), 'events');
  if (!events) return false;

  var handlerStr = handler.toString();
  return events[eventName]?.some(function (ev) {
    return ev.handler.toString() === handlerStr;
  }) || false;
}


// ============================================================================
// FILE: field_defaulting_and_validation.js
// ============================================================================

/**
 * Field Defaulting and Validation — Async element polling, custom data
 * extraction, and reference field validation.
 */

/**
 * Waits for a DOM element to appear, optionally sets a default value,
 * focuses it, and/or invokes a callback.
 *
 * @param {string}   selector - jQuery selector for the target element.
 * @param {boolean}  [focusAfter=false] - Focus the element after it appears.
 * @param {string}   [defaultValue] - Value to set on the element.
 * @param {number}   [timeoutSeconds] - Maximum wait time.
 * @param {function} [callback] - Callback to invoke after the element is found.
 */
async function waitForElement(selector, focusAfter, defaultValue, timeoutSeconds, callback) {
  try {
    await PollDomForElement(selector, timeoutSeconds);
    if (defaultValue) $(selector).val(defaultValue);
    if ($.isFunction(callback)) callback();
    if (focusAfter) $(selector).focus();
  } catch (error) {
    Logger.Log({ Source: 'waitForElement()', Error: error });
  }
}

/**
 * Waits for a <select> to have more than `minOptions` options loaded,
 * then optionally selects one by index or value string.
 *
 * @param {string}   selectSelector - jQuery selector for the select element.
 * @param {number}   [minOptions=1] - Minimum option count before proceeding.
 * @param {number}   [indexToSelect] - Option index to select.
 * @param {string}   [valueToSelect] - Option value string to select.
 * @param {boolean}  [clearFirst=false] - Clear the select before waiting.
 * @param {number}   [timeoutSeconds] - Maximum wait time.
 * @param {function} [callback] - Callback after options are loaded.
 */
async function waitForSelectOptions(selectSelector, minOptions, indexToSelect, valueToSelect, clearFirst, timeoutSeconds, callback) {
  if (clearFirst) $(selectSelector).val([]);
  var optionSelector = selectSelector + ' option:gt(' + (minOptions || 1) + ')';

  try {
    await PollDomForElement(optionSelector, timeoutSeconds);
    if ($.isFunction(callback)) callback();
    if (indexToSelect != null) $(selectSelector + ' option').eq(indexToSelect).prop('selected', 'selected');
    if (valueToSelect) $(selectSelector).val(valueToSelect);
  } catch (error) {
    Logger.Log({ Source: 'waitForSelectOptions()', Error: error });
  }
}

/**
 * Extracts a value from a CustomData key-value array by key name.
 *
 * @param {string}   key - The key to look up (case-insensitive).
 * @param {object[]} customDataArray - Array of {Key, Value} objects.
 * @returns {string} The matching value, or empty string.
 */
function getCustomDataValue(key, customDataArray) {
  if (!customDataArray) return '';
  for (var i = 0; i < customDataArray.length; i++) {
    if (customDataArray[i].Key.toLowerCase() === key.toLowerCase()) {
      return customDataArray[i].Value;
    }
  }
  return '';
}

/**
 * Extracts a custom field value from either the User (OriginAddress) or
 * Consignee custom data on the shipment request.
 *
 * @param {string} fieldName - The custom data key.
 * @param {string} source - "User" for OriginAddress, "To" for Consignee.
 * @param {object} shipmentRequest - The current shipment request.
 * @returns {string} The matching value, or empty string.
 */
function getCustomField(fieldName, source, shipmentRequest) {
  var customData = null;

  if (source === 'User') {
    customData = shipmentRequest?.PackageDefaults?.OriginAddress?.CustomData;
  } else if (source === 'To') {
    customData = shipmentRequest?.PackageDefaults?.Consignee?.CustomData;
  }

  return getCustomDataValue(fieldName, customData);
}

/**
 * Validates a reference value against a list of allowed values.
 * Throws an error if the value is not found.
 *
 * @param {string}   value - The value to validate.
 * @param {object[]} allowedList - Array of objects with a .Value property.
 * @throws {object} Error with message and errorCode if validation fails.
 */
function validateReference(value, allowedList) {
  var found = false;
  for (var i = 0; i < allowedList.length; i++) {
    if (allowedList[i].Value === value) {
      found = true;
      break;
    }
  }
  if (!found) {
    throw { message: 'Unable to validate shipment', errorCode: '001' };
  }
}


// ============================================================================
// FILE: file_upload.js
// ============================================================================

/**
 * File Upload — FTP file retrieval, carrier label image upload,
 * and guide file download/upload.
 */

/**
 * Uploads a carrier label image file selected via a file input.
 * Reads the file as base64 and triggers a load with the label data.
 *
 * @param {string}   fileInputSelector - jQuery selector for the file input.
 * @param {function} onReady - Callback receiving (base64Data) when the file is read.
 */
function uploadCarrierLabelFile(fileInputSelector, onReady) {
  var $input    = $(fileInputSelector);
  var imageFile = $input.prop('files')[0];

  if (!imageFile) {
    $input.click();
    return;
  }

  var reader    = new FileReader();
  reader.readAsDataURL(imageFile);
  reader.onload = function () {
    var base64Data = this.result.replace(/^data:.+;base64,/, '');
    onReady(base64Data);
    $input.val(null);
  };
}

/**
 * Downloads a file from the server via UserMethod and triggers a browser download.
 *
 * @param {string}   fileName - The file name/key to request.
 * @param {function} httpClient - The HTTP client function.
 */
function downloadServerFile(fileName, httpClient) {
  var payload = { Action: 'downloadFile', Data: fileName };
  var data    = { UserContext: undefined, Data: JSON.stringify(payload) };

  httpClient('UserMethod', data).then(function (ret) {
    if (ret.ErrorCode !== 0) return;

    var fileData     = JSON.parse(atob(ret.Data));
    var linkData     = 'data:' + fileData.fileType + ';base64,' + fileData.encodedFile;
    var downloadLink = document.createElement('a');

    downloadLink.style.display = 'none';
    downloadLink.download      = fileData.fileName;
    downloadLink.href          = linkData;

    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
  });
}

/**
 * Uploads a replacement file to the server by reading it as base64
 * and sending it via UserMethod.
 *
 * @param {File}     file - The File object to upload.
 * @param {string}   serverKey - The server-side key/name for the file.
 * @param {function} httpClient - The HTTP client function.
 */
function uploadReplacementFile(file, serverKey, httpClient) {
  var reader    = new FileReader();
  reader.onload = function () {
    var request = {
      Key:   serverKey,
      Value: JSON.stringify({
        fileType:    file.type,
        fileName:    file.name,
        encodedFile: this.result.split(',')[1]
      })
    };
    var payload = { Action: 'updateFile', Data: request };
    var data    = { UserContext: undefined, Data: JSON.stringify(payload) };
    httpClient('UserMethod', data);
  };
  reader.readAsDataURL(file);
}

/**
 * Binds a change handler to a paperless file input, reading the selected
 * file as base64 and storing it on the view model.
 *
 * @param {string} fileInputSelector - jQuery selector for the file input.
 * @param {object} vmInstance - The view model to store the paperless data on.
 */
function bindPaperlessFileInput(fileInputSelector, vmInstance) {
  $('body').on('change', fileInputSelector, vmInstance, function (e) {
    var file = e.target.files[0];
    if (!file) {
      e.data.paperless = null;
      return;
    }
    var reader    = new FileReader();
    reader.readAsDataURL(file);
    reader.onloadend = function () {
      e.data.paperless = { fileName: file.name, fileData: this.result.split(',')[1] };
    };
  });
}


// ============================================================================
// FILE: history_report_filtering.js
// ============================================================================

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


// ============================================================================
// FILE: history_report_generation.js
// ============================================================================

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


// ============================================================================
// FILE: load_state_management.js
// ============================================================================

/**
 * Load State Management — Preserving and restoring shipment state across
 * PreLoad / PostLoad lifecycle hooks.
 *
 * Patterns found:
 *   - Saving shipper and batch name before load, restoring after
 *   - Showing / hiding loader during load
 *   - PostLoad error display
 *   - PostLoad alert from server-side UserData1
 *   - PostLoad email restoration
 *   - PostLoad test-data detection
 */

// ---------------------------------------------------------------------------
// Preserve and restore fields across a load
// ---------------------------------------------------------------------------

/**
 * Saves transient shipment fields before the load so they can be restored
 * afterward (e.g. the active shipper and batch name are overwritten by the
 * server response).
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @returns {object} A snapshot of the fields to restore.
 */
function capturePreLoadState(shipmentRequest) {
  return {
    shipper:   shipmentRequest.PackageDefaults.Shipper,
    batchName: shipmentRequest.Packages[0].MiscReference5,
    batchId:   $('#cboBatches select option:selected').val().replace('string:', '')
  };
}

/**
 * Restores previously saved fields after a load completes.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {object} savedState      - The snapshot returned by capturePreLoadState.
 */
function restorePostLoadState(shipmentRequest, savedState) {
  shipmentRequest.PackageDefaults.Shipper   = savedState.shipper;
  shipmentRequest.Packages[0].MiscReference5 = savedState.batchName;
}

// ---------------------------------------------------------------------------
// PostLoad alerts
// ---------------------------------------------------------------------------

/**
 * After a load, checks the server-side ErrorCode and displays the error
 * message if present.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function showPostLoadError(shipmentRequest) {
  if (shipmentRequest.PackageDefaults.ErrorCode === 1) {
    alert(shipmentRequest.PackageDefaults.ErrorMessage);
    shipmentRequest.PackageDefaults.ErrorCode = 0;
  }
}

/**
 * After a load, checks UserData1 for an alert message from the server.
 * A leading "~" displays a "new shipment" modal; otherwise a standard modal.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {jQuery} $alertModal     - The standard alert modal element.
 * @param {jQuery} $newShipModal   - The new-shipment alert modal element.
 */
function showPostLoadUserDataAlert(shipmentRequest, $alertModal, $newShipModal) {
  var userData = shipmentRequest.Packages[0].UserData1;
  if (!userData || userData.length === 0) return;

  if (userData.charAt(0) === '~') {
    shipmentRequest.AlertTitle   = 'ShipExec Alert';
    shipmentRequest.AlertMessage = userData.substring(1);
    $newShipModal.modal('show');
  } else {
    shipmentRequest.AlertTitle   = 'ShipExec Alert';
    shipmentRequest.AlertMessage = userData;
    $alertModal.modal('show');
  }
}

// ---------------------------------------------------------------------------
// PostLoad email restoration
// ---------------------------------------------------------------------------

/**
 * After a void, restores the consignee email into the UI field.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function restoreConsigneeEmail(shipmentRequest) {
  $('input[ng-model="nameaddress.Email"]').val(
    shipmentRequest.PackageDefaults.Consignee.Email
  );
}


// ============================================================================
// FILE: loading.js
// ============================================================================

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


// ============================================================================
// FILE: logging.js
// ============================================================================

/**
 * Logging - Client-side logging framework with console output and optional
 * server-side logging via the UserMethod endpoint.
 *
 * Log levels mirror the server-side SBR nomenclature:
 *   Error, Info, Debug, Trace, Fatal
 */

var LogLevel = {
  Error: 'Error',
  Info:  'Info',
  Debug: 'Debug',
  Trace: 'Trace',
  Fatal: 'Fatal'
};

var Logger = {

  /**
   * Logs a message to the console and optionally to the server.
   *
   * @param {object} logObject - { Source, Error?, Message?, Data?, LogLevel? }
   */
  Log: function (logObject) {
    try {
      if (logObject.Error) {
        logObject.LogLevel = LogLevel.Error;
        logObject.Error = { name: logObject.Error.name, message: logObject.Error.message };
      } else if (!logObject.LogLevel) {
        logObject.LogLevel = LogLevel.Info;
      }

      if (logObject.LogLevel === LogLevel.Error) {
        console.error('Exception encountered in', logObject.Source);
        console.error(logObject.Error.name);
        console.error(logObject.Error.message);
        if (logObject.Data) console.log(logObject.Data);
      } else {
        console.log('Output from', logObject.Source);
        console.log(logObject.Message);
        if (logObject.Data) console.log(logObject.Data);
      }

      if (Tools.GetServerDebugMode() === true) {
        var ajaxObject = {
          UserContext: Tools.GetCurrentUserContext(),
          Data: JSON.stringify({ ServerMethod: 'AddClientEntry', MessageObject: logObject })
        };

        $.ajax({
          url:         'api/ShippingService/UserMethod',
          method:      'POST',
          contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
          dataType:    'json',
          data:        ajaxObject,
          async:       true
        }).fail(function (jqXHR, textStatus) {
          console.log('Unable to log message to server.');
        });
      }

      Tools.HideLoader();
    } catch (error) {
      console.log(error.message);
    }
  }
};

// D2M-specific logging helpers

/**
 * Logs the start of a method with an optional callee chain.
 * @param {string} method
 * @param {string} [calleeMethod]
 */
function logStartMethod(method, calleeMethod) {
  if (calleeMethod) {
    console.log('...STARTING ' + method + ' called by ' + calleeMethod);
  } else {
    console.log('...STARTING ' + method);
  }
}

/**
 * Logs an informational message indented to indicate sub-method detail.
 * @param {string} message
 */
function logMethodInfo(message) {
  console.log('      ' + message);
}

/**
 * Safely decodes a base64-encoded JSON string returned by the server.
 * @param {string} data - The encoded data string.
 * @returns {object|undefined}
 */
function decodeReturnString(data) {
  try {
    return JSON.parse($('<div />').html(data).text());
  } catch (error) {
    console.log('DecodeReturnString Error: ' + error.name + ' ' + error.message);
  }
}


// ============================================================================
// FILE: nmfc.js
// ============================================================================

/**
 * NMFC — LTL dimension matching, NMFC description/class assignment,
 * BOL comment generation, and dry-ice handling.
 *
 * Patterns found:
 *   - Interstate-McBee: LTL dimension table lookup, NMFC class assignment,
 *     BOL comment appending, parent container code generation
 *   - OSU: Dry-ice weight/regulation/purpose configuration
 *   - Reference concatenation for shipper/consignee references
 */

// ---------------------------------------------------------------------------
// LTL dimension matching
// ---------------------------------------------------------------------------

/**
 * Standard dimension pairs used for LTL BOL comment matching.
 * Each entry is [longestSide, secondSide].
 */
var LTL_DIMENSION_TABLE = [
  [34, 32], [48, 40], [52, 36],
  [12, 52], [14, 44], [18, 32],
  [18, 52], [28, 44], [37, 37]
];

/**
 * Matches package dimensions against the LTL dimension table and
 * returns a formatted dimension string for use in BOL comments.
 *
 * Dimensions are sorted descending before matching.
 *
 * @param {number} length - Package length.
 * @param {number} width  - Package width.
 * @param {number} height - Package height.
 * @returns {string} Formatted dimension string (e.g. "48x40x12") or "0x0x0".
 */
function matchLtlDimensions(length, width, height) {
  var dims = [length || 0, width || 0, height || 0];
  dims.sort(function (a, b) { return b - a; });

  if (dims[0] === 0 && dims[1] === 0 && dims[2] === 0) return '0x0x0';

  for (var i = 0; i < LTL_DIMENSION_TABLE.length; i++) {
    var x = LTL_DIMENSION_TABLE[i][0];
    var y = LTL_DIMENSION_TABLE[i][1];

    if (i < 3) {
      if (dims[0] === x && dims[1] === y) return x + 'x' + y + 'x' + dims[2];
      if (dims[1] === x && dims[2] === y) return x + 'x' + y + 'x' + dims[0];
    } else {
      if (dims[0] === y && dims[1] === x) return x + 'x' + y + 'x' + dims[2];
      if (dims[0] === y && dims[2] === x) return x + 'x' + y + 'x' + dims[1];
      if (dims[1] === y && dims[2] === x) return x + 'x' + y + 'x' + dims[0];
    }
  }

  return dims.join('x');
}

// ---------------------------------------------------------------------------
// LTL package defaults (Interstate-McBee)
// ---------------------------------------------------------------------------

/**
 * Configures an LTL package with NMFC description, BOL comment containing
 * matched dimensions, parent container code, and waybill number.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {number} packageIndex    - The 0-based index of the new package.
 * @param {string} nmfcDescription - The NMFC description text (e.g. "DIESEL ENGINE PARTS NMFC # 133300, SUB 4, Class 60").
 */
function configureLtlPackage(shipmentRequest, packageIndex, nmfcDescription) {
  var pkg = shipmentRequest.Packages[packageIndex - 1];
  if (!pkg) return;

  /* Match dimensions */
  var d = pkg.Dimensions || {};
  var dimString = matchLtlDimensions(d.Length, d.Width, d.Height);
  if (dimString !== '0x0x0') {
    pkg.BolComment = ((pkg.BolComment || '') + ' ' + dimString).trim();
  }

  /* Set NMFC description */
  pkg.Description = nmfcDescription + ' L' + packageIndex;

  /* Default waybill BOL number from MiscReference20 */
  if (!pkg.WaybillBolNumber) {
    pkg.WaybillBolNumber = pkg.MiscReference20;
  }

  /* Set parent container code when MiscReference20 is present */
  if (pkg.MiscReference20 && !shipmentRequest.Packages[packageIndex]?.ParentContainerCode) {
    shipmentRequest.Packages[packageIndex] = shipmentRequest.Packages[packageIndex] || {};
    if (!shipmentRequest.Packages[packageIndex].ParentContainerCode) {
      shipmentRequest.Packages[packageIndex].ParentContainerCode = 'P' + pkg.MiscReference20 + '-' + packageIndex;
    }
  }
}

// ---------------------------------------------------------------------------
// Dry-ice handling (OSU)
// ---------------------------------------------------------------------------

/**
 * Validates and configures dry-ice settings on all packages.
 * Throws if multiple packages are used with dry ice or if the weight is missing.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @throws {object} Error with message and errorCode.
 */
function configureDryIce(shipmentRequest) {
  var dryIceWeightRaw = returnPropertyValue(shipmentRequest.Packages[0].MiscReference8);
  var consigneeCountry = shipmentRequest.PackageDefaults.Consignee.Country;

  shipmentRequest.Packages.forEach(function (pkg) {
    if (!pkg.MiscReference7) return;

    if (shipmentRequest.Packages.length > 1) {
      throw { message: 'Multiple Packages Not Supported with Dry Ice', errorCode: '' };
    }
    if (returnPropertyValue(pkg.MiscReference8) === '') {
      throw { message: 'Missing Dry Ice Weight', errorCode: '' };
    }

    pkg.DryIceWeight  = { Amount: dryIceWeightRaw.replace('kgs', ''), Units: 'KG' };
    pkg.DryIceRegulationSet = (consigneeCountry === 'US') ? 1 : 2;
    pkg.DryIcePurpose = 2;
  });
}

// ---------------------------------------------------------------------------
// Reference concatenation (OSU)
// ---------------------------------------------------------------------------

/**
 * Builds shipper and consignee reference strings from MiscReference fields
 * and applies them to all packages along with notification emails.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {string} userId          - The current user's ID.
 */
function applyOsuReferences(shipmentRequest, userId) {
  var pkg0 = shipmentRequest.Packages[0];
  var refs = [1, 2, 3, 4, 5, 6].map(function (n) {
    return returnPropertyValue(pkg0['MiscReference' + n]);
  });

  var shipperRef   = refs.slice(0, 4).join('~');
  var consigneeRef = refs.slice(4, 6).join('~');

  shipmentRequest.Packages.forEach(function (pkg) {
    pkg.ShipperReference   = shipperRef;
    pkg.ConsigneeReference = consigneeRef;
    pkg.MiscReference20    = userId;
    pkg.ShipNotificationAddressEmail = '';
  });
}

/**
 * Returns the value unchanged if truthy, or an empty string if null/undefined.
 *
 * @param {*} value
 * @returns {string}
 */
function returnPropertyValue(value) {
  return value || '';
}


// ============================================================================
// FILE: nmfc_subclass_and_commodity_mapping.js
// ============================================================================

/**
 * NMFC Sub-class and Commodity Mapping — Builds commodity content objects
 * for international FedEx shipments, deduplicating by description and
 * combining quantities.
 *
 * Used by Gem Group for consolidating commodity lines across packages
 * before PreShip on international ShipConsole shipments.
 */

/**
 * Consolidates commodity contents across all packages for an international
 * FedEx shipment. Duplicate descriptions have their quantities summed.
 * The consolidated list is placed on the first package.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function consolidateInternationalCommodities(shipmentRequest) {
  var isInternational = shipmentRequest.PackageDefaults.Consignee.Country !== 'US';
  if (!isInternational) return;

  var descriptionIndex = new Map();
  var consolidated = [];

  shipmentRequest.Packages.forEach(function (pkg) {
    if (!pkg.CommodityContents) return;

    pkg.CommodityContents.forEach(function (commodity) {
      if (descriptionIndex.has(commodity.Description)) {
        var existing = consolidated[descriptionIndex.get(commodity.Description)];
        existing.Quantity = String(parseInt(existing.Quantity) + parseInt(commodity.Quantity));
      } else {
        descriptionIndex.set(commodity.Description, consolidated.length);
        consolidated.push(buildCommodityForExport(commodity));
      }
    });

    pkg.CommodityContents = [];
  });

  shipmentRequest.Packages[0].CommodityContents = consolidated;
}

/**
 * Adds a commodity to a specific package's CommodityContents array.
 * Supports both table-row cell arrays and structured bundle objects.
 *
 * @param {object}  vm        - The view model (to access currentShipment).
 * @param {object}  cellsOrBundle - Table row cells array or bundle data object.
 * @param {number}  packageIndex  - The 0-based package index.
 * @param {boolean} isBundle      - True if cellsOrBundle is a bundle object.
 */
function addToCommodityList(vm, cellsOrBundle, packageIndex, isBundle) {
  if (!cellsOrBundle || cellsOrBundle === '') {
    console.log('Error: Commodity data unavailable');
    return;
  }

  var commodity;

  if (isBundle) {
    var price = (cellsOrBundle.InternationalInfo && cellsOrBundle.InternationalInfo.ExportUnitPrice)
      ? cellsOrBundle.InternationalInfo.ExportUnitPrice
      : cellsOrBundle.SaleUnitPrice;

    commodity = {
      UnitValue:        { Currency: cellsOrBundle.InternationalInfo.ExportCurrencyCode, Amount: price },
      UnitWeight:       { Units: 'LB', Amount: cellsOrBundle.UnitWeight },
      QuantityUnitMeasure: 'PC',
      OriginCountry:    cellsOrBundle.InternationalInfo.CountryOfOrigin,
      ProductCode:      cellsOrBundle.KitComponentPart,
      HarmonizedCode:   cellsOrBundle.InternationalInfo.ExportTariffCode,
      Quantity:         cellsOrBundle.UnitsPerAssembly,
      Description:      cellsOrBundle.KitComponentPartDesc,
      packageIndex:     packageIndex
    };
  } else {
    var cells = cellsOrBundle;
    var price = (cells[17].textContent || cells[17].textContent !== '')
      ? cells[17].textContent
      : cells[4].textContent;

    commodity = {
      UnitValue:        { Currency: cells[15].textContent, Amount: price },
      UnitWeight:       { Units: 'LB', Amount: cells[7].textContent },
      QuantityUnitMeasure: 'PC',
      OriginCountry:    cells[14].textContent,
      ProductCode:      cells[1].textContent,
      HarmonizedCode:   cells[16].textContent,
      Quantity:         cells[10].textContent,
      Description:      cells[5].textContent,
      packageIndex:     packageIndex
    };
  }

  var existing = vm.currentShipment.Packages[packageIndex].CommodityContents || [];
  existing.push(commodity);
  vm.currentShipment.Packages[packageIndex].CommodityContents = existing;
}

// ---------------------------------------------------------------------------
// Internal helper
// ---------------------------------------------------------------------------

function buildCommodityForExport(source) {
  return {
    UnitValue:                  source.UnitValue,
    UnitWeight:                 source.UnitWeight,
    QuantityUnitMeasure:        'PC',
    ExportQuantityUnitMeasure1: 'PC',
    ExportQuantityUnitMeasure2: 'PC',
    LicenseUnitValue:           { Currency: 'USD' },
    DdtcUnitMeasure:            'PC',
    OriginCountry:              source.OriginCountry,
    ProductCode:                source.ProductCode,
    HarmonizedCode:             source.HarmonizedCode,
    Quantity:                   source.Quantity,
    Description:                source.Description,
    LicenseExpirationDate:      null,
    NaftaRvcAvgStartDate:       null,
    nAFTARVCAvgEndDate:         null
  };
}


// ============================================================================
// FILE: notification_polling_and_ui_refresh.js
// ============================================================================

/**
 * Notification Polling and UI Refresh — Shipping profile CRUD, return label
 * creation, EOD processing, and shipping profile change handlers.
 *
 * This file is a cross-reference. The core logic is covered in:
 *   - loading.js (loadShippingProfiles, saveOrDeleteShippingProfile,
 *     applyShippingProfile, createReturnLabelFromPrevious, processEOD)
 *   - d2m.js (D2M checkbox and element hiding)
 *
 * See those files for the canonical implementations.
 */


// ============================================================================
// FILE: notifications.js
// ============================================================================

/**
 * Notifications — Mailroom vs Traveler NewShipment configuration,
 * email notification defaults, and D2M enable/disable via UserMethod.
 *
 * Patterns found:
 *   - AlstonBird mailroom/traveler mode detection and NewShipment setup
 *   - Default email notification fields to current user's email
 *   - D2M EnableD2MShipping UserMethod call
 *
 * See also: alerts.js for ShowErrorAlert, d2m.js for D2M authorization checks.
 */

// ---------------------------------------------------------------------------
// Mailroom vs Traveler mode
// ---------------------------------------------------------------------------

/**
 * Determines whether the current profile is a "Mailroom" or "Traveler" profile.
 *
 * @param {string} profileName - The current profile name.
 * @returns {object} { IsMailroom: boolean, IsTraveler: boolean, LoadRadioButtonsIsHidden: boolean }
 */
function setMailroomOrTraveler(profileName) {
  return {
    IsMailroom:               profileName !== 'Traveler Profile',
    IsTraveler:               profileName === 'Traveler Profile',
    LoadRadioButtonsIsHidden: true
  };
}

// ---------------------------------------------------------------------------
// Email notification defaults
// ---------------------------------------------------------------------------

/**
 * Sets all notification email fields on a package to the given email address.
 *
 * @param {object} pkg   - The package object.
 * @param {string} email - The email address to populate.
 */
function setNotificationDefaults(pkg, email) {
  pkg.ShipNotificationEmail                    = true;
  pkg.ShipNotificationAddressEmail             = email;
  pkg.DeliveryNotificationEmail                = true;
  pkg.DeliveryNotificationAddressEmail         = email;
  pkg.DeliveryExceptionNotificationEmail       = true;
  pkg.DeliveryExceptionNotificationAddressEmail = email;
}

/**
 * Binds notification checkbox clicks to auto-populate the corresponding
 * email field with the user's email.
 *
 * @param {object} vm        - The view model.
 * @param {function} getUserName - Function returning the current user's email.
 */
function bindNotificationCheckboxes(vm, getUserName) {
  var mappings = {
    '[property="ShipNotificationEmail"]':              '[name="shipNotificationAddressEmail"]',
    '[property="DeliveryExceptionNotificationEmail"]': '[name="DeliveryExceptionNotificationAddressEmail"]',
    '[property="DeliveryNotificationEmail"]':          '[name="DeliveryNotificationEmail"]'
  };

  Object.keys(mappings).forEach(function (checkboxSelector) {
    $('body').delegate(checkboxSelector, 'click', function () {
      var emailField = mappings[checkboxSelector];
      $(emailField).val(getUserName()).change();
    });
  });
}

// ---------------------------------------------------------------------------
// D2M enable via UserMethod
// ---------------------------------------------------------------------------

/**
 * Enables D2M (Direct-to-Mobile) shipping by calling the server-side
 * UserMethod and refreshing the profile.
 *
 * @param {function} thinClientAPIRequest - The API request function.
 * @param {object}   userContext          - The current user context.
 * @param {object}   vm                   - The view model (vm.profile is updated).
 */
function enableD2MShipping(thinClientAPIRequest, userContext, vm) {
  var params = { ActionMessage: 'EnableD2MShipping' };
  var data   = { Data: JSON.stringify(params), UserContext: userContext };
  var response = thinClientAPIRequest('UserMethod', data, false);

  if (response && response.responseJSON.ErrorCode !== 0) {
    alert('Error while executing UserMethod: ' + response.responseJSON.ErrorCode);
  } else {
    vm.profile = decodeReturnString(response.responseJSON.Data);
  }
}


// ============================================================================
// FILE: package_history_anonymization.js
// ============================================================================

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


// ============================================================================
// FILE: package_reference_propagation.js
// ============================================================================

/**
 * Package Reference Propagation — Setting ShipperReference from CustomData
 * on NewShipment and AddPackage, and updating references with consolidated
 * order numbers.
 *
 * Patterns found:
 *   - ABB Optical / ABCFoods / AGCS: Set ShipperReference from OriginAddress CustomData
 *   - AGCS: Validate ShipperReference against FieldOptionValidations
 *   - Consolidated orders: Append order numbers to ShipperReference and MiscReference7
 *
 * See also: field_defaulting_and_validation.js for getCustomField / validateReference.
 */

// ---------------------------------------------------------------------------
// ShipperReference from CustomData
// ---------------------------------------------------------------------------

/**
 * Sets the ShipperReference on a package from the user's custom data.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {number} packageIndex    - The 0-based package index.
 * @param {string} customDataKey   - The CustomData key to look up (e.g. "Custom1", "Custom6").
 */
function setShipperReferenceFromCustomData(shipmentRequest, packageIndex, customDataKey) {
  var customData = shipmentRequest.PackageDefaults.OriginAddress.CustomData;
  var value = getKeyValue(customDataKey, customData);
  if (value !== '') {
    shipmentRequest.Packages[packageIndex].ShipperReference = value;
  }
}

/**
 * Builds a compound ShipperReference from the custom-data value and an
 * additional reference field, then applies it to all packages.
 * Validates the custom-data value against a list of allowed values.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {string}   customValue     - The custom-data-derived value.
 * @param {string}   additionalRef   - An additional reference value (e.g. MiscReference1).
 * @param {object[]} validationList  - Array of { Value } objects for validation.
 */
function applyCompoundShipperReference(shipmentRequest, customValue, additionalRef, validationList) {
  var referenceValue = (customValue + ' ' + (additionalRef || '')).trim();

  for (var i = 0; i < shipmentRequest.Packages.length; i++) {
    shipmentRequest.Packages[i].ShipperReference = referenceValue;
  }

  validateAgainstFieldOptions(customValue, validationList);
}

// ---------------------------------------------------------------------------
// Consolidated order reference update
// ---------------------------------------------------------------------------

/**
 * Appends an order number to the ShipperReference and MiscReference7 fields,
 * maintaining a unique comma-separated list.
 *
 * ShipperReference only keeps the first value before the comma when sent to
 * PostShip; MiscReference7 keeps the full list for server-side processing.
 *
 * @param {string} orderNumber - The order number to append.
 */
function updateReferencesWithOrderNumber(orderNumber) {
  var $shipperRef = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]');
  var $miscRef7   = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].MiscReference7"]');

  var currentVal  = $shipperRef.val() + ',' + orderNumber;
  var uniqueVal   = getUniqueCSVStringFromString(currentVal);

  $shipperRef.val(uniqueVal);
  $miscRef7.val(uniqueVal);
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Extracts a value from a CustomData key-value array.
 *
 * @param {string}   key       - The key to look up.
 * @param {object[]} customData - Array of { Key, Value } objects.
 * @returns {string} The matching value or empty string.
 */
function getKeyValue(key, customData) {
  if (!customData) return '';
  for (var i = 0; i < customData.length; i++) {
    if (customData[i].Key === key) return customData[i].Value;
  }
  return '';
}

/**
 * Validates a value against a list of field-option entries, throwing
 * an error if not found.
 *
 * @param {string}   value          - The value to validate.
 * @param {object[]} validationList - Array of { Value } objects.
 * @throws {object} With message and errorCode.
 */
function validateAgainstFieldOptions(value, validationList) {
  for (var i = 0; i < validationList.length; i++) {
    if (validationList[i].Value === value) return;
  }
  throw { message: 'Unable to validate shipment', errorCode: '001' };
}


// ============================================================================
// FILE: page_initialization_and_navigation.js
// ============================================================================

/**
 * Page Initialization and Navigation — PageLoaded routing, NewShipment
 * setup, and consolidated-orders button creation.
 *
 * This file is a cross-reference. The canonical implementations live in:
 *   - loading.js (onPageLoaded, bindConsigneeTabSearch, hideD2MCheckbox,
 *     loadShippingProfiles, applyShippingProfile)
 *   - shipping_view_initialization.js (initConsolidatedOrdersButton,
 *     setCurrentViewModel, focusOnLoadInput)
 *
 * See those files for details.
 */


// ============================================================================
// FILE: printing_and_document_output.js
// ============================================================================

/**
 * Printing and Document Output — Traveler label PDF generation, ZPL
 * printer defaults, file download, and PrePrint/PostPrint hooks.
 *
 * Patterns found:
 *   - AlstonBird: Generate a traveler label PDF via UserMethod and open
 *     it in a new browser tab as a data URI
 *   - Bulgari MX: Prepend ZPL printer-default control string to raw label data
 *   - CMR: Download a file from the server as a data-URI link click
 */

// ---------------------------------------------------------------------------
// Traveler label PDF generation (AlstonBird)
// ---------------------------------------------------------------------------

/**
 * Sends the current shipment to the server via UserMethod and opens the
 * returned PDF traveler label in a new browser tab.
 *
 * @param {object}   shipmentRequest     - The current shipment request.
 * @param {function} thinClientAPIRequest - Synchronous API request function.
 */
function printTravelerLabel(shipmentRequest, thinClientAPIRequest) {
  var data   = { Data: JSON.stringify(shipmentRequest) };
  var result = thinClientAPIRequest('UserMethod', data, false).responseJSON;
  var response     = JSON.parse(atob(result.Data));
  var pdfBase64    = response.DocumentResponses[0].PdfData[0];
  var dataURI      = 'data:application/pdf;base64,' + pdfBase64;

  window.open(dataURI, '_blank');
  alert('Delegate Label Created');
}

// ---------------------------------------------------------------------------
// ZPL printer defaults (Bulgari MX)
// ---------------------------------------------------------------------------

/**
 * Prepends ZPL printer-default commands to the raw label data so that the
 * printer initialises correctly when profile settings are not sent.
 *
 * @param {object} doc - The document object (has DocumentSymbol and RawData).
 * @param {string} expectedSymbol - The document symbol to match (e.g. "UPSAPI.UPS.PACKAGE_LABEL.STANDARD").
 */
function prependZplDefaults(doc, expectedSymbol) {
  if (doc.DocumentSymbol !== expectedSymbol || !doc.RawData) return;

  var printerDefaults = '^XA^LH0,0^XSY,Y^MD30^XZ\n';
  var originalRaw     = atou(doc.RawData);
  doc.RawData[0]      = utoa(printerDefaults + originalRaw);
}

// ---------------------------------------------------------------------------
// File download (CMR)
// ---------------------------------------------------------------------------

/**
 * Downloads a file from the server by requesting its content via
 * UserMethod and triggering a browser download.
 *
 * @param {string}   fileName   - The name of the file to download.
 * @param {function} httpClient - The thin-client HTTP client function.
 */
function downloadFile(fileName, httpClient) {
  var payload = { Action: 'downloadFile', Data: fileName };
  var data    = { UserContext: undefined, Data: JSON.stringify(payload) };

  httpClient('UserMethod', data).then(function (ret) {
    if (ret.ErrorCode !== 0) return;

    var fileData     = JSON.parse(atob(ret.Data));
    var linkData     = 'data:' + fileData.fileType + ';base64,' + fileData.encodedFile;
    var downloadLink = document.createElement('a');

    downloadLink.style.display = 'none';
    downloadLink.download      = fileData.fileName;
    downloadLink.href          = linkData;

    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
  });
}


// ============================================================================
// FILE: return_delivery_address.js
// ============================================================================

/**
 * Return Delivery Address — Consignee defaults by MiscReference20 lookup,
 * D2M return address population from shipper, and return-delivery
 * configuration toggling.
 *
 * Patterns found:
 *   - ABB Digital Eye Lab: Map MiscReference20 to a hard-coded consignee
 *   - D2M (Alight): Copy shipper address fields into the return-address form
 *   - D2M: Set/clear return-delivery description, email, checkbox, and
 *     commercial invoice method
 */

// ---------------------------------------------------------------------------
// Consignee defaults by reference key (ABB Digital Eye Lab)
// ---------------------------------------------------------------------------

/**
 * Populates consignee address fields based on a reference key value.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {string} referenceKey    - The reference key (e.g. "GREEN_OPTICS", "DEL-NY").
 * @param {object} addressMap      - Map of referenceKey → address object.
 * @param {object} defaultAddress  - Fallback address if key not found.
 */
function setConsigneeByReference(shipmentRequest, referenceKey, addressMap, defaultAddress) {
  var address = addressMap[referenceKey] || defaultAddress;
  var consignee = shipmentRequest.PackageDefaults.Consignee;

  consignee.Company       = address.Company;
  consignee.Contact       = address.Contact;
  consignee.Address1      = address.Address1;
  consignee.City          = address.City;
  consignee.StateProvince = address.StateProvince;
  consignee.PostalCode    = address.PostalCode;
  if (address.Phone) consignee.Phone = address.Phone;
}

// ---------------------------------------------------------------------------
// D2M return address from shipper
// ---------------------------------------------------------------------------

/**
 * Copies the first shipper's address into the return-address form fields.
 * Uses an interval to wait for the shipper data to load.
 *
 * @param {object} vm - The view model (vm.profile.Shippers[0] is the source).
 */
function loadReturnAddressFromShipper(vm) {
  var interval = setInterval(function () {
    var shipper = vm.profile.Shippers[0];
    if (!shipper || !shipper.Company) return;
    clearInterval(interval);

    var fields = ['company', 'address1', 'city', 'stateProvince', 'postalCode', 'phone', 'contact'];
    var props  = ['Company', 'Address1', 'City', 'StateProvince', 'PostalCode', 'Phone', 'Contact'];

    fields.forEach(function (field, i) {
      $('[name="' + field + '"]:eq(1)').val(shipper[props[i]]).trigger('change');
    });

    /* Country uses "string:" prefix for AngularJS binding */
    $('[name="Country"]:eq(1)').val('string:' + shipper.Country).trigger('change');
  }, 150);
}

// ---------------------------------------------------------------------------
// Return-delivery shipment configuration toggle
// ---------------------------------------------------------------------------

/**
 * Enables return-delivery settings: sets the description, configures the
 * insurance checkbox, and loads the return address from the shipper.
 *
 * @param {object} vm - The view model.
 */
function setReturnDeliveryConfiguration(vm) {
  setDescription('Returned Goods');
  setReturnDeliveryCheckbox(true);
  loadReturnAddressFromShipper(vm);
}

/**
 * Clears return-delivery settings: blanks description, email, checkbox,
 * and commercial invoice method.
 */
function clearReturnDeliveryConfiguration() {
  setDescription('');
  setReturnDeliveryEmail('');
  setReturnDeliveryCheckbox(false);
  $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].CommercialInvoiceMethod"]').val('');
}

// ---------------------------------------------------------------------------
// Internal helpers
// ---------------------------------------------------------------------------

function setDescription(value) {
  $('textarea[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]').val(value);
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]').val(value);
}

function setReturnDeliveryEmail(value) {
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ReturnDeliveryAddressEmail"]').val(value);
}

function setReturnDeliveryCheckbox(enabled) {
  if (enabled) {
    $('select option[value="number:1"]').prop('selected', true);
  } else {
    $('select option[value="number:1"]').prop('selected', false);
  }
}


// ============================================================================
// FILE: service_filtering.js
// ============================================================================

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


// ============================================================================
// FILE: shipment_commodity_contents_initialization.js
// ============================================================================

/**
 * Shipment Commodity Contents Initialization — D2M return-delivery
 * configuration, shipper-to-return-address copy, description helpers,
 * and commodity tab refresh.
 *
 * This file is a cross-reference. The canonical implementations live in:
 *   - return_delivery_address.js (setReturnDeliveryConfiguration,
 *     clearReturnDeliveryConfiguration, loadReturnAddressFromShipper)
 *   - commodity_mapping.js (commodity content object building)
 *   - loading.js (applyShippingProfile for AllCare profile CRUD)
 *
 * Unique content:
 *   - UpdateCommodities: Refreshes the commodity list UI after adding items
 *   - OnShipperChange: Toggles return-delivery config based on D2M state
 */

/**
 * Refreshes the commodity-contents UI by clicking the active tab and
 * the first inactive pagination button.
 */
function refreshCommodityUI() {
  $('div.ui-tab-container > div.ng-isolate-scope > ul > li.active > a').click();
  $('#goods').find('div.ng-table-counts.btn-group').find('button:not(.active):first').click();
}

/**
 * Handles shipper change events in D2M mode: enables return-delivery
 * configuration when the D2M shipper is selected, clears it otherwise.
 *
 * @param {string}  currentShipper   - The newly selected shipper symbol.
 * @param {string}  d2mShipperSymbol - The authorized D2M shipper symbol.
 * @param {boolean} isD2MCapable     - Whether the session supports D2M.
 * @param {object}  vm               - The view model.
 */
function onShipperChangeD2M(currentShipper, d2mShipperSymbol, isD2MCapable, vm) {
  if (isD2MCapable && currentShipper === d2mShipperSymbol) {
    setDescription('Returned Goods');
    loadReturnAddressFromShipper(vm);
  } else {
    setDescription('');
    setReturnDeliveryEmail('');
    $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].CommercialInvoiceMethod"]').val('');
  }
}

/* Helpers referenced from return_delivery_address.js */
function setDescription(value) {
  $('textarea[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]').val(value);
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]').val(value);
}

function setReturnDeliveryEmail(value) {
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ReturnDeliveryAddressEmail"]').val(value);
}


// ============================================================================
// FILE: shipment_conversion_and_validation.js
// ============================================================================

/**
 * Shipment Conversion and Validation — Consolidated order save/process
 * workflow, consignee matching, and reference validation.
 *
 * This file is a cross-reference. The canonical implementations live in:
 *   - third_party_options.js (showConsolidatedOrderDialog,
 *     addOrderNumbersToList, saveConsolidatedOrderNumbers,
 *     processDataFromCustomer)
 *   - validation.js (checkForMatchingConsignee)
 *   - package_reference_propagation.js (updateReferencesWithOrderNumber,
 *     validateAgainstFieldOptions)
 *   - field_defaulting_and_validation.js (getCustomField, validateReference)
 *
 * See those files for the canonical implementations.
 */


// ============================================================================
// FILE: shipment_defaults.js
// ============================================================================

/**
 * Shipment Defaults — NewShipment reference defaulting from CustomData,
 * Tab-key consignee search binding, batch population, and D2M return-
 * delivery configuration on shipper change.
 *
 * This file is a cross-reference. The canonical implementations live in:
 *   - package_reference_propagation.js (setShipperReferenceFromCustomData)
 *   - loading.js (bindConsigneeTabSearch)
 *   - return_delivery_address.js (setReturnDeliveryConfiguration,
 *     clearReturnDeliveryConfiguration)
 *   - load_state_management.js (capturePreLoadState, restorePostLoadState)
 *
 * See those files for the canonical implementations.
 */


// ============================================================================
// FILE: shipment_timestamp_stamping.js
// ============================================================================

/**
 * Shipment Timestamp Stamping — Adding date/time stamps to MiscReference15
 * before ship and load, and shipping-profile change with date stamping.
 *
 * Patterns found:
 *   - CMR: Stamp MiscReference15 with dateTimeStamp() on PreShip
 *   - CMR: Stamp MiscReference15 with today's date on PreLoad
 *   - AllCare: Update Shipdate to current date on profile change
 *   - Clarios: CloseAllShippers (duplicated from close_manifest_processing.js)
 */

/**
 * Returns the current date/time as a locale string with timezone abbreviation.
 *
 * @returns {string} e.g. "4/7/2026, 14:30:00 EDT"
 */
function dateTimeStamp() {
  return new Date().toLocaleString('en-US', { timeZoneName: 'short', hour12: false });
}

/**
 * Returns today's date formatted for the locale.
 *
 * @returns {string}
 */
function todayString() {
  return new Date().toLocaleDateString();
}

/**
 * Stamps MiscReference15 on the current package with the provided value.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {number} packageIndex    - The current package index.
 * @param {string} stampValue      - The value to stamp (from dateTimeStamp or todayString).
 */
function stampMiscReference15(shipmentRequest, packageIndex, stampValue) {
  shipmentRequest.Packages[packageIndex].MiscReference15 = stampValue;
}

/**
 * Creates a shipdate object for the current date in the format expected
 * by the shipment model.
 *
 * @returns {object} { Year, Month, Day }
 */
function currentShipdate() {
  var today = new Date();
  return {
    Year:  today.getFullYear(),
    Month: today.getMonth() + 1,
    Day:   today.getDate()
  };
}


// ============================================================================
// FILE: shipping_charges_defaulting_and_return_address_population.js
// ============================================================================

/**
 * Shipping Charges Defaulting and Return Address Population — Return-label
 * creation, shipping-profile change, notification defaults, origin-address
 * to return-address copy, cost-center loading, and notification email
 * persistence across lifecycle events.
 *
 * This file is a cross-reference. The canonical implementations live in:
 *   - loading.js (createReturnLabelFromPrevious, applyShippingProfile)
 *   - notifications.js (setNotificationDefaults, bindNotificationCheckboxes)
 *   - return_delivery_address.js (loadReturnAddressFromShipper)
 *   - user_context_filtering.js (loadCostCenter, copyOriginToReturnAddress)
 *
 * Unique content:
 *   - AppliedImaging: Persist ShipNotificationAddressEmail across PreShip,
 *     PreRate, PostRate, and PostSelectAddressBook
 */

/**
 * Reads the ship-notification email address from the DOM input field.
 * Used to persist the value across lifecycle hooks that may reset it.
 *
 * @returns {string} The current notification email address.
 */
function getShipNotificationEmail() {
  return $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipNotificationAddressEmail"]').val();
}

/**
 * Persists the notification email on the package model so it survives
 * lifecycle transitions.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {number} packageIndex    - The package index (usually 0).
 */
function persistNotificationEmail(shipmentRequest, packageIndex) {
  shipmentRequest.Packages[packageIndex].ShipNotificationAddressEmail = getShipNotificationEmail();
}


// ============================================================================
// FILE: shipping_view_initialization.js
// ============================================================================

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


// ============================================================================
// FILE: sorting.js
// ============================================================================

/**
 * Sorting — Array deduplication, CSV string utilities, and generic
 * comparators for sorting lists by key.
 *
 * These utilities are part of the "Tools" namespace and are used
 * throughout the CBR codebase for managing order-number lists and
 * sorting shippers/rate results.
 */

// ---------------------------------------------------------------------------
// Unique array / CSV string utilities
// ---------------------------------------------------------------------------

/**
 * Splits a delimited string into an array of unique, trimmed, non-empty values.
 *
 * @param {string} inputString    - The delimited string.
 * @param {string} [delimiter=","] - The delimiter character.
 * @returns {string[]} Array of unique values.
 */
function getUniqueArrayFromString(inputString, delimiter) {
  delimiter = delimiter || ',';
  var parts = inputString.split(delimiter).filter(function (s) {
    return s.trim().length > 0;
  });
  return parts.filter(function (value, index, self) {
    return self.indexOf(value) === index;
  });
}

/**
 * Converts an array of strings into a comma-separated string with
 * duplicates removed.
 *
 * @param {string[]} inputArray - The source array.
 * @returns {string} A comma-separated string (trailing comma included).
 */
function getUniqueCSVStringFromArray(inputArray) {
  var unique = inputArray.filter(function (value, index, self) {
    return self.indexOf(value) === index;
  });

  var result = '';
  unique.forEach(function (item) {
    if (item.trim().length > 0) {
      result += item.trim() + ',';
    }
  });
  return result;
}

/**
 * Deduplicates a comma-separated string, returning a unique CSV string.
 *
 * @param {string} inputString - The comma-separated string.
 * @returns {string} A deduplicated comma-separated string.
 */
function getUniqueCSVStringFromString(inputString) {
  var uniqueArray = getUniqueArrayFromString(inputString);
  return getUniqueCSVStringFromArray(uniqueArray);
}

// ---------------------------------------------------------------------------
// Generic comparators
// ---------------------------------------------------------------------------

/**
 * Returns a comparator function that sorts objects by a given key.
 * Useful for sorting arrays of shippers, services, etc.
 *
 * @param {string} key - The property name to sort by.
 * @returns {function} A comparator suitable for Array.prototype.sort().
 *
 * @example
 *   vm.profile.Shippers.sort(compareByKey('Name'));
 */
function compareByKey(key) {
  return function (a, b) {
    if (a[key] > b[key]) return 1;
    if (a[key] < b[key]) return -1;
    return 0;
  };
}

// ---------------------------------------------------------------------------
// Rate-result selection by SortIndex (AlstonBird)
// ---------------------------------------------------------------------------

/**
 * Finds the rate result with SortIndex === 0 and returns its service.
 * Used by mailroom profiles to auto-select the recommended service.
 *
 * @param {object[]} rateResults - The array of rate results from the server.
 * @returns {object|undefined} The service object { Symbol } or undefined.
 */
function selectServiceBySortIndex(rateResults) {
  for (var i = 0; i < rateResults.length; i++) {
    if (rateResults[i].SortIndex === 0) {
      return rateResults[i].PackageDefaults.Service;
    }
  }
}


// ============================================================================
// FILE: thin_client_api_request.js
// ============================================================================

/**
 * Thin Client API Request — AJAX wrappers for communicating with the
 * ShipExec service API (via config.json or direct URL), UserMethod
 * requests, and user-context initialisation.
 *
 * Patterns found:
 *   - Config-based: Read ShipExecServiceUrl from config.json, attach Bearer token
 *   - Direct: Use client.config.ShipExecServiceUrl with getAuthorizationToken()
 *   - MakeUserMethodRequest: Full AJAX wrapper with loader, callbacks, and error handling
 *   - SetCurrentUserContext: Populate CompanyId/UserId from vm or GET /api/usercontext
 *   - ajaxGet: Simple synchronous GET helper
 */

// ---------------------------------------------------------------------------
// Config-based API request
// ---------------------------------------------------------------------------

/**
 * Makes an API request by first reading the service URL from config.json,
 * then performing a POST with a Bearer token.
 *
 * @param {string}  method - The API method path (e.g. "UserMethod").
 * @param {object}  data   - The POST data.
 * @param {boolean} isAsync - Whether the request is asynchronous.
 */
function thinClientApiRequestFromConfig(method, data, isAsync) {
  $.getJSON('config.json', function (config) {
    var url   = config.ShipExecServiceUrl;
    var token = url.startsWith('http')
      ? { Authorization: 'Bearer ' + JSON.parse(window.localStorage.getItem('TCToken')).access_token }
      : '';
    return $.post({ url: url + '/' + method, data: data, async: isAsync, headers: token });
  });
}

// ---------------------------------------------------------------------------
// Direct API request
// ---------------------------------------------------------------------------

/**
 * Makes a POST request to the ShipExec service using a pre-configured
 * client object.
 *
 * @param {string}  method - The API method path.
 * @param {object}  data   - The POST data.
 * @param {boolean} isAsync - Whether the request is asynchronous.
 * @param {object}  client  - The client service with `config.ShipExecServiceUrl` and `getAuthorizationToken()`.
 * @returns {jqXHR} The jQuery AJAX promise.
 */
function thinClientApiRequest(method, data, isAsync, client) {
  return $.post({
    url:     client.config.ShipExecServiceUrl + '/' + method,
    data:    data,
    async:   isAsync,
    headers: client.getAuthorizationToken()
  });
}

// ---------------------------------------------------------------------------
// UserMethod request with loader and callbacks
// ---------------------------------------------------------------------------

/**
 * Makes a UserMethod request with loader display, error handling, and an
 * optional callback that receives the parsed response.
 *
 * @param {string}   requestMethod - The server method name.
 * @param {string}   requestData   - The data payload.
 * @param {boolean}  [isAsync=true] - Whether the request is asynchronous.
 * @param {function} [callback]    - Called with the parsed response JSON.
 * @returns {object|undefined} The parsed response (only available for sync calls).
 */
function makeUserMethodRequest(requestMethod, requestData, isAsync, callback) {
  showLoader();

  var retJson;
  var dataObject = { ServerMethod: requestMethod, MethodData: requestData };
  var ajaxObject = {
    UserContext: getCurrentUserContext(),
    Data:        JSON.stringify(dataObject)
  };

  var request = $.ajax({
    url:         'api/ShippingService/UserMethod',
    method:      'POST',
    contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
    dataType:    'json',
    async:       isAsync !== false,
    data:        ajaxObject
  });

  request.done(function (json) {
    retJson = parseReturnData(json.Data);
  });

  request.fail(function (jqXHR) {
    console.error('MakeUserMethodRequest failed:', jqXHR.responseText);
  });

  request.always(function () {
    hideLoader();
    if (typeof callback === 'function') callback(retJson);
  });

  return retJson;
}

// ---------------------------------------------------------------------------
// User context initialisation
// ---------------------------------------------------------------------------

var _currentUserContext = {};

/**
 * Populates the current user context (CompanyId, UserId) from the
 * view model or via a synchronous GET to /api/usercontext.
 *
 * @param {object}  viewModel   - The AngularJS view model.
 * @param {boolean} includeUser - If true, fetches UserId via AJAX if not available.
 */
function setCurrentUserContext(viewModel, includeUser) {
  var ctx = {};

  try {
    if (viewModel.UserInformation) {
      ctx.CompanyId = viewModel.UserInformation.CompanyId;
      ctx.UserId    = viewModel.UserInformation.UserId;
      _currentUserContext = ctx;
      return;
    }

    if (!_currentUserContext.UserId && includeUser) {
      $.ajax({
        url:    'api/usercontext/GET',
        method: 'GET',
        async:  false
      }).done(function (data) {
        ctx = data;
      }).fail(function (jqXHR) {
        console.error('SetCurrentUserContext failed:', jqXHR.responseText);
      });
    } else if (!_currentUserContext.CompanyId) {
      ctx.CompanyId = viewModel.profile.CompanyId;
      ctx.UserId    = _currentUserContext.UserId;
    }
  } catch (error) {
    console.error('SetCurrentUserContext error:', error.message);
  }

  _currentUserContext = ctx;
}

/**
 * Returns the current user context.
 *
 * @returns {object} { CompanyId, UserId }
 */
function getCurrentUserContext() {
  return _currentUserContext;
}

// ---------------------------------------------------------------------------
// Simple synchronous GET helper
// ---------------------------------------------------------------------------

/**
 * Performs a synchronous AJAX GET request and returns the response.
 *
 * @param {string} url - The URL to GET.
 * @returns {object} The response data.
 */
function ajaxGet(url) {
  var result;
  $.ajax({
    type:    'GET',
    url:     url,
    async:   false,
    success: function (response) { result = response; }
  });
  return result;
}

// ---------------------------------------------------------------------------
// Loader helpers (referenced by other functions)
// ---------------------------------------------------------------------------

function showLoader() {
  $('div.loading').removeClass('ng-hide').addClass('ng-show');
}

function hideLoader() {
  $('div.loading').removeClass('ng-show').addClass('ng-hide');
}


// ============================================================================
// FILE: third_party_options.js
// ============================================================================

/**
 * Third Party Options — Consolidated orders dialog for adding multiple
 * order numbers to a shipment, validating consignee matching, and
 * sending orders to the server for processing.
 *
 * These functions make up the "ConsolidatedOrders" namespace in the
 * CBR 1.5 pattern.
 */

// ---------------------------------------------------------------------------
// Consolidated orders dialog
// ---------------------------------------------------------------------------

/**
 * Opens the consolidated-orders modal, populates it with existing order
 * numbers from ShipperReference, and focuses the input field.
 */
function showConsolidatedOrderDialog() {
  showOverlay();

  /* Clear existing options */
  $('#selectConsolidatedOrders').empty();

  /* Add existing orders from ShipperReference */
  var shipperRef  = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]').val();
  var existingOrders = getUniqueArrayFromString(shipperRef);

  existingOrders.forEach(function (order) {
    $('#selectConsolidatedOrders').append(
      $('<option />').val(order).text(order)
        .removeClass('not-processed').addClass('processed')
        .css('color', '#265ca1')
    );
  });

  $('#divModalConsolidateShipments').fadeIn('fast').promise().done(function () {
    $('#textOrderNumber').val('').focus();
  });
}

/**
 * Adds a unique order number from the input field to the consolidated-
 * orders select box. Prevents duplicates.
 *
 * @returns {boolean} false if the order already exists.
 */
function addOrderNumberToList() {
  var value = $('#textOrderNumber').val();
  if (!value || value.length === 0) return false;

  /* Check for duplicates */
  var exists = false;
  $('#selectConsolidatedOrders option').each(function () {
    if (this.text === value) {
      exists = true;
      return false; /* break */
    }
  });

  if (exists) {
    $('#textOrderNumber').val('').focus();
    return false;
  }

  /* Add the new order */
  $('#selectConsolidatedOrders').append(
    $('<option />').val(value).text(value).addClass('not-processed').trigger('change')
  ).promise().done(function () {
    $('#textOrderNumber').val('').focus();
  });
}

// ---------------------------------------------------------------------------
// Save and process consolidated orders
// ---------------------------------------------------------------------------

/**
 * Collects unprocessed order numbers from the select box and sends them
 * to the server for processing.
 *
 * @param {function} makeRequest       - The UserMethod request function.
 * @param {function} processCallback   - Callback to handle the server response.
 */
function saveConsolidatedOrderNumbers(makeRequest, processCallback) {
  try {
    var ordersToQuery = '';
    $('#selectConsolidatedOrders > option.not-processed').each(function () {
      ordersToQuery += $(this).text() + ',';
    });

    $('#divModalConsolidateShipments').hide();

    if (ordersToQuery.length > 0) {
      makeRequest('GetOrderInformation', ordersToQuery, true, processCallback);
    } else {
      showErrorAlert('ERROR: No unique orders to pull from the server. All consolidated orders have already been added to this shipment.');
    }
  } catch (error) {
    console.error('SaveConsolidatedOrderNumbers error:', error);
  }
}

/**
 * Processes the server response containing order data. For each order,
 * validates the consignee address match and updates references.
 *
 * @param {object[]} orderDataArray - Array of order objects from the server.
 */
function processOrderData(orderDataArray) {
  try {
    orderDataArray.forEach(function (order) {
      if (checkForMatchingConsignee(order)) {
        updateReferencesWithOrderNumber(order.orderNumber);
      } else {
        showErrorAlert('ERROR 500: Could not add commodities for order ' + order.orderNumber + '. Address mismatch.');
      }
    });
  } catch (error) {
    console.error('ProcessOrderData error:', error);
  }
}

// ---------------------------------------------------------------------------
// Consignee matching
// ---------------------------------------------------------------------------

/**
 * Compares a customer order's delivery address against the current
 * shipment's consignee address fields. Returns true if they match
 * (ignoring extra whitespace).
 *
 * @param {object} customerOrder - The customer order with shipAddress1, shipCity, shipState, shipZipCode.
 * @returns {boolean} True if addresses match.
 */
function checkForMatchingConsignee(customerOrder) {
  try {
    var $con = $('name-address[nameaddress="vm.currentShipment.PackageDefaults.Consignee"]');

    var current = {
      address: $con.find('input[name="address1"]').val().trim().toUpperCase(),
      city:    $con.find('input[name="city"]').val().trim().toUpperCase(),
      state:   $con.find('input[name="stateProvince"]').val().trim().toUpperCase(),
      zip:     $con.find('input[name="postalCode"]').val().trim().substring(0, 5)
    };

    var incoming = {
      address: customerOrder.shipAddress1.trim().toUpperCase(),
      city:    customerOrder.shipCity.trim().toUpperCase(),
      state:   customerOrder.shipState.trim().toUpperCase(),
      zip:     customerOrder.shipZipCode.trim().substring(0, 5)
    };

    var a = JSON.stringify(current).replace(/ /g, '');
    var b = JSON.stringify(incoming).replace(/ /g, '');

    return a === b;
  } catch (error) {
    console.error('CheckForMatchingConsignee error:', error);
    return false;
  }
}


// ============================================================================
// FILE: ui.js
// ============================================================================

/**
 * UI — Show/hide loader, show/hide overlay, and Bootstrap dismissible
 * error alerts.
 *
 * These utilities are part of the "Tools" namespace and provide visual
 * feedback during async operations and error conditions.
 *
 * See also: alerts.js for the modal-based alert pattern.
 */

// ---------------------------------------------------------------------------
// Loader
// ---------------------------------------------------------------------------

/**
 * Shows the global loading spinner by toggling AngularJS CSS classes.
 *
 * @param {function} [callback] - Optional callback after showing.
 */
function showLoader(callback) {
  $('div.loading').removeClass('ng-hide').addClass('ng-show');
  if (typeof callback === 'function') callback();
}

/**
 * Hides the global loading spinner and the overlay.
 */
function hideLoader() {
  $('div.loading').removeClass('ng-show').addClass('ng-hide');
  hideOverlay();
}

// ---------------------------------------------------------------------------
// Overlay
// ---------------------------------------------------------------------------

/**
 * Shows a semi-transparent overlay over the page content, creating the
 * overlay element if it doesn't already exist.
 */
function showOverlay() {
  if ($('div.overlay').length === 0) {
    $('div.body-content').first().append(
      $('<div />')
        .addClass('overlay')
        .css({
          content:         '',
          position:        'fixed',
          top: 0, left: 0,
          width:           '100vw',
          height:          '100vh',
          backgroundColor: 'rgba(0, 0, 0, .25)',
          zIndex:          1049
        })
    ).show();
  } else {
    $('div.overlay').show();
  }
}

/**
 * Hides the overlay.
 */
function hideOverlay() {
  $('div.overlay').hide();
}

// ---------------------------------------------------------------------------
// Dismissible error alert
// ---------------------------------------------------------------------------

/**
 * Creates and displays a Bootstrap-style dismissible error alert at the
 * bottom of the page.
 *
 * @param {string} errorMessage - The error message to display.
 */
function showErrorAlert(errorMessage) {
  var id = 'alert-' + Math.random().toString(36).substring(2, 10);

  var $alert = $('<div />')
    .attr('id', id)
    .addClass('alert alert-dismissible alert-bottom alert-danger ng-scope')
    .css({ zIndex: 1550, cursor: 'pointer' })
    .attr('role', 'alert')
    .append(
      $('<button />').attr('type', 'button').addClass('close')
        .append($('<span />').attr('aria-hidden', 'false').text('x'))
        .append($('<span />').addClass('sr-only').text('Close'))
    )
    .append(
      $('<div />').append(
        $('<div />').addClass('ng-scope alert-message')
          .text(errorMessage)
          .prepend(
            $('<span />')
              .css('padding-right', '10px')
              .addClass('glyphicon glyphicon-alert custom-error-icon')
          )
      )
    );

  $('body').append($alert.show());
}

// ---------------------------------------------------------------------------
// Random string helper
// ---------------------------------------------------------------------------

/**
 * Generates a random alphanumeric string suitable for DOM element IDs.
 *
 * @returns {string}
 */
function getRandomAlphaNumericString() {
  return 'id-' + Math.random().toString(36).substring(2, 10);
}


// ============================================================================
// FILE: user_context_filtering.js
// ============================================================================

/**
 * User Context Filtering — PreSearchHistory user/traveler filters,
 * GetUserContext, setMailroomOrTraveler, GetCustom data accessors,
 * and cost-center / origin-address helpers.
 *
 * Patterns found:
 *   - ABB Optical / AIG / COREBRIDGE / Applied Material: Filter history by UserId
 *   - AlstonBird: Filter history by MiscReference20 for traveler profiles
 *   - Amplifon: Load cost center from CustomData, copy origin address to return address
 *   - Avery / APPNEXUS: Shipment helper class with UserRef/ToRef getters
 *   - ASEA: getUserContext via GET to /api/usercontext
 */

// ---------------------------------------------------------------------------
// User-context retrieval
// ---------------------------------------------------------------------------

/**
 * Returns the user context from the view model (checks both common locations).
 *
 * @param {object} vm - The view model.
 * @returns {object|undefined} { CompanyId, UserId }
 */
function getUserContext(vm) {
  if (vm.userContext)      return vm.userContext;
  if (vm.UserInformation) return vm.UserInformation;
}

/**
 * Fetches the user context from the server via a GET request.
 *
 * @param {object} client - The client service (client.config.ShipExecServiceUrl, client.getAuthorizationToken).
 */
function fetchUserContext(client) {
  var url   = client.config.ShipExecServiceUrl.replace('ShippingService', 'usercontext/GET');
  var token = client.getAuthorizationToken();
  client.userContext = $.get({ url: url, headers: token, async: false }).responseJSON;
}

// ---------------------------------------------------------------------------
// Mailroom / Traveler detection
// ---------------------------------------------------------------------------

/**
 * Determines whether the profile is Mailroom or Traveler and returns
 * configuration flags.
 *
 * @param {string} profileName - The current profile name.
 * @returns {object} { IsMailroom, IsTraveler, LoadRadioButtonsIsHidden }
 */
function setMailroomOrTraveler(profileName) {
  return {
    IsMailroom:               profileName !== 'Traveler Profile',
    IsTraveler:               profileName === 'Traveler Profile',
    LoadRadioButtonsIsHidden: true
  };
}

// ---------------------------------------------------------------------------
// Custom data accessors
// ---------------------------------------------------------------------------

/**
 * Extracts a custom field value from either the User (OriginAddress) or
 * Consignee custom data.
 *
 * @param {string} fieldName       - The key to look up (e.g. "Custom1").
 * @param {string} source          - "User" or "To".
 * @param {object} shipmentRequest - The current shipment request.
 * @returns {string} The matching value, or empty string.
 */
function getCustom(fieldName, source, shipmentRequest) {
  var customData = null;
  if (source === 'User') customData = shipmentRequest?.PackageDefaults?.OriginAddress?.CustomData;
  if (source === 'To')   customData = shipmentRequest?.PackageDefaults?.Consignee?.CustomData;

  if (!customData) return '';
  var result = '';
  customData.forEach(function (item) {
    if (item.Key === fieldName) result = item.Value;
  });
  return result;
}

/**
 * Shipment helper class providing convenient getters for CustomData fields
 * (UserRef1 through UserRef10, ToRef1 through ToRef10).
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function Shipment(shipmentRequest) {
  this.ShipmentRequest = shipmentRequest;
}

/** @returns {string} */
Shipment.prototype.GetCustom = function (fieldName, source) {
  return getCustom(fieldName, source, this.ShipmentRequest);
};

/* Generate convenience getters for Custom1..Custom10 for both User and To */
[1,2,3,4,5,6,7,8,9,10].forEach(function (n) {
  Object.defineProperty(Shipment.prototype, 'UserRef' + n, {
    get: function () { return this.GetCustom('Custom' + n, 'User'); }
  });
  Object.defineProperty(Shipment.prototype, 'ToRef' + n, {
    get: function () { return this.GetCustom('Custom' + n, 'To'); }
  });
});

// ---------------------------------------------------------------------------
// Cost center and origin address (Amplifon)
// ---------------------------------------------------------------------------

/**
 * Loads a cost-center value from the user's CustomData and sets it as the
 * ShipperReference, disabling the field.
 *
 * @param {object} vm         - The view model.
 * @param {string} customKey  - The CustomData key (e.g. "CostCenter").
 */
function loadCostCenter(vm, customKey) {
  var customData = vm?.profile?.UserInformation?.Address?.CustomData;
  if (!customData) return;

  var entry = customData.find(function (item) { return item.Key === customKey; });
  if (!entry) return;

  vm.currentShipment.Packages[vm.packageIndex].ShipperReference = entry.Value;
  $('[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]').prop('disabled', true);
}

/**
 * Copies the user's origin address to the shipment's return address.
 *
 * @param {object} vm - The view model.
 */
function copyOriginToReturnAddress(vm) {
  var originAddress = vm?.profile?.UserInformation?.Address;
  if (!originAddress || $.isEmptyObject(originAddress)) return;
  vm.currentShipment.PackageDefaults.ReturnAddress = originAddress;
}


// ============================================================================
// FILE: validation.js
// ============================================================================

/**
 * Validation — Consignee address matching, reference field validation,
 * client-matter validation, custom state/province select replacement,
 * and various PreShip validation checks.
 *
 * Patterns found:
 *   - Consolidated orders: CheckForMatchingConsignee (address comparison)
 *   - AGCS: CheckValidation against FieldOptionValidations
 *   - AlstonBird: validateClientMatter with alert and focus
 *   - AlstonBird: Custom state/province <select> for US/CA
 *   - Altard State: OMS packing verification
 *   - D2M: International insurance toggle based on country
 */

// ---------------------------------------------------------------------------
// Consignee address matching
// ---------------------------------------------------------------------------

/**
 * Compares a customer order's shipping address against the current
 * consignee address in the UI. Ignores extra whitespace.
 *
 * @param {object} customerOrder - { shipAddress1, shipCity, shipState, shipZipCode }
 * @returns {boolean} True if addresses match.
 */
function checkForMatchingConsignee(customerOrder) {
  try {
    var $con = $('name-address[nameaddress="vm.currentShipment.PackageDefaults.Consignee"]');

    var current = JSON.stringify({
      address: $con.find('input[name="address1"]').val().trim().toUpperCase(),
      city:    $con.find('input[name="city"]').val().trim().toUpperCase(),
      state:   $con.find('input[name="stateProvince"]').val().trim().toUpperCase(),
      zip:     $con.find('input[name="postalCode"]').val().trim().substring(0, 5)
    }).replace(/ /g, '');

    var incoming = JSON.stringify({
      address: customerOrder.shipAddress1.trim().toUpperCase(),
      city:    customerOrder.shipCity.trim().toUpperCase(),
      state:   customerOrder.shipState.trim().toUpperCase(),
      zip:     customerOrder.shipZipCode.trim().substring(0, 5)
    }).replace(/ /g, '');

    return current === incoming;
  } catch (error) {
    console.error('CheckForMatchingConsignee error:', error);
    return false;
  }
}

// ---------------------------------------------------------------------------
// Reference validation
// ---------------------------------------------------------------------------

/**
 * Validates a reference value against a list of allowed FieldOptionValidation
 * entries. Throws an error if the value is not found.
 *
 * @param {string}   value          - The value to validate.
 * @param {object[]} validationList - Array of { Value } objects.
 * @throws {object} With message and errorCode.
 */
function checkValidation(value, validationList) {
  for (var i = 0; i < validationList.length; i++) {
    if (validationList[i].Value === value) return;
  }
  throw { message: 'Unable to validate shipment', errorCode: '001' };
}

/**
 * Returns the value unchanged if truthy, or an empty string if falsy.
 * Used to normalise potentially-null reference values before concatenation.
 *
 * @param {*} value
 * @returns {string}
 */
function checkReference(value) {
  return value || '';
}

// ---------------------------------------------------------------------------
// Client-matter validation (AlstonBird)
// ---------------------------------------------------------------------------

/**
 * Validates the ShipperReference (client matter) field against the
 * FieldOptionValidations list. If valid, calls saveLicensePlate();
 * otherwise alerts the user and focuses the field.
 *
 * @param {object} vm - The view model.
 */
function validateClientMatter(vm) {
  var cm = vm.currentShipment.Packages[0].ShipperReference;

  if (cm === null) {
    alert('Client Matter field is required. Please enter a valid client matter number!');
    setFocus('input[name="ShipperReference_txt"]');
    return;
  }

  var validationList = vm.fieldOptions.ShipperReference.FieldOptionValidations;
  var found = false;

  for (var i = 0; i < validationList.length; i++) {
    if (validationList[i].Value === cm) {
      found = true;
      break;
    }
  }

  if (found) {
    vm.saveLicensePlate();
  } else {
    alert('Client Matter field is invalid. Please enter a valid client matter number!');
    setFocus('input[name="ShipperReference_txt"]');
  }
}

// ---------------------------------------------------------------------------
// Custom state/province select (AlstonBird)
// ---------------------------------------------------------------------------

/**
 * Replaces the default state/province text input with a <select> dropdown
 * that shows US states, Canadian provinces, or a freeform input based on
 * the selected country.
 *
 * @param {string}   stateProvinceSelector - jQuery selector for the original input.
 * @param {object}   usStates              - { abbreviation: name } for US states.
 * @param {object}   canadianProvinces     - { abbreviation: name } for CA provinces.
 * @param {function} changeCallback        - Called with the country code when switching.
 */
function initCustomStateProvinceSelect(stateProvinceSelector, usStates, canadianProvinces, changeCallback) {
  var interval = setInterval(function () {
    var $original = $(stateProvinceSelector);
    if (!$original.length) return;
    clearInterval(interval);

    if ($original.attr('name') === 'StateProvince') {
      console.warn('Custom State/Province does not work with FieldOption validation');
      return;
    }

    var attrs = {
      'class':    $original.attr('class'),
      name:       $original.attr('name'),
      type:       $original.attr('type'),
      required:   $original.attr('required') !== undefined,
      readonly:   $original.attr('readonly') !== undefined
    };

    var $usSelect = buildSelectForCountry('US', attrs, usStates);
    var $caSelect = buildSelectForCountry('CA', attrs, canadianProvinces);

    changeCallback($original, $usSelect, $caSelect);
  }, 10);

  function buildSelectForCountry(country, attrs, list) {
    var $select = $('<select />').attr($.extend({}, attrs, { country: country }));
    $select.append(new Option('', ''));
    $.each(list, function (val, text) {
      $select.append(new Option(text, val));
    });
    return $select;
  }
}

// ---------------------------------------------------------------------------
// International insurance toggle (D2M)
// ---------------------------------------------------------------------------

/**
 * Sets or clears the commercial-invoice insurance option based on whether
 * the consignee country is outside the US.
 *
 * @param {boolean} enabled          - Whether return delivery is enabled.
 * @param {object}  shipmentRequest  - The current shipment request.
 */
function setReturnDeliveryInsurance(enabled, shipmentRequest) {
  if (!shipmentRequest || !enabled) return;

  var country = shipmentRequest.PackageDefaults.Consignee.Country.toUpperCase();
  var isInternational = (country !== 'US' && country !== 'USA');

  $('select option[value="number:1"]').prop('selected', isInternational);
}

// ---------------------------------------------------------------------------
// Focus helper
// ---------------------------------------------------------------------------

function setFocus(selector) {
  $(selector).focus();
}


// ============================================================================
// FILE: validation_alerts.js
// ============================================================================

/**
 * Validation Alerts — Combined validation checks with user-facing alerts
 * and D2M authorization verification.
 *
 * This file is a cross-reference. The canonical implementations live in:
 *   - validation.js (checkForMatchingConsignee, checkValidation,
 *     validateClientMatter, setReturnDeliveryInsurance)
 *   - alerts.js / ui.js (showErrorAlert)
 *   - service_filtering.js (isCurrentShipperD2MEnabled,
 *     isCurrentUserD2MEnabled, isCurrentShipmentD2M)
 *
 * See those files for the canonical implementations.
 */


