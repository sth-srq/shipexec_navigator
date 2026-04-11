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
