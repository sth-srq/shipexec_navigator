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
