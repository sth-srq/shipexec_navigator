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
