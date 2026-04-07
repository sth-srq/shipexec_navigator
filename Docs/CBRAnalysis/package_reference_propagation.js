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
