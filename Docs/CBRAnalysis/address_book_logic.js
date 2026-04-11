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
