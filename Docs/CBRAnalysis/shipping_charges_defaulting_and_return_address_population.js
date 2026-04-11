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
