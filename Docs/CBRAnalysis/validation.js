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
