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
