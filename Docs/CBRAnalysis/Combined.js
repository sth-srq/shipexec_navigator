// ============================================================================
// Combined.js — Consolidated CBR Helper Library
//
// Merged from All.js (48 source files) by de-duplicating identical and
// near-identical functions, removing cross-reference-only stubs, and
// keeping one canonical version of every helper.
//
// Original: All.js  (4 497 lines, 48 files, many duplicates)
// Result:   Combined.js (single-copy of every unique function)
//
// Organisation
// ─────────────
//   1. Utilities        — arrays, strings, comparators, dates
//   2. Logging          — LogLevel, Logger, console / server output
//   3. UI               — loader, overlay, alerts, focus
//   4. DOM              — poll / wait for elements, event helpers
//   5. API              — thin-client requests, UserMethod, ajaxGet
//   6. User Context     — session, mailroom / traveler, custom data
//   7. Address Book     — search triggers, consignee matching
//   8. References       — ShipperReference, CustomData propagation
//   9. Notifications    — email notification defaults
//  10. Validation       — field validation, client-matter checks
//  11. Services         — weight-based selection, filtered lists
//  12. D2M              — Direct-to-Mobile checks and enablement
//  13. Commodities      — build, add, remove, move, consolidate
//  14. Consolidated Ord — dialog, save, process
//  15. History          — search filters, anonymisation, links
//  16. Manifest & EOD   — close manifest, end-of-day
//  17. Batch            — lookup, selection, custom processing
//  18. Shipping Profile — CRUD, apply, return-label creation
//  19. Load State       — capture / restore across PreLoad / PostLoad
//  20. Third-Party Bill — billing rules, EU logic
//  21. Return Delivery  — address, config toggle, insurance
//  22. Printing         — traveler labels, ZPL, document download
//  23. Files            — upload, download, paperless
//  24. Camera           — video stream, capture, stop
//  25. NMFC / LTL       — dimension matching, dry ice, references
//  26. Shipment Class   — convenience getters for CustomData
// ============================================================================


// ============================================================================
// 1. UTILITIES
// ============================================================================

/**
 * Splits a delimited string into an array of unique, trimmed,
 * non-empty values.
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
 * Converts an array of strings into a comma-separated string
 * with duplicates removed.  A trailing comma is included.
 *
 * @param {string[]} inputArray
 * @returns {string}
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
 * De-duplicates a comma-separated string.
 *
 * @param {string} inputString
 * @returns {string}
 */
function getUniqueCSVStringFromString(inputString) {
  return getUniqueCSVStringFromArray(getUniqueArrayFromString(inputString));
}

/**
 * Returns a comparator that sorts objects by the given property.
 *
 * @param {string} key - Property name to sort by.
 * @returns {function} Comparator for Array.prototype.sort().
 */
function compareByKey(key) {
  return function (a, b) {
    if (a[key] > b[key]) return 1;
    if (a[key] < b[key]) return -1;
    return 0;
  };
}

/**
 * Returns the value unchanged if truthy, otherwise an empty string.
 * Used to normalise potentially-null reference values.
 *
 * @param {*} value
 * @returns {string}
 */
function returnPropertyValue(value) {
  return value || '';
}

/**
 * Generates a random alphanumeric string for DOM element IDs.
 *
 * @returns {string}
 */
function getRandomAlphaNumericString() {
  return 'id-' + Math.random().toString(36).substring(2, 10);
}

/**
 * Returns the current date/time as a locale string with timezone.
 *
 * @returns {string} e.g. "4/7/2026, 14:30:00 EDT"
 */
function dateTimeStamp() {
  return new Date().toLocaleString('en-US', {
    timeZoneName: 'short',
    hour12: false
  });
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
 * Creates a shipdate object for the current date.
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

/**
 * Translates verbose unit-of-measurement strings to abbreviations.
 *
 * @param {string} value - e.g. "EACH", "YARDS", "PAIR".
 * @returns {string} e.g. "EA", "YD", "PR".
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
// 2. LOGGING
// ============================================================================

var LogLevel = {
  Error: 'Error',
  Info:  'Info',
  Debug: 'Debug',
  Trace: 'Trace',
  Fatal: 'Fatal'
};

var Logger = {

  /** @private */
  _serverLogging: false,

  /**
   * Enables or disables server-side debug logging.
   *
   * @param {boolean} enabled
   */
  setServerDebugMode: function (enabled) {
    console.log('Logging to server:', enabled);
    this._serverLogging = enabled;
  },

  /**
   * Logs a message to the console and optionally to the server.
   *
   * @param {object} entry - { Source, Error?, Message?, Data?, LogLevel? }
   */
  Log: function (entry) {
    try {
      if (entry.Error) {
        entry.LogLevel = LogLevel.Error;
        entry.Error = { name: entry.Error.name, message: entry.Error.message };
      } else if (!entry.LogLevel) {
        entry.LogLevel = LogLevel.Info;
      }

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

      if (this._serverLogging) {
        var ajaxData = {
          UserContext: getCurrentUserContext(),
          Data: JSON.stringify({
            ServerMethod: 'AddClientEntry',
            MessageObject: entry
          })
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

      hideLoader();
    } catch (error) {
      console.log(error.message);
    }
  },

  /**
   * Logs the start of a method with an optional callee chain.
   *
   * @param {string} method
   * @param {string} [calleeMethod]
   */
  logStartMethod: function (method, calleeMethod) {
    if (calleeMethod) {
      console.log('...STARTING ' + method + ' called by ' + calleeMethod);
    } else {
      console.log('...STARTING ' + method);
    }
  },

  /**
   * Logs an informational message indented for sub-method detail.
   *
   * @param {string} message
   */
  logMethodInfo: function (message) {
    console.log('      ' + message);
  }
};

/**
 * Safely decodes a base64-encoded JSON string from the server.
 *
 * @param {string} data
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
// 3. UI
// ============================================================================

/**
 * Shows the global loading spinner.
 *
 * @param {function} [callback]
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

/**
 * Shows a semi-transparent overlay, creating it if it doesn't exist.
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

/**
 * Creates a Bootstrap-style dismissible error alert at the bottom
 * of the page.
 *
 * @param {string} errorMessage
 */
function showErrorAlert(errorMessage) {
  var id = getRandomAlphaNumericString();

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

/**
 * Shows a modal alert with a title and message.
 *
 * @param {string} title
 * @param {string} message
 * @param {object} $alertModal - jQuery reference to the modal.
 */
function showModalAlert(title, message, $alertModal) {
  vm.currentShipment.AlertTitle = title;
  vm.currentShipment.AlertMessage = message;
  $alertModal.modal('show');
}

/**
 * Sets focus on a DOM element.
 *
 * @param {string} selector - jQuery selector.
 */
function setFocus(selector) {
  $(selector).focus();
}


// ============================================================================
// 4. DOM
// ============================================================================

/**
 * Waits for a DOM element to appear, optionally sets a value,
 * focuses it, and/or invokes a callback.
 *
 * @param {string}   selector
 * @param {boolean}  [focusAfter=false]
 * @param {string}   [defaultValue]
 * @param {number}   [timeoutSeconds]
 * @param {function} [callback]
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
 * Waits for a <select> to have more than minOptions loaded,
 * then optionally selects by index or value.
 *
 * @param {string}   selectSelector
 * @param {number}   [minOptions=1]
 * @param {number}   [indexToSelect]
 * @param {string}   [valueToSelect]
 * @param {boolean}  [clearFirst=false]
 * @param {number}   [timeoutSeconds]
 * @param {function} [callback]
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
 * Checks whether a specific event handler is already attached.
 *
 * @param {jQuery}   $element
 * @param {string}   eventName - e.g. "click", "change".
 * @param {function} handler
 * @returns {boolean}
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
// 5. API
// ============================================================================

/**
 * Makes an API request by reading the service URL from config.json,
 * then POSTing with a Bearer token.
 *
 * @param {string}  method  - API method path (e.g. "UserMethod").
 * @param {object}  data    - POST data.
 * @param {boolean} isAsync
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

/**
 * Makes a POST request using a pre-configured client object.
 *
 * @param {string}  method
 * @param {object}  data
 * @param {boolean} isAsync
 * @param {object}  client - { config.ShipExecServiceUrl, getAuthorizationToken() }
 * @returns {jqXHR}
 */
function thinClientApiRequest(method, data, isAsync, client) {
  return $.post({
    url:     client.config.ShipExecServiceUrl + '/' + method,
    data:    data,
    async:   isAsync,
    headers: client.getAuthorizationToken()
  });
}

/**
 * Makes a UserMethod request with loader, error handling, and callback.
 *
 * @param {string}   requestMethod
 * @param {string}   requestData
 * @param {boolean}  [isAsync=true]
 * @param {function} [callback]
 * @returns {object|undefined}
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

/**
 * Simple synchronous GET helper.
 *
 * @param {string} url
 * @returns {object}
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


// ============================================================================
// 6. USER CONTEXT
// ============================================================================

var _currentUserContext = {};

/**
 * Populates the current user context (CompanyId, UserId) from the
 * view model or via a synchronous GET.
 *
 * @param {object}  viewModel
 * @param {boolean} includeUser - Fetch UserId via AJAX if unavailable.
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

/**
 * Returns the user context from whichever property is populated.
 *
 * @param {object} vmInstance
 * @returns {object|undefined}
 */
function getUserContext(vmInstance) {
  return vmInstance.userContext || vmInstance.UserInformation;
}

/**
 * Fetches the user context from the server via GET.
 *
 * @param {object} client - { config.ShipExecServiceUrl, getAuthorizationToken }
 */
function fetchUserContext(client) {
  var url   = client.config.ShipExecServiceUrl.replace('ShippingService', 'usercontext/GET');
  var token = client.getAuthorizationToken();
  client.userContext = $.get({ url: url, headers: token, async: false }).responseJSON;
}

/**
 * Determines whether the profile is Mailroom or Traveler.
 *
 * @param {string} profileName
 * @returns {object} { IsMailroom, IsTraveler, LoadRadioButtonsIsHidden }
 */
function setMailroomOrTraveler(profileName) {
  return {
    IsMailroom:               profileName !== 'Traveler Profile',
    IsTraveler:               profileName === 'Traveler Profile',
    LoadRadioButtonsIsHidden: true
  };
}

/**
 * Extracts a value from a CustomData key-value array.
 *
 * @param {string}   key        - The key to look up (case-insensitive).
 * @param {object[]} customData - Array of { Key, Value }.
 * @returns {string} The matching value, or empty string.
 */
function getCustomDataValue(key, customData) {
  if (!customData) return '';
  for (var i = 0; i < customData.length; i++) {
    if (customData[i].Key.toLowerCase() === key.toLowerCase()) {
      return customData[i].Value;
    }
  }
  return '';
}

/**
 * Extracts a custom field value from OriginAddress or Consignee
 * CustomData on the shipment request.
 *
 * @param {string} fieldName       - The CustomData key.
 * @param {string} source          - "User" for OriginAddress, "To" for Consignee.
 * @param {object} shipmentRequest
 * @returns {string}
 */
function getCustomField(fieldName, source, shipmentRequest) {
  var customData = null;
  if (source === 'User') customData = shipmentRequest?.PackageDefaults?.OriginAddress?.CustomData;
  if (source === 'To')   customData = shipmentRequest?.PackageDefaults?.Consignee?.CustomData;
  return getCustomDataValue(fieldName, customData);
}

/**
 * Stores the current view model reference, optionally initialises
 * the user context, and invokes a callback.
 *
 * @param {object}   viewModel
 * @param {boolean}  setUserCtx
 * @param {boolean}  includeUser
 * @param {function} [callback]
 */
function setCurrentViewModel(viewModel, setUserCtx, includeUser, callback) {
  if (setUserCtx) {
    setCurrentUserContext(viewModel, includeUser);
  }
  window._currentViewModel = viewModel;
  if (typeof callback === 'function') callback();
}


// ============================================================================
// 7. ADDRESS BOOK
// ============================================================================

/**
 * Binds a Tab-key handler to the Consignee code input so Tab
 * triggers the address book search.
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

/**
 * Binds Enter-key and focusout events to a code input to trigger
 * the consignee address book search.
 *
 * @param {string} codeInputSelector
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
 * Copies the user's origin address to the return address.
 *
 * @param {object} vm - The view model.
 */
function copyOriginToReturnAddress(vm) {
  var originAddress = vm?.profile?.UserInformation?.Address;
  if (!originAddress || $.isEmptyObject(originAddress)) {
    console.log('Error: Origin Address not found.');
    return;
  }
  vm.currentShipment.PackageDefaults.ReturnAddress = originAddress;
  console.log('User Origin Address copied to Return Address.');
}

/**
 * Compares a customer order's delivery address against the current
 * consignee address in the UI.  Ignores extra whitespace.
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

    if (current !== incoming) {
      Logger.Log({
        Source:  'checkForMatchingConsignee()',
        Message: 'Address mismatch for order ' + customerOrder.orderNumber,
        Data:    { current: current, incoming: incoming }
      });
    }

    return current === incoming;
  } catch (error) {
    Logger.Log({ Source: 'checkForMatchingConsignee()', Error: error });
    return false;
  }
}

/**
 * Reads the ship-notification email from the DOM input and writes
 * it to the first package.
 *
 * @param {object} shipmentRequest
 */
function syncShipNotificationEmail(shipmentRequest) {
  var sel = 'input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipNotificationAddressEmail"]';
  shipmentRequest.Packages[0].ShipNotificationAddressEmail = $(sel).val();
}

/**
 * Populates consignee address fields based on a reference key.
 *
 * @param {object} shipmentRequest
 * @param {string} referenceKey
 * @param {object} addressMap      - Map of key → address object.
 * @param {object} defaultAddress  - Fallback if key not found.
 */
function setConsigneeByReference(shipmentRequest, referenceKey, addressMap, defaultAddress) {
  var address   = addressMap[referenceKey] || defaultAddress;
  var consignee = shipmentRequest.PackageDefaults.Consignee;

  consignee.Company       = address.Company;
  consignee.Contact       = address.Contact;
  consignee.Address1      = address.Address1;
  consignee.City          = address.City;
  consignee.StateProvince = address.StateProvince;
  consignee.PostalCode    = address.PostalCode;
  if (address.Phone) consignee.Phone = address.Phone;
}


// ============================================================================
// 8. REFERENCES
// ============================================================================

/**
 * Sets the ShipperReference on a package from the user's custom data.
 *
 * @param {object} shipmentRequest
 * @param {number} packageIndex
 * @param {string} customDataKey - e.g. "Custom1", "Custom6".
 */
function setShipperReferenceFromCustomData(shipmentRequest, packageIndex, customDataKey) {
  var customData = shipmentRequest.PackageDefaults.OriginAddress.CustomData;
  var value = getCustomDataValue(customDataKey, customData);
  if (value !== '') {
    shipmentRequest.Packages[packageIndex].ShipperReference = value;
  }
}

/**
 * Appends an order number to ShipperReference and MiscReference7,
 * keeping only unique comma-separated values.
 *
 * @param {string} orderNumber
 */
function updateReferencesWithOrderNumber(orderNumber) {
  var $shipperRef = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]');
  var $miscRef7   = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].MiscReference7"]');

  var currentVal = $shipperRef.val() + ',' + orderNumber;
  var uniqueVal  = getUniqueCSVStringFromString(currentVal);

  $shipperRef.val(uniqueVal);
  $miscRef7.val(uniqueVal);
}

/**
 * Builds a compound ShipperReference from custom data and an
 * additional reference, then validates and applies to all packages.
 *
 * @param {object}   shipmentRequest
 * @param {string}   customValue
 * @param {string}   additionalRef
 * @param {object[]} validationList - Array of { Value }.
 */
function applyCompoundShipperReference(shipmentRequest, customValue, additionalRef, validationList) {
  var referenceValue = (customValue + ' ' + (additionalRef || '')).trim();

  for (var i = 0; i < shipmentRequest.Packages.length; i++) {
    shipmentRequest.Packages[i].ShipperReference = referenceValue;
  }

  validateAgainstFieldOptions(customValue, validationList);
}

/**
 * Stamps MiscReference15 on a package.
 *
 * @param {object} shipmentRequest
 * @param {number} packageIndex
 * @param {string} stampValue - From dateTimeStamp() or todayString().
 */
function stampMiscReference15(shipmentRequest, packageIndex, stampValue) {
  shipmentRequest.Packages[packageIndex].MiscReference15 = stampValue;
}

/**
 * Builds shipper and consignee reference strings from MiscReference
 * fields and applies them to all packages.
 *
 * @param {object} shipmentRequest
 * @param {string} userId
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


// ============================================================================
// 9. NOTIFICATIONS
// ============================================================================

/**
 * Sets all notification email fields on a package.
 *
 * @param {object} pkg
 * @param {string} email
 */
function setNotificationDefaults(pkg, email) {
  pkg.ShipNotificationEmail                     = true;
  pkg.ShipNotificationAddressEmail              = email;
  pkg.DeliveryNotificationEmail                 = true;
  pkg.DeliveryNotificationAddressEmail          = email;
  pkg.DeliveryExceptionNotificationEmail        = true;
  pkg.DeliveryExceptionNotificationAddressEmail = email;
}

/**
 * Binds notification checkbox clicks to auto-populate the
 * corresponding email field.
 *
 * @param {object}   vm
 * @param {function} getUserName - Returns the current user's email.
 */
function bindNotificationCheckboxes(vm, getUserName) {
  var mappings = {
    '[property="ShipNotificationEmail"]':              '[name="shipNotificationAddressEmail"]',
    '[property="DeliveryExceptionNotificationEmail"]': '[name="DeliveryExceptionNotificationAddressEmail"]',
    '[property="DeliveryNotificationEmail"]':          '[name="DeliveryNotificationEmail"]'
  };

  Object.keys(mappings).forEach(function (checkboxSelector) {
    $('body').delegate(checkboxSelector, 'click', function () {
      $(mappings[checkboxSelector]).val(getUserName()).change();
    });
  });
}

/**
 * Reads the ship-notification email from the DOM.
 *
 * @returns {string}
 */
function getShipNotificationEmail() {
  return $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipNotificationAddressEmail"]').val();
}

/**
 * Persists the notification email on the package model.
 *
 * @param {object} shipmentRequest
 * @param {number} packageIndex
 */
function persistNotificationEmail(shipmentRequest, packageIndex) {
  shipmentRequest.Packages[packageIndex].ShipNotificationAddressEmail = getShipNotificationEmail();
}

/**
 * Sets the return delivery address email field.
 *
 * @param {string} email
 */
function setReturnDeliveryAddressEmail(email) {
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ReturnDeliveryAddressEmail"]').val(email);
}

/**
 * Gets the current return delivery address email.
 *
 * @returns {string}
 */
function getReturnDeliveryAddressEmail() {
  return $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ReturnDeliveryAddressEmail"]').val();
}


// ============================================================================
// 10. VALIDATION
// ============================================================================

/**
 * Validates a value against a list of allowed FieldOptionValidation
 * entries.  Throws if not found.
 *
 * @param {string}   value
 * @param {object[]} validationList - Array of { Value }.
 * @throws {object} { message, errorCode }
 */
function validateAgainstFieldOptions(value, validationList) {
  for (var i = 0; i < validationList.length; i++) {
    if (validationList[i].Value === value) return;
  }
  throw { message: 'Unable to validate shipment', errorCode: '001' };
}

/**
 * Validates the ShipperReference (client matter) against
 * FieldOptionValidations; alerts and refocuses on failure.
 *
 * @param {object} vm
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

/**
 * Replaces the default state/province text input with a <select>
 * for US states, Canadian provinces, or freeform for other countries.
 *
 * @param {string}   stateProvinceSelector
 * @param {object}   usStates          - { abbr: name }
 * @param {object}   canadianProvinces - { abbr: name }
 * @param {function} changeCallback
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
      'class':  $original.attr('class'),
      name:     $original.attr('name'),
      type:     $original.attr('type'),
      required: $original.attr('required') !== undefined,
      readonly: $original.attr('readonly') !== undefined
    };

    var $usSelect = buildSelectForCountry(attrs, usStates);
    var $caSelect = buildSelectForCountry(attrs, canadianProvinces);

    changeCallback($original, $usSelect, $caSelect);
  }, 10);

  function buildSelectForCountry(attrs, list) {
    var $select = $('<select />').attr(attrs);
    $select.append(new Option('', ''));
    $.each(list, function (val, text) {
      $select.append(new Option(text, val));
    });
    return $select;
  }
}


// ============================================================================
// 11. SERVICES
// ============================================================================

/**
 * Automatically selects a service based on package weights.
 * Packages >= threshold route to freight; otherwise express.
 * Throws if mixed weights prevent a single service.
 *
 * @param {object} shipmentRequest
 * @param {number} freightThresholdLbs - e.g. 70.
 * @param {string} expressServiceSymbol
 * @param {string} freightServiceSymbol
 */
function autoSelectServiceByWeight(shipmentRequest, freightThresholdLbs, expressServiceSymbol, freightServiceSymbol) {
  var hasFreight = false;
  var hasMixed   = false;

  shipmentRequest.Packages.forEach(function (pkg) {
    var weight = parseInt(pkg.Weight.Amount, 10);
    if (weight >= freightThresholdLbs) {
      hasFreight = true;
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
      pkg.ImportDelivery = true;
    });
  }
}

/**
 * Builds a filtered service list from comma-separated custom codes.
 *
 * @param {object}   profile
 * @param {string}   serviceCodesCSV - e.g. "2dn,grn,nda".
 * @param {function} getServiceSymbol - Maps code → carrier symbol.
 * @returns {object[]}
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

/**
 * Finds the rate result with SortIndex === 0 and returns its service.
 *
 * @param {object[]} rateResults
 * @returns {object|undefined} { Symbol }
 */
function selectServiceBySortIndex(rateResults) {
  for (var i = 0; i < rateResults.length; i++) {
    if (rateResults[i].SortIndex === 0) {
      return rateResults[i].PackageDefaults.Service;
    }
  }
}


// ============================================================================
// 12. D2M (DIRECT-TO-MOBILE)
// ============================================================================

/**
 * Checks whether the selected shipper is authorised for D2M.
 *
 * @param {object[]} authorizedShippers - Array of { ShipperSymbol }.
 * @returns {boolean}
 */
function isCurrentShipperD2MEnabled(authorizedShippers) {
  var currentSymbol = $('select[ng-change="vm.shipperChange()"]')
    .val().substring(7).toUpperCase();

  return authorizedShippers.some(function (s) {
    return s.ShipperSymbol === currentSymbol;
  });
}

/**
 * Checks whether the user has D2M enabled via CustomData.
 *
 * @param {object[]} customData
 * @param {string}   d2mCustomDataKey - e.g. "D2M".
 * @param {string}   enabledValue     - e.g. "YES".
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
 * Checks whether the current shipment qualifies as D2M
 * (user enabled + checkbox checked + shipper authorised).
 *
 * @param {object} config - { customData, d2mKey, enabledValue, authorizedShippers, checkboxId }
 * @returns {boolean}
 */
function isCurrentShipmentD2M(config) {
  var userOk    = isCurrentUserD2MEnabled(config.customData, config.d2mKey, config.enabledValue);
  var checked   = $('#' + config.checkboxId).is(':checked');
  var shipperOk = isCurrentShipperD2MEnabled(config.authorizedShippers);
  return userOk && checked && shipperOk;
}

/**
 * Enables D2M shipping via UserMethod and refreshes the profile.
 *
 * @param {function} apiRequest  - thinClientAPIRequest function.
 * @param {object}   userContext
 * @param {object}   vm          - vm.profile is updated on success.
 */
function enableD2MShipping(apiRequest, userContext, vm) {
  var params   = { ActionMessage: 'EnableD2MShipping' };
  var data     = { Data: JSON.stringify(params), UserContext: userContext };
  var response = apiRequest('UserMethod', data, false);

  if (response && response.responseJSON.ErrorCode !== 0) {
    alert('Error while executing UserMethod: ' + response.responseJSON.ErrorCode);
  } else {
    vm.profile = decodeReturnString(response.responseJSON.Data);
  }
}

/**
 * Hides the D2M template checkbox once the shipping date loads.
 *
 * @param {string} d2mTemplateId
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


// ============================================================================
// 13. COMMODITIES
// ============================================================================

/**
 * Creates a commodity content object from raw order data.
 *
 * @param {object} data - { skuId, quantity, unitPrice, weightLbs }
 * @returns {object} CommodityContents-compatible object.
 */
function buildCommodityContentObject(data) {
  return {
    Quantity:            data.quantity,
    ProductCode:         data.skuId,
    QuantityUnitMeasure: 'EA',
    UnitValue:           { Currency: 'USD', Amount: +data.unitPrice },
    UnitWeight:          { Units: 'LB', Amount: +data.weightLbs },
    UniqueId:            data.UniqueId,
    PkgIndex:            data.PkgIndex || 1,
    PVTotalWeight:       +data.quantity * +data.weightLbs,
    PVTotalValue:        +data.quantity * +data.unitPrice
  };
}

/**
 * Adds a commodity to a package and refreshes the UI.
 *
 * @param {object}   shipmentRequest
 * @param {number}   packageIndex
 * @param {object}   commodity
 * @param {function} refreshFn
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
 * Removes the last commodity from a package and refreshes the UI.
 *
 * @param {object}   shipmentRequest
 * @param {number}   packageIndex
 * @param {function} refreshFn
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

/**
 * Refreshes the commodity list UI by clicking the active tab
 * and toggling the pagination button.
 */
function refreshCommodityDisplay() {
  $('div.ui-tab-container > div.ng-isolate-scope > ul > li.active > a').click();
  $('#goods').find('div.ng-table-counts.btn-group').find('button:not(.active):first').click();
}

/**
 * Shows the assign-goods slideout panel, populates it, and
 * assigns all goods to the first package by default.
 *
 * @param {object}   context  - { packages, goods, viewModel }
 * @param {object[]} rawGoods
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
    pkg.Weight.Amount              = (pkg.Weight.Amount || 0) + commodity.PVTotalWeight;
    pkg.DeclaredValueAmount.Amount = (pkg.DeclaredValueAmount.Amount || 0) +
                                     Math.round(commodity.PVTotalValue * 100) / 100;
    $item.show();
  });

  $('#divAssignCommodities').show().animate({ left: 0 }, 400);
  $('.scan-input:visible').first().focus();
  $('#divAssignCommodities').find('[data-toggle="tooltip"]').tooltip();
}

/**
 * Moves a commodity from its current package to a destination,
 * recalculating weights and values.
 *
 * @param {object} context - { packages }
 * @param {jQuery} $li     - The list-item element.
 * @param {number} destBox - 1-based destination package index.
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

/**
 * Consolidates commodity contents across all packages for an
 * international shipment.  Duplicates by description are summed.
 *
 * @param {object} shipmentRequest
 */
function consolidateInternationalCommodities(shipmentRequest) {
  var isInternational = shipmentRequest.PackageDefaults.Consignee.Country !== 'US';
  if (!isInternational) return;

  var descriptionIndex = new Map();
  var consolidated     = [];

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
 * Adds a commodity from table-row cells or a bundle object.
 *
 * @param {object}  vm
 * @param {object}  cellsOrBundle
 * @param {number}  packageIndex
 * @param {boolean} isBundle
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
      UnitValue:           { Currency: cellsOrBundle.InternationalInfo.ExportCurrencyCode, Amount: price },
      UnitWeight:          { Units: 'LB', Amount: cellsOrBundle.UnitWeight },
      QuantityUnitMeasure: 'PC',
      OriginCountry:       cellsOrBundle.InternationalInfo.CountryOfOrigin,
      ProductCode:         cellsOrBundle.KitComponentPart,
      HarmonizedCode:      cellsOrBundle.InternationalInfo.ExportTariffCode,
      Quantity:            cellsOrBundle.UnitsPerAssembly,
      Description:         cellsOrBundle.KitComponentPartDesc,
      packageIndex:        packageIndex
    };
  } else {
    var cells = cellsOrBundle;
    var price = (cells[17].textContent || cells[17].textContent !== '')
      ? cells[17].textContent
      : cells[4].textContent;

    commodity = {
      UnitValue:           { Currency: cells[15].textContent, Amount: price },
      UnitWeight:          { Units: 'LB', Amount: cells[7].textContent },
      QuantityUnitMeasure: 'PC',
      OriginCountry:       cells[14].textContent,
      ProductCode:         cells[1].textContent,
      HarmonizedCode:      cells[16].textContent,
      Quantity:            cells[10].textContent,
      Description:         cells[5].textContent,
      packageIndex:        packageIndex
    };
  }

  var existing = vm.currentShipment.Packages[packageIndex].CommodityContents || [];
  existing.push(commodity);
  vm.currentShipment.Packages[packageIndex].CommodityContents = existing;
}

/** @private */
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
// 14. CONSOLIDATED ORDERS
// ============================================================================

/**
 * Opens the consolidated-orders modal, pre-populating with
 * existing order numbers from ShipperReference.
 */
function showConsolidatedOrderDialog() {
  showOverlay();
  $('#selectConsolidatedOrders').empty();

  var shipperRef     = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]').val();
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
 * Adds a unique order number to the consolidated-orders select box.
 *
 * @returns {boolean} false if duplicate or empty.
 */
function addOrderNumberToList() {
  var value = $('#textOrderNumber').val();
  if (!value || value.length === 0) return false;

  var exists = false;
  $('#selectConsolidatedOrders option').each(function () {
    if (this.text === value) {
      exists = true;
      return false;
    }
  });

  if (exists) {
    $('#textOrderNumber').val('').focus();
    return false;
  }

  $('#selectConsolidatedOrders').append(
    $('<option />').val(value).text(value).addClass('not-processed').trigger('change')
  ).promise().done(function () {
    $('#textOrderNumber').val('').focus();
  });
}

/**
 * Collects unprocessed orders and sends them to the server.
 *
 * @param {function} makeRequest     - UserMethod request function.
 * @param {function} processCallback
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
      showErrorAlert('ERROR: No unique orders to pull from the server.');
    }
  } catch (error) {
    Logger.Log({ Source: 'saveConsolidatedOrderNumbers()', Error: error });
  }
}

/**
 * Processes order data from the server — validates consignee match,
 * updates references.
 *
 * @param {object[]} orderDataArray
 */
function processOrderData(orderDataArray) {
  try {
    orderDataArray.forEach(function (order) {
      if (checkForMatchingConsignee(order)) {
        updateReferencesWithOrderNumber(order.orderNumber);
      } else {
        showErrorAlert('ERROR 500: Address mismatch for order ' + order.orderNumber);
      }
    });
  } catch (error) {
    Logger.Log({ Source: 'processOrderData()', Error: error });
  }
}

/**
 * Closes the consolidated orders dialog without saving.
 */
function closeDialogWithoutSaving() {
  $('#selectConsolidatedOrders').empty();
  $('#divModalConsolidateShipments').hide();
  hideLoader();
}

/**
 * Creates the "Consolidate Orders" toolbar button.
 *
 * @param {boolean} logToServer
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

  Logger.setServerDebugMode(logToServer);
}


// ============================================================================
// 15. HISTORY
// ============================================================================

/**
 * Adds a UserId filter to history search criteria.
 *
 * @param {object} searchCriteria
 * @param {string} userId
 * @param {number} [operator=0] - 0 = equals, 5 = contains.
 */
function filterHistoryByUser(searchCriteria, userId, operator) {
  searchCriteria.WhereClauses.push({
    FieldName:  'UserId',
    FieldValue: userId,
    Operator:   operator || 0
  });
}

/**
 * Adds a branch-number filter from CustomData.
 *
 * @param {object}   searchCriteria
 * @param {string}   customDataKey
 * @param {object[]} customData
 * @param {function} getValueByKey
 */
function filterHistoryByBranch(searchCriteria, customDataKey, customData, getValueByKey) {
  var branchNo = getValueByKey(customDataKey, customData);
  if (branchNo) {
    searchCriteria.WhereClauses.push({
      FieldName:  'MiscReference20',
      FieldValue: branchNo,
      Operator:   5
    });
  }
}

/**
 * Sets the history start date to N days ago.
 *
 * @param {object} vm
 * @param {object} searchCriteria
 * @param {number} daysBack
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

/**
 * Hides apportioned totals from non-admin users.
 *
 * @param {object[]} packages
 * @param {string}   currentProfile
 * @param {string}   adminProfileName
 */
function anonymizeApportionedTotals(packages, currentProfile, adminProfileName) {
  if (currentProfile === adminProfileName) return;
  packages.forEach(function (pkg) {
    pkg.ApportionedTotal = { Amount: null, Currency: null };
  });
}

/**
 * Masks tracking numbers (chars 4–9 → "XXXXXX") using a MutationObserver.
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

/**
 * Sets CantPrint = true on packages older than maxDays.
 *
 * @param {object[]} packages
 * @param {number}   maxDays
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

/**
 * Adds clickable tracking links to the detailed history table.
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

/**
 * Applies a bulk filter to batch-detail table rows.
 *
 * @param {object} filterCriteria - Key/value pairs (keys are column titles).
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
 * Clears any active bulk filter.
 */
function clearBulkFilter() {
  $('#tableBatchDetailItems tr:hidden').fadeIn('fast');
  $('div[title="Filter"] input[type="text"]').each(function () {
    $(this).val('').change();
  });
}

/**
 * Initialises the history page with search and report-change handlers.
 *
 * @param {object}   vm
 * @param {object}   thinClient - { httpClient, adminProfileName, loadData }
 */
function initHistoryPage(vm, thinClient) {
  vm.history = {
    searchButton:   'button:contains("Search")',
    reportSelector: '#reports'
  };

  $(document).on('click', vm.history.searchButton, onReportChange);
  $(document).on('change', vm.history.reportSelector, onReportChange);

  /* Restrict reports for non-admin profiles */
  if (vm.profile.Name !== thinClient.adminProfileName) {
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
      if (thinClient.loadData) thinClient.loadData.show();
    });
  }

  function getSites() {
    var sites = vm.UserInformation.SiteId
      ? vm.sites.filter(function (s) { return s.Id === vm.UserInformation.SiteId; })
      : vm.sites;
    return sites.map(function (s) { return { name: s.Name }; });
  }
}

/**
 * Enables a "duplicate" action on history rows.
 *
 * @param {object} cbr - { historyShipment, bulk }
 */
function enableHistoryDuplicate(cbr) {
  $('body').delegate('table a[action-duplicate]', 'click', function () {
    cbr.historyShipment = $(this).attr('action-duplicate');
    document.location = '#!/shipping';
  });
}

/**
 * Restores a duplicated history shipment on NewShipment.
 *
 * @param {object} cbr
 * @param {object} vm
 * @param {object} shipmentRequest
 * @param {string} defaultDescription
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
// 16. MANIFEST & EOD
// ============================================================================

/**
 * Closes manifests for all shippers under a single carrier.
 *
 * @param {object}   clientService
 * @param {object[]} allShippers
 * @param {string}   selectedCarrierSymbol
 * @param {string}   companyId
 * @param {function} showAlert
 */
function closeAllShippers(clientService, allShippers, selectedCarrierSymbol, companyId, showAlert) {
  var authToken = clientService.authorizationToken();
  var lastResponse;

  allShippers.forEach(function (shipper) {
    var carrierParams = { ShipperId: shipper.Id, CompanyId: companyId };
    var carrierResult = $.post({ url: clientService.config.ApiUrl + '/api/ShippingService/GetShipperCarriers', data: carrierParams, async: false, headers: authToken }).responseJSON;

    if (!carrierResult.Carriers || carrierResult.Carriers.length === 0) return;

    var manifestParams = {
      Carrier:         selectedCarrierSymbol,
      Shipper:         shipper.Symbol,
      SearchCriteria:  { OrderByClauses: [{ FieldName: 'ShipDate', Direction: 'DESC' }] },
      IncludeImported: false,
      CompanyId:       companyId
    };
    var manifestResult = $.post({ url: clientService.config.ApiUrl + '/api/ShippingService/GetManifestItems', data: manifestParams, async: false, headers: authToken }).responseJSON;

    if (!manifestResult.ManifestItems || manifestResult.ManifestItems.length === 0) return;

    var items = manifestResult.ManifestItems.map(function (m) {
      return { Attributes: m.Attributes, ShipDate: m.ShipDate, Symbol: m.Symbol, Name: m.Name };
    });

    var closeParams = {
      Carrier:       selectedCarrierSymbol,
      ManifestItems: items,
      Shipper:       shipper.Symbol,
      Print:         true,
      UserParams:    {},
      CompanyId:     companyId
    };
    lastResponse = $.post({ url: clientService.config.ApiUrl + '/api/ShippingService/CloseManifest', data: closeParams, async: false, headers: authToken }).responseJSON;
  });

  displayCloseManifestResult(lastResponse, showAlert, false);
  $('#loaderspinnerImg').hide();
}

/**
 * Closes manifests across all shippers and all carriers.
 */
function closeAllShippersAndCarriers(clientService, allShippers, allCarriers, companyId, showAlert) {
  allCarriers.forEach(function (carrier) {
    closeAllShippers(clientService, allShippers, carrier.Symbol, companyId, function () {});
  });

  displayCloseManifestResult(undefined, showAlert, true);
  $('#loaderspinnerImg').hide();
}

/**
 * Displays the close-manifest result message.
 *
 * @param {object|undefined} response
 * @param {function} showAlert
 * @param {boolean}  isAllCarriers
 */
function displayCloseManifestResult(response, showAlert, isAllCarriers) {
  var errorSel   = isAllCarriers ? '#divAllshippersAndCarriersErrorMsg' : '#divAllshippersErrorMsg';
  var failSel    = isAllCarriers ? '#divAllshipperAndCarrierssunsuccessfulcloseoutMsg' : '#divAllshippersunsuccessfulcloseoutMsg';
  var successSel = isAllCarriers ? '#divsuccessfulAllShippersAndAllCarriers' : '#divsuccessfulAllShippers';

  $('#divAllshippersErrorMsg, #divAllshippersunsuccessfulcloseoutMsg, #divAllshippersAndCarriersErrorMsg, #divAllshipperAndCarrierssunsuccessfulcloseoutMsg, #divsuccessfulAllShippers, #divsuccessfulAllShippersAndAllCarriers').hide();

  if (!response || response.ErrorCode === -1) {
    $(errorSel).show();
    showAlert('ErrorModalCloseShippers');
  } else if (response.CloseManifestResults[0].ErrorCode !== 0) {
    $(failSel).show();
    showAlert('ErrorModalCloseShippers');
  } else {
    $(successSel).show();
    showAlert('successfulModalCloseShippers');
  }
}

/**
 * Submits an End-of-Day processing request.
 *
 * @param {object}   shipmentRequest
 * @param {function} httpClient
 * @param {object}   $modal - jQuery modal to hide on success.
 */
function processEndOfDay(shipmentRequest, httpClient, $modal) {
  shipmentRequest.Action = 'EOD';
  var data = { Data: JSON.stringify(shipmentRequest) };

  httpClient('UserMethod', data).then(function () {
    $modal.modal('hide');
  });
}


// ============================================================================
// 17. BATCH
// ============================================================================

/**
 * Retrieves the list of batches from the server (synchronous).
 *
 * @param {function} apiRequest
 * @param {string}   companyId
 * @returns {object[]}
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
 * Reads the selected batch name from the dropdown.
 *
 * @returns {string}
 */
function getSelectedBatchName() {
  return $('#cboBatches select option:selected').val().replace('string:', '');
}

/**
 * Binds the custom batch-processing button to a UserMethod call.
 *
 * @param {object} client - { config, userContext, getAuthorizationToken }
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
// 18. SHIPPING PROFILES
// ============================================================================

/**
 * Loads shipping profiles from the server.
 *
 * @param {function} httpClient
 * @param {function} callback - Receives parsed profile list.
 */
function loadShippingProfiles(httpClient, callback) {
  var payload = { Action: 'L', ShippingProfileName: '' };
  var data    = { Data: JSON.stringify(payload) };

  httpClient('UserMethod', data).then(function (ret) {
    callback(JSON.parse(atob(ret.Data)));
  });
}

/**
 * Saves or deletes a shipping profile.
 *
 * @param {string}   action          - "S" for save, "D" for delete.
 * @param {object}   shipmentRequest
 * @param {string}   profileName
 * @param {string}   [selService]    - Required for save.
 * @param {function} httpClient
 * @param {function} callback
 */
function saveOrDeleteShippingProfile(action, shipmentRequest, profileName, selService, httpClient, callback) {
  shipmentRequest.Action = action;
  shipmentRequest.ShippingProfileName = profileName;
  if (action === 'S') shipmentRequest.SelService = selService;

  var data = { Data: JSON.stringify(shipmentRequest) };
  httpClient('UserMethod', data).then(function (ret) {
    callback(JSON.parse(atob(ret.Data)));
  });
}

/**
 * Applies a saved shipping profile to the current shipment.
 *
 * @param {object}   vm
 * @param {object[]} shippingProfiles
 * @param {string}   profileName
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

  profile.PackageDefaults.Shipdate = currentShipdate();

  vm.currentShipment     = profile;
  vm.selectedServices    = vm.selectedServices || [];
  vm.selectedServices[0] = service;
}

/**
 * Creates a return label from the previous shipment.
 *
 * @param {object} vm - Must have lastShipmentRequest.
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


// ============================================================================
// 19. LOAD STATE
// ============================================================================

/**
 * Saves transient shipment fields before a load.
 *
 * @param {object} shipmentRequest
 * @returns {object} Snapshot to pass to restorePostLoadState.
 */
function capturePreLoadState(shipmentRequest) {
  return {
    shipper:   shipmentRequest.PackageDefaults.Shipper,
    batchName: shipmentRequest.Packages[0].MiscReference5,
    batchId:   $('#cboBatches select option:selected').val().replace('string:', '')
  };
}

/**
 * Restores previously saved fields after a load.
 *
 * @param {object} shipmentRequest
 * @param {object} savedState
 */
function restorePostLoadState(shipmentRequest, savedState) {
  shipmentRequest.PackageDefaults.Shipper    = savedState.shipper;
  shipmentRequest.Packages[0].MiscReference5 = savedState.batchName;
}

/**
 * After a load, checks for a server-side error and alerts the user.
 *
 * @param {object} shipmentRequest
 */
function showPostLoadError(shipmentRequest) {
  if (shipmentRequest.PackageDefaults.ErrorCode === 1) {
    alert(shipmentRequest.PackageDefaults.ErrorMessage);
    shipmentRequest.PackageDefaults.ErrorCode = 0;
  }
}

/**
 * After a load, checks UserData1 for an alert message.
 * A leading "~" uses the new-shipment modal.
 *
 * @param {object} shipmentRequest
 * @param {jQuery} $alertModal
 * @param {jQuery} $newShipModal
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

/**
 * After a void, restores the consignee email into the UI.
 *
 * @param {object} shipmentRequest
 */
function restoreConsigneeEmail(shipmentRequest) {
  $('input[ng-model="nameaddress.Email"]').val(
    shipmentRequest.PackageDefaults.Consignee.Email
  );
}


// ============================================================================
// 20. THIRD-PARTY BILLING
// ============================================================================

/**
 * Applies third-party billing rules based on origin/consignee countries.
 *
 * @param {object}   shipmentRequest
 * @param {function} isMemberOfEu
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
 * @param {object} shipmentRequest
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
 * Disables the third-party billing checkbox and button.
 */
function disableThirdPartyBillingElements() {
  $('input[type=checkbox][ng-model="vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('checked', false).attr('checked', false);
  $('input[type=button][ng-disabled="!vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('disabled', true).attr('disabled', true);
}


// ============================================================================
// 21. RETURN DELIVERY
// ============================================================================

/**
 * Copies the first shipper's address into the return-address form.
 *
 * @param {object} vm
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

    $('[name="Country"]:eq(1)').val('string:' + shipper.Country).trigger('change');
  }, 150);
}

/**
 * Enables return-delivery settings.
 *
 * @param {object} vm
 */
function setReturnDeliveryConfiguration(vm) {
  setDescription('Returned Goods');
  setReturnDeliveryCheckbox(true);
  loadReturnAddressFromShipper(vm);
}

/**
 * Clears return-delivery settings.
 */
function clearReturnDeliveryConfiguration() {
  setDescription('');
  setReturnDeliveryAddressEmail('');
  setReturnDeliveryCheckbox(false);
  $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].CommercialInvoiceMethod"]').val('');
}

/**
 * Sets or clears commercial-invoice insurance based on country.
 *
 * @param {boolean} enabled
 * @param {object}  shipmentRequest
 */
function setReturnDeliveryInsurance(enabled, shipmentRequest) {
  if (!shipmentRequest || !enabled) return;

  var country = shipmentRequest.PackageDefaults.Consignee.Country.toUpperCase();
  var isInternational = (country !== 'US' && country !== 'USA');

  $('select option[value="number:1"]').prop('selected', isInternational);
}

/**
 * Handles shipper change in D2M mode — toggles return-delivery.
 *
 * @param {string}  currentShipper
 * @param {string}  d2mShipperSymbol
 * @param {boolean} isD2MCapable
 * @param {object}  vm
 */
function onShipperChangeD2M(currentShipper, d2mShipperSymbol, isD2MCapable, vm) {
  if (isD2MCapable && currentShipper === d2mShipperSymbol) {
    setDescription('Returned Goods');
    loadReturnAddressFromShipper(vm);
  } else {
    setDescription('');
    setReturnDeliveryAddressEmail('');
    $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].CommercialInvoiceMethod"]').val('');
  }
}

/** @private */
function setDescription(value) {
  $('textarea[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]').val(value);
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]').val(value);
}

/** @private */
function setReturnDeliveryCheckbox(enabled) {
  $('select option[value="number:1"]').prop('selected', enabled);
}


// ============================================================================
// 22. PRINTING
// ============================================================================

/**
 * Sends the shipment to the server and opens the returned PDF
 * in a new tab.
 *
 * @param {object}   shipmentRequest
 * @param {function} apiRequest
 */
function printTravelerLabel(shipmentRequest, apiRequest) {
  var data     = { Data: JSON.stringify(shipmentRequest) };
  var result   = apiRequest('UserMethod', data, false).responseJSON;
  var response = JSON.parse(atob(result.Data));
  var pdf      = response.DocumentResponses[0].PdfData[0];
  var dataURI  = 'data:application/pdf;base64,' + pdf;

  window.open(dataURI, '_blank');
  alert('Delegate Label Created');
}

/**
 * Prepends ZPL printer-default commands to raw label data.
 *
 * @param {object} doc            - { DocumentSymbol, RawData }
 * @param {string} expectedSymbol
 */
function prependZplDefaults(doc, expectedSymbol) {
  if (doc.DocumentSymbol !== expectedSymbol || !doc.RawData) return;

  var printerDefaults = '^XA^LH0,0^XSY,Y^MD30^XZ\n';
  var originalRaw     = atou(doc.RawData);
  doc.RawData[0]      = utoa(printerDefaults + originalRaw);
}

/**
 * Downloads a file from the server via UserMethod.
 *
 * @param {string}   fileName
 * @param {function} httpClient
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
// 23. FILES
// ============================================================================

/**
 * Reads a carrier label image file as base64.
 *
 * @param {string}   fileInputSelector
 * @param {function} onReady - Receives (base64Data).
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
 * Uploads a replacement file to the server.
 *
 * @param {File}     file
 * @param {string}   serverKey
 * @param {function} httpClient
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
 * Binds a paperless file input to the view model.
 *
 * @param {string} fileInputSelector
 * @param {object} vmInstance
 */
function bindPaperlessFileInput(fileInputSelector, vmInstance) {
  $('body').on('change', fileInputSelector, vmInstance, function (e) {
    var file = e.target.files[0];
    if (!file) {
      e.data.paperless = null;
      return;
    }
    var reader       = new FileReader();
    reader.readAsDataURL(file);
    reader.onloadend = function () {
      e.data.paperless = { fileName: file.name, fileData: this.result.split(',')[1] };
    };
  });
}


// ============================================================================
// 24. CAMERA
// ============================================================================

/**
 * Starts the device camera and attaches the stream.
 *
 * @param {string} videoElementId
 * @param {object} videoModal - Stores zoom/track state.
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
    var video       = document.getElementById(videoElementId);
    video.srcObject = stream;

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
 * Captures the current video frame as base64 PNG.
 *
 * @param {string} videoElementId
 * @returns {Promise<string>}
 */
function captureImage(videoElementId) {
  var video        = document.getElementById(videoElementId);
  var track        = video.srcObject.getVideoTracks()[0];
  var imageCapture = new ImageCapture(track);

  return imageCapture.grabFrame().then(function (imageBitmap) {
    var canvas    = document.createElement('canvas');
    var ctx       = canvas.getContext('2d');
    canvas.width  = imageBitmap.width;
    canvas.height = imageBitmap.height;
    ctx.drawImage(imageBitmap, 0, 0);

    var dataUrl = canvas.toDataURL('image/png', 1.0);
    return dataUrl.replace(/^data:.+;base64,/, '');
  });
}

/**
 * Stops the camera stream and releases the video element.
 *
 * @param {string} videoElementId
 */
function stopVideoStream(videoElementId) {
  var video = document.getElementById(videoElementId);
  if (video.srcObject) {
    video.srcObject.getTracks().forEach(function (track) { track.stop(); });
    video.srcObject = null;
  }
}


// ============================================================================
// 25. NMFC / LTL
// ============================================================================

/**
 * Standard dimension pairs for LTL BOL comment matching.
 */
var LTL_DIMENSION_TABLE = [
  [34, 32], [48, 40], [52, 36],
  [12, 52], [14, 44], [18, 32],
  [18, 52], [28, 44], [37, 37]
];

/**
 * Matches package dimensions against the LTL dimension table.
 *
 * @param {number} length
 * @param {number} width
 * @param {number} height
 * @returns {string} e.g. "48x40x12" or "0x0x0".
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

/**
 * Configures an LTL package with NMFC description, BOL comment,
 * parent container code, and waybill number.
 *
 * @param {object} shipmentRequest
 * @param {number} packageIndex    - 1-based.
 * @param {string} nmfcDescription
 */
function configureLtlPackage(shipmentRequest, packageIndex, nmfcDescription) {
  var pkg = shipmentRequest.Packages[packageIndex - 1];
  if (!pkg) return;

  var d         = pkg.Dimensions || {};
  var dimString = matchLtlDimensions(d.Length, d.Width, d.Height);
  if (dimString !== '0x0x0') {
    pkg.BolComment = ((pkg.BolComment || '') + ' ' + dimString).trim();
  }

  pkg.Description = nmfcDescription + ' L' + packageIndex;

  if (!pkg.WaybillBolNumber) {
    pkg.WaybillBolNumber = pkg.MiscReference20;
  }

  if (pkg.MiscReference20 && !shipmentRequest.Packages[packageIndex]?.ParentContainerCode) {
    shipmentRequest.Packages[packageIndex] = shipmentRequest.Packages[packageIndex] || {};
    if (!shipmentRequest.Packages[packageIndex].ParentContainerCode) {
      shipmentRequest.Packages[packageIndex].ParentContainerCode = 'P' + pkg.MiscReference20 + '-' + packageIndex;
    }
  }
}

/**
 * Validates and configures dry-ice settings on all packages.
 *
 * @param {object} shipmentRequest
 * @throws {object} { message, errorCode }
 */
function configureDryIce(shipmentRequest) {
  var dryIceWeightRaw  = returnPropertyValue(shipmentRequest.Packages[0].MiscReference8);
  var consigneeCountry = shipmentRequest.PackageDefaults.Consignee.Country;

  shipmentRequest.Packages.forEach(function (pkg) {
    if (!pkg.MiscReference7) return;

    if (shipmentRequest.Packages.length > 1) {
      throw { message: 'Multiple Packages Not Supported with Dry Ice', errorCode: '' };
    }
    if (returnPropertyValue(pkg.MiscReference8) === '') {
      throw { message: 'Missing Dry Ice Weight', errorCode: '' };
    }

    pkg.DryIceWeight        = { Amount: dryIceWeightRaw.replace('kgs', ''), Units: 'KG' };
    pkg.DryIceRegulationSet = (consigneeCountry === 'US') ? 1 : 2;
    pkg.DryIcePurpose       = 2;
  });
}


// ============================================================================
// 26. SHIPMENT CLASS
// ============================================================================

/**
 * Convenience class providing getters for CustomData fields
 * (UserRef1–10, ToRef1–10).
 *
 * @param {object} shipmentRequest
 */
function Shipment(shipmentRequest) {
  this.ShipmentRequest = shipmentRequest;
}

Shipment.prototype.GetCustom = function (fieldName, source) {
  return getCustomField(fieldName, source, this.ShipmentRequest);
};

[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].forEach(function (n) {
  Object.defineProperty(Shipment.prototype, 'UserRef' + n, {
    get: function () { return this.GetCustom('Custom' + n, 'User'); }
  });
  Object.defineProperty(Shipment.prototype, 'ToRef' + n, {
    get: function () { return this.GetCustom('Custom' + n, 'To'); }
  });
});


// ============================================================================
// PAGE INITIALISATION HELPERS
// ============================================================================

/**
 * Routes PageLoaded to the correct initialisation.
 *
 * @param {string} location - e.g. "/shipping", "/history".
 * @param {object} vm
 * @param {object} [options]
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
 * Waits for the load-value input, focuses it, and initialises
 * custom elements.
 */
function focusOnLoadInput() {
  waitForElement(
    'input[type=text][ng-model="vm.loadValue"]',
    true, null, null,
    initializeCustomElements
  );
}

/**
 * Resets custom UI elements (e.g. rate-shopping checkbox).
 */
function initializeCustomElements() {
  $('#chkDoNotRateShop').prop('checked', false);
}

/**
 * Adds an "Importer of Record: Same As Consignee" checkbox.
 *
 * @param {string}   iorBtnSelector
 * @param {function} onChecked - Receives (isChecked).
 * @returns {jQuery}
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

/**
 * Hides specific tabs in the commodity modal.
 *
 * @param {jQuery}   $commodityModal
 * @param {number[]} hideIndexes
 */
function hideCommodityModalTabs($commodityModal, hideIndexes) {
  $commodityModal.find('li.uib-tab.nav-item').each(function () {
    if (hideIndexes.indexOf(parseInt(this.getAttribute('index'))) > -1) {
      $(this).hide();
    }
  });
}

/**
 * Loads a cost-center value from CustomData and sets it as
 * ShipperReference, disabling the field.
 *
 * @param {object} vm
 * @param {string} customKey
 */
function loadCostCenter(vm, customKey) {
  var customData = vm?.profile?.UserInformation?.Address?.CustomData;
  if (!customData) return;

  var entry = customData.find(function (item) { return item.Key === customKey; });
  if (!entry) return;

  vm.currentShipment.Packages[vm.packageIndex].ShipperReference = entry.Value;
  $('[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]').prop('disabled', true);
}
