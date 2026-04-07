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
