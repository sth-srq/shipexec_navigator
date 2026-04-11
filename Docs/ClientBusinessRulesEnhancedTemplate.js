/**
 * @fileoverview Enhanced Client Business Rules Template
 *
 * A comprehensive, single-file starting point for ShipExec Navigator
 * Client Business Rules (CBR) scripts.  This template combines the
 * standard lifecycle-hook structure from {@link ClientBusinessRulesTemplate.js}
 * with all reusable utility modules extracted from the Combined analysis:
 *
 *   - {@link CbrLogger}            – Structured multi-level logging
 *   - {@link ThinClientApi}        – Unified server request layer
 *   - {@link UserContextFilter}    – User context retrieval & history filtering
 *   - {@link EventHandling}        – Default hooks, page routing, DOM helpers
 *   - {@link FieldValidation}      – DOM polling, custom-data access, validation
 *   - {@link TimestampUtils}       – Date/time stamp generation
 *   - {@link AddressBookLogic}     – Consignee matching, reference updates
 *   - {@link AddressBookLookup}    – Code-search binding, return-address population
 *   - {@link ShippingDefaults}     – Address population, notifications, cost centres
 *   - {@link ServiceFiltering}     – Service-symbol mapping, D2M checks
 *   - {@link CommodityHandling}    – UOM translation, weight-based service selection
 *   - {@link ShipmentValidation}   – Reference composition, dimension/weight validation
 *   - {@link LoadStateManager}     – Loader/spinner, PreLoad/PostLoad state
 *   - {@link BatchHistoryOps}      – Batch/void/history hook helpers
 *   - {@link PackageHistoryAnon}   – Cost anonymisation, tracking links, masking
 *   - {@link PrintingOutput}       – ZPL injection, delegate labels, file I/O
 *   - {@link CustomBatchProcessing} – Consolidated-order dialog, batch helpers
 *   - {@link ThirdPartyOptions}    – Profile CRUD, manifest close-out
 *
 * ## Usage
 *
 * 1. Copy this file as your starting CBR script.
 * 2. Fill in the lifecycle hooks you need (remove or keep stubs for the rest).
 * 3. Call the utility-module methods from inside the hooks.
 *
 * @example <caption>Minimal CBR using the enhanced template</caption>
 *   // Inside the ClientBusinessRules constructor, override only what you need:
 *   this.PageLoaded = function (location) {
 *     EventHandling.onPageLoaded(location, vm, {
 *       '/shipping': function () { EventHandling.sortShippers(vm); },
 *     });
 *   };
 *   this.PreShip = function (shipmentRequest, userParams) {
 *     TimestampUtils.stampAllPackages(shipmentRequest, 'MiscReference15');
 *   };
 */

/* global $, angular, structuredClone, atob, atou, utoa */

// ===========================================================================
//  UTILITY MODULES
// ===========================================================================

// ---------------------------------------------------------------------------
//  CbrLogger – Structured multi-level logging
// ---------------------------------------------------------------------------

/**
 * @module CbrLogger
 * @see file://Docs/CBRAnalysis/Combined/logging.js
 */
const CbrLogger = (function () {
  'use strict';

  const LogLevel = Object.freeze({
    Info:  'Info',
    Debug: 'Debug',
    Trace: 'Trace',
    Error: 'Error',
    Fatal: 'Fatal',
  });

  var _serverLogging = false;

  function _sendToServer(logObject) {
    var dataObject = {
      ServerMethod:  'AddClientEntry',
      MessageObject: logObject,
    };
    var ajaxObject = {
      UserContext: (typeof Tools !== 'undefined') ? Tools.GetCurrentUserContext() : null,
      Data:        JSON.stringify(dataObject),
    };
    var request = $.ajax({
      url:         'api/ShippingService/UserMethod',
      method:      'POST',
      contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
      dataType:    'json',
      data:        ajaxObject,
      async:       true,
    });
    request.fail(function (jqXHR, textStatus) {
      console.log('Unable to log message to server.');
      console.log(jqXHR);
      console.log(textStatus);
    });
  }

  return {
    LogLevel: LogLevel,

    setServerDebugMode: function (enabled) {
      _serverLogging = !!enabled;
      console.log('Server-side logging: ' + _serverLogging);
    },

    getServerDebugMode: function () {
      return _serverLogging;
    },

    log: function (logObject) {
      try {
        if (logObject.Error) {
          logObject.LogLevel = LogLevel.Error;
          logObject.Error = {
            name:    logObject.Error.name    || 'Error',
            message: logObject.Error.message || String(logObject.Error),
          };
        } else if (!logObject.LogLevel) {
          logObject.LogLevel = LogLevel.Info;
        }

        switch (logObject.LogLevel) {
          case LogLevel.Error:
            console.error('Exception in', logObject.Source);
            console.error(logObject.Error.name);
            console.error(logObject.Error.message);
            if (logObject.Data) console.log(logObject.Data);
            break;
          case LogLevel.Info:
          case LogLevel.Debug:
          case LogLevel.Trace:
          case LogLevel.Fatal:
          default:
            console.log('Output from', logObject.Source);
            console.log(logObject.Message);
            if (logObject.Data) console.log(logObject.Data);
            break;
        }

        if (_serverLogging) {
          _sendToServer(logObject);
        }
      } catch (err) {
        console.log('CbrLogger internal error:', err.message);
      }
    },

    logMethodStart: function (methodName, callerName) {
      var msg = '...STARTING ' + methodName;
      if (callerName) msg += ' called by ' + callerName;
      console.log(msg);
    },

    logMethodInfo: function (message) {
      console.log('      ' + message);
    },
  };
})();

// ---------------------------------------------------------------------------
//  ThinClientApi – Unified server request layer
// ---------------------------------------------------------------------------

/**
 * @module ThinClientApi
 * @see file://Docs/CBRAnalysis/Combined/thin client api request.js
 */
const ThinClientApi = (function () {
  'use strict';

  var _userContext     = {};
  var _serverDebugMode = false;

  function _getConnectionInfo(clientService) {
    if (clientService && clientService.config) {
      return {
        url:     clientService.config.ShipExecServiceUrl,
        headers: clientService.getAuthorizationToken
          ? clientService.getAuthorizationToken()
          : {},
      };
    }
    var info = {};
    $.ajax({
      url: 'config.json', dataType: 'json', async: false,
      success: function (config) {
        var svcUrl = config.ShipExecServiceUrl;
        var token  = svcUrl.startsWith('http')
          ? {
              Authorization:
                'Bearer ' +
                JSON.parse(window.localStorage.getItem('TCToken')).access_token,
            }
          : {};
        info = { url: svcUrl, headers: token };
      },
    });
    return info;
  }

  return {
    post: function (method, data, isAsync, clientService) {
      var conn = _getConnectionInfo(clientService);
      return $.post({
        url:     conn.url + '/' + method,
        data:    data,
        async:   isAsync !== false,
        headers: conn.headers,
      });
    },

    callUserMethod: function (requestMethod, requestData, isAsync, callback) {
      $('div.loading').removeClass('ng-hide').addClass('ng-show');

      var retJson;
      var dataObject = {
        ServerMethod: requestMethod,
        MethodData:   requestData,
      };
      var ajaxObject = {
        UserContext: _userContext,
        Data:        JSON.stringify(dataObject),
      };
      var request = $.ajax({
        url:         'api/ShippingService/UserMethod',
        method:      'POST',
        contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
        dataType:    'json',
        async:       isAsync !== false,
        data:        ajaxObject,
      });
      request.done(function (json) {
        retJson = ThinClientApi.parseReturnData(json.Data);
      });
      request.fail(function (jqXHR, textStatus) {
        CbrLogger.log({
          Source: 'ThinClientApi.callUserMethod()',
          Error:  { name: 'AjaxError', message: jqXHR.responseText || textStatus },
        });
      });
      request.always(function () {
        $('div.loading').removeClass('ng-show').addClass('ng-hide');
        if (typeof callback === 'function') callback(retJson);
      });
      return retJson;
    },

    getSync: function (url) {
      var result;
      $.ajax({
        type: 'GET', url: url, async: false,
        success: function (response) { result = response; },
      });
      return result;
    },

    setUserContext: function (viewModel, includeUser) {
      try {
        if (viewModel.UserInformation) {
          _userContext = {
            CompanyId: viewModel.UserInformation.CompanyId,
            UserId:    viewModel.UserInformation.UserId,
          };
          return;
        }
        if (!_userContext.UserId && includeUser) {
          $.ajax({
            url: 'api/usercontext/GET', method: 'GET', async: false,
          }).done(function (data) { _userContext = data; });
        } else if (!_userContext.CompanyId && viewModel.profile) {
          _userContext.CompanyId = viewModel.profile.CompanyId;
        }
      } catch (error) {
        CbrLogger.log({ Source: 'ThinClientApi.setUserContext()', Error: error });
      }
    },

    getUserContext: function () {
      return _userContext;
    },

    parseReturnData: function (data) {
      try {
        return JSON.parse($('<div />').html(data).text());
      } catch (error) {
        console.log('parseReturnData failed:', error.message);
        console.log('Raw data:', data);
        return undefined;
      }
    },

    setServerDebugMode: function (enabled) { _serverDebugMode = !!enabled; },
    getServerDebugMode: function ()        { return _serverDebugMode; },
  };
})();

// ---------------------------------------------------------------------------
//  UserContextFilter – User context retrieval & history filtering
// ---------------------------------------------------------------------------

/**
 * @module UserContextFilter
 * @see file://Docs/CBRAnalysis/Combined/user context filtering.js
 */
const UserContextFilter = (function () {
  'use strict';

  var _cachedContext = null;

  return {
    getUserContext: function (viewModel) {
      if (_cachedContext) return _cachedContext;
      if (viewModel) {
        if (viewModel.userContext)     { _cachedContext = viewModel.userContext;     return _cachedContext; }
        if (viewModel.UserInformation) { _cachedContext = viewModel.UserInformation; return _cachedContext; }
      }
      $.get({ url: 'api/usercontext/GET', async: false }).done(function (data) { _cachedContext = data; });
      return _cachedContext;
    },

    clearCache: function () { _cachedContext = null; },

    filterHistoryByUser: function (searchCriteria, viewModel, fieldName, operator) {
      var ctx = this.getUserContext(viewModel);
      searchCriteria.WhereClauses.push({
        FieldName:  fieldName || 'UserId',
        FieldValue: ctx.UserId,
        Operator:   operator !== undefined ? operator : 0,
      });
    },

    filterHistoryByUserUnlessAll: function (searchCriteria, viewModel, allLabel) {
      var label    = allLabel || 'All History';
      var selValue = $('select[id="reports"]:eq(0)').val();
      var selText  = $("select[id='reports'] option[value='" + selValue + "']").text();
      if (selText !== label) {
        this.filterHistoryByUser(searchCriteria, viewModel);
      }
    },

    findCustomData: function (customData, key) {
      if (!customData) return undefined;
      return customData.find(function (e) { return e.Key === key; });
    },

    getUserCustomDataValue: function (viewModel, key) {
      var customData = viewModel?.profile?.UserInformation?.Address?.CustomData;
      var entry = this.findCustomData(customData, key);
      return entry ? entry.Value : '';
    },

    detectRole: function (profileName) {
      var isTraveler = profileName === 'Traveler Profile';
      return {
        IsMailroom:               !isTraveler,
        IsTraveler:               isTraveler,
        LoadRadioButtonsIsHidden: true,
      };
    },

    Shipment: class {
      constructor(shipmentRequest) { this.ShipmentRequest = shipmentRequest; }

      get UserRef1()  { return this._get('Custom1',  'User'); }
      get UserRef2()  { return this._get('Custom2',  'User'); }
      get UserRef3()  { return this._get('Custom3',  'User'); }
      get UserRef4()  { return this._get('Custom4',  'User'); }
      get UserRef5()  { return this._get('Custom5',  'User'); }
      get UserRef6()  { return this._get('Custom6',  'User'); }
      get UserRef7()  { return this._get('Custom7',  'User'); }
      get UserRef8()  { return this._get('Custom8',  'User'); }
      get UserRef9()  { return this._get('Custom9',  'User'); }
      get UserRef10() { return this._get('Custom10', 'User'); }

      get ToRef1()  { return this._get('Custom1',  'To'); }
      get ToRef2()  { return this._get('Custom2',  'To'); }
      get ToRef3()  { return this._get('Custom3',  'To'); }
      get ToRef4()  { return this._get('Custom4',  'To'); }
      get ToRef5()  { return this._get('Custom5',  'To'); }
      get ToRef6()  { return this._get('Custom6',  'To'); }
      get ToRef7()  { return this._get('Custom7',  'To'); }
      get ToRef8()  { return this._get('Custom8',  'To'); }
      get ToRef9()  { return this._get('Custom9',  'To'); }
      get ToRef10() { return this._get('Custom10', 'To'); }

      _get(fieldName, type) {
        var data = null;
        if (type === 'User') data = this.ShipmentRequest?.PackageDefaults?.OriginAddress?.CustomData;
        if (type === 'To')   data = this.ShipmentRequest?.PackageDefaults?.Consignee?.CustomData;
        var value = '';
        if (data) { data.forEach(function (f) { if (f.Key === fieldName) value = f.Value; }); }
        return value;
      }
    },
  };
})();

// ---------------------------------------------------------------------------
//  EventHandling – Default hooks, page routing, DOM helpers
// ---------------------------------------------------------------------------

/**
 * @module EventHandling
 * @see file://Docs/CBRAnalysis/Combined/event handling.js
 */
const EventHandling = (function () {
  'use strict';

  const DEFAULT_HOOKS = {
    PageLoaded:            function () {},
    Keystroke:             function () {},
    NewShipment:           function () {},
    AddPackage:            function () {},
    CopyPackage:           function () {},
    RemovePackage:         function () {},
    PreviousPackage:       function () {},
    NextPackage:           function () {},
    PreShip:               function () {},
    PostShip:              function () {},
    PreLoad:               function () {},
    PostLoad:              function () {},
    PreRate:               function () {},
    PostRate:              function () {},
    PreProcessBatch:       function () {},
    PostProcessBatch:      function () {},
    PreVoid:               function () {},
    PostVoid:              function () {},
    PrePrint:              function () {},
    PostPrint:             function () {},
    PreCloseManifest:      function () {},
    PostCloseManifest:     function () {},
    PreTransmit:           function () {},
    PostTransmit:          function () {},
    PreSearchHistory:      function () {},
    PostSearchHistory:     function () {},
    PostSelectAddressBook: function () {},
    PreBuildShipment:      function () {},
    PostBuildShipment:     function () {},
    RepeatShipment:        function () {},
    PreShipOrder:          function () {},
    PostShipOrder:         function () {},
    PreCreateGroup:        function () {},
    PostCreateGroup:       function () {},
    PreModifyGroup:        function () {},
    PostModifyGroup:       function () {},
    PreCloseGroup:         function () {},
    PostCloseGroup:        function () {},
  };

  return {
    applyDefaults: function (target) {
      Object.keys(DEFAULT_HOOKS).forEach(function (hookName) {
        if (typeof target[hookName] !== 'function') {
          target[hookName] = DEFAULT_HOOKS[hookName];
        }
      });
    },

    onPageLoaded: function (location, viewModel, handlers) {
      if (handlers && typeof handlers[location] === 'function') {
        handlers[location](viewModel);
      }
    },

    isEventAttached: function ($element, eventName, eventHandlerFn) {
      if (!$element || !$element.length) return false;
      var events = $._data($element.get(0), 'events');
      if (!events) return false;
      var handlerStr = eventHandlerFn.toString();
      return (
        events[eventName] &&
        events[eventName].some(function (ev) {
          return ev.handler.toString() === handlerStr;
        })
      ) || false;
    },

    sortShippers: function (viewModel, sortKey) {
      var key = sortKey || 'Name';
      if (viewModel.profile && viewModel.profile.Shippers) {
        viewModel.profile.Shippers.sort(function (a, b) {
          if (a[key] > b[key]) return 1;
          if (a[key] < b[key]) return -1;
          return 0;
        });
      }
    },

    waitForElement: function (selector, callback, intervalMs, timeoutMs) {
      var interval = intervalMs || 50;
      var timeout  = timeoutMs  || 10000;
      var elapsed  = 0;
      var poller = setInterval(function () {
        elapsed += interval;
        var $el = $(selector);
        if ($el.length) {
          clearInterval(poller);
          if (typeof callback === 'function') callback($el);
        } else if (elapsed >= timeout) {
          clearInterval(poller);
          console.warn('EventHandling.waitForElement: timed out waiting for ' + selector);
        }
      }, interval);
    },

    focusLoadInput: function () {
      this.waitForElement(
        'input[type=text][ng-model="vm.loadValue"]',
        function ($el) { $el.focus(); }
      );
    },
  };
})();

// ---------------------------------------------------------------------------
//  FieldValidation – DOM polling, custom-data access, validation
// ---------------------------------------------------------------------------

/**
 * @module FieldValidation
 * @see file://Docs/CBRAnalysis/Combined/field defaulting and validation.js
 */
const FieldValidation = (function () {
  'use strict';

  function _pollDomForElement(selector, timeoutSeconds) {
    var timeout = (timeoutSeconds || 10) * 1000;
    return new Promise(function (resolve, reject) {
      var elapsed = 0, interval = 100;
      var poller = setInterval(function () {
        elapsed += interval;
        var $el = $(selector);
        if ($el.length)            { clearInterval(poller); resolve($el); }
        else if (elapsed >= timeout) { clearInterval(poller); reject(new Error('Timed out waiting for element: ' + selector)); }
      }, interval);
    });
  }

  function _pollInputsForValues(inputSelector, minEntries, timeoutSeconds) {
    var timeout = (timeoutSeconds || 10) * 1000;
    return new Promise(function (resolve, reject) {
      var elapsed = 0, interval = 100;
      var poller = setInterval(function () {
        elapsed += interval;
        var filled = 0;
        $(inputSelector).each(function () { if ($(this).val()) filled++; });
        if (filled >= minEntries)   { clearInterval(poller); resolve(); }
        else if (elapsed >= timeout) { clearInterval(poller); reject(new Error('Timed out waiting for input values: ' + inputSelector)); }
      }, interval);
    });
  }

  return {
    waitForElement: async function (selector, focusAfter, defaultValue, timeoutSeconds, callback) {
      try {
        await _pollDomForElement(selector, timeoutSeconds);
        if (defaultValue) $(selector).val(defaultValue);
        if (typeof callback === 'function') callback();
        if (focusAfter) $(selector).focus();
      } catch (error) {
        CbrLogger.log({ Source: 'FieldValidation.waitForElement()', Error: error });
      }
    },

    waitForSelectOptions: async function (
      selectSelector, minOptions, selectByIndex, selectByValue,
      clearFirst, timeoutSeconds, callback
    ) {
      if (selectByIndex !== undefined && selectByValue !== undefined) {
        throw new Error('Cannot use both selectByIndex and selectByValue.');
      }
      if (clearFirst) $(selectSelector).val([]);
      var optionSelector = selectSelector + ' option:gt(' + (minOptions || 1) + ')';
      try {
        await _pollDomForElement(optionSelector, timeoutSeconds);
        if (typeof callback === 'function') callback();
        if (selectByIndex !== undefined) {
          $(selectSelector + ' option').eq(selectByIndex).prop('selected', 'selected');
        }
        if (selectByValue !== undefined) {
          $(selectSelector).val(selectByValue);
        }
      } catch (error) {
        CbrLogger.log({ Source: 'FieldValidation.waitForSelectOptions()', Error: error });
      }
    },

    waitForInputValues: async function (inputSelector, minEntries, timeoutSeconds, callback) {
      try {
        await _pollInputsForValues(inputSelector, minEntries, timeoutSeconds);
        if (typeof callback === 'function') callback();
      } catch (error) {
        CbrLogger.log({ Source: 'FieldValidation.waitForInputValues()', Error: error });
      }
    },

    getCustomDataValue: function (fieldName, customDataType, shipmentRequest) {
      var returnValue = '';
      var customData  = null;
      if (customDataType === 'User') {
        customData = shipmentRequest?.PackageDefaults?.OriginAddress?.CustomData;
      } else if (customDataType === 'To') {
        customData = shipmentRequest?.PackageDefaults?.Consignee?.CustomData;
      }
      if (customData) {
        customData.forEach(function (entry) {
          if (entry.Key === fieldName) returnValue = entry.Value;
        });
      }
      return returnValue;
    },

    getKeyValue: function (key, arr) {
      if (!arr) return '';
      for (var i = 0; i < arr.length; i++) {
        if (arr[i].Key.toLowerCase() === key.toLowerCase()) {
          return arr[i].Value;
        }
      }
      return '';
    },

    validateAgainstList: function (referenceValue, validationList) {
      if (!validationList) return true;
      for (var i = 0; i < validationList.length; i++) {
        if (validationList[i].Value === referenceValue) return true;
      }
      throw { message: 'Unable to validate shipment', errorCode: '001' };
    },

    ensureString: function (value) {
      return value || '';
    },
  };
})();

// ---------------------------------------------------------------------------
//  TimestampUtils – Date/time stamp generation
// ---------------------------------------------------------------------------

/**
 * @module TimestampUtils
 * @see file://Docs/CBRAnalysis/Combined/shipment timestamp stamping.js
 */
const TimestampUtils = (function () {
  'use strict';

  return {
    now: function () { return new Date(); },

    todayString: function () {
      return new Date().toLocaleDateString('en-US');
    },

    dateTimeStamp: function () {
      return new Date().toLocaleString('en-US', {
        timeZoneName: 'short',
        hour12: false,
      });
    },

    toShipdate: function () {
      var today = new Date();
      return { Year: today.getFullYear(), Month: today.getMonth() + 1, Day: today.getDate() };
    },

    stampField: function (packageObj, fieldName, value) {
      if (!packageObj) return;
      packageObj[fieldName] = value || this.dateTimeStamp();
    },

    stampAllPackages: function (shipmentRequest, fieldName, value) {
      var stamp = value || this.dateTimeStamp();
      (shipmentRequest.Packages || []).forEach(function (pkg) {
        pkg[fieldName] = stamp;
      });
    },

    shipdateAgeDays: function (shipdate) {
      if (!shipdate) return Infinity;
      var sd     = new Date(shipdate.Year, shipdate.Month - 1, shipdate.Day);
      var diffMs = Math.abs(sd - new Date());
      return Math.floor(diffMs / (1000 * 60 * 60 * 24));
    },
  };
})();

// ---------------------------------------------------------------------------
//  AddressBookLogic – Consignee matching, reference updates
// ---------------------------------------------------------------------------

/**
 * @module AddressBookLogic
 * @see file://Docs/CBRAnalysis/Combined/address book logic.js
 */
const AddressBookLogic = (function () {
  'use strict';

  function _normalise(value) {
    return (value || '').trim().toUpperCase().replaceAll(' ', '');
  }

  function _buildAddressFingerprint(address1, city, state, postalCode) {
    var snapshot = {
      conAddress1:   _normalise(address1),
      conCity:       _normalise(city),
      conState:      _normalise(state),
      conPostalCode: (postalCode || '').trim().substring(0, 5),
    };
    return JSON.stringify(snapshot).replaceAll(' ', '');
  }

  const CONSIGNEE_SELECTOR =
    'name-address[nameaddress="vm.currentShipment.PackageDefaults.Consignee"]';

  return {
    postSelectAddressBook: function () {},

    bindAddressCodeTabSearch: function (sectionId, sectionCaption) {
      $(document).ready(function () {
        var $section = $('[id="' + sectionId + '"]');
        $section.on('keydown', $section.find("input[name='code']"), function (e) {
          var keyCode = e.keyCode || e.which;
          if (keyCode === 9) {
            e.preventDefault();
            $('[caption="' + sectionCaption + '"]')
              .find('button[ng-click="search(nameaddress)"]')
              .click();
          }
        });
      });
    },

    isConsigneeMatch: function (customerOrder) {
      try {
        var $con = $(CONSIGNEE_SELECTOR);
        var uiFingerprint = _buildAddressFingerprint(
          $con.find('input[name="address1"]').val(),
          $con.find('input[name="city"]').val(),
          $con.find('input[name="stateProvince"]').val(),
          $con.find('input[name="postalCode"]').val()
        );
        var orderFingerprint = _buildAddressFingerprint(
          customerOrder.shipAddress1,
          customerOrder.shipCity,
          customerOrder.shipState,
          customerOrder.shipZipCode
        );
        if (uiFingerprint !== orderFingerprint) {
          CbrLogger.log({
            Source:  'AddressBookLogic.isConsigneeMatch()',
            Message: 'Address mismatch for order ' +
                     customerOrder.orderNumber + ' (' + customerOrder.orderId + ').',
            Data: { ui: uiFingerprint, order: orderFingerprint },
          });
          return false;
        }
        return true;
      } catch (error) {
        CbrLogger.log({ Source: 'AddressBookLogic.isConsigneeMatch()', Error: error });
        return false;
      }
    },

    appendOrderNumberToReferences: function (orderNumber) {
      var $shipperRef = $(
        'input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]'
      );
      var $miscRef7 = $(
        'input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].MiscReference7"]'
      );
      var combined = $shipperRef.val() + ',' + orderNumber;
      var unique   = Tools.GetUniqueCSVStringFromString(combined);
      $shipperRef.val(unique);
      $miscRef7.val(unique);
    },
  };
})();

// ---------------------------------------------------------------------------
//  AddressBookLookup – Code-search binding, return-address population
// ---------------------------------------------------------------------------

/**
 * @module AddressBookLookup
 * @see file://Docs/CBRAnalysis/Combined/address book lookup and population.js
 */
const AddressBookLookup = (function () {
  'use strict';

  const CON_SECTION    = '[nameaddress="vm.currentShipment.PackageDefaults.Consignee"] ';
  const CON_SEARCH_BTN = CON_SECTION + 'button[ng-click="search(nameaddress)"]';

  const RETURN_ADDRESS_FIELDS = [
    { selector: 'company',       property: 'Company' },
    { selector: 'address1',      property: 'Address1' },
    { selector: 'city',          property: 'City' },
    { selector: 'stateProvince', property: 'StateProvince' },
    { selector: 'postalCode',    property: 'PostalCode' },
    { selector: 'phone',         property: 'Phone' },
    { selector: 'contact',       property: 'Contact' },
  ];

  function _setFieldAndTrigger(nameAttr, eqIndex, value) {
    $('[name="' + nameAttr + '"]:eq(' + eqIndex + ')').val(value).trigger('change');
  }

  return {
    bindConsigneeCodeSearch: function (codeInputSelector) {
      var selector = codeInputSelector || (CON_SECTION + "input[name='code']");
      $('body').on('keyup focusout', selector, function (e) {
        var keycode = e.keyCode || e.which;
        if (keycode === 13 || e.type === 'focusout') {
          $(CON_SEARCH_BTN).trigger('click');
        }
      });
    },

    copyOriginToReturnAddress: function (viewModel) {
      var originAddress = viewModel?.profile?.UserInformation?.Address;
      if (originAddress !== undefined && !$.isEmptyObject(originAddress)) {
        viewModel.currentShipment.PackageDefaults.ReturnAddress = originAddress;
        console.log('User origin address copied to return address: OK');
        return true;
      }
      console.log('Error: Origin address not found on user profile.');
      return false;
    },

    loadReturnAddressFromShipper: function (viewModel, callerName, returnAddressEq, pollIntervalMs) {
      var eqIdx    = returnAddressEq || 1;
      var interval = pollIntervalMs  || 150;
      console.log('...STARTING loadReturnAddressFromShipper() called by ' + (callerName || ''));
      var poller = setInterval(function () {
        var shipper = viewModel.profile?.Shippers?.[0];
        if (!shipper?.Company) return;
        clearInterval(poller);
        RETURN_ADDRESS_FIELDS.forEach(function (field) {
          _setFieldAndTrigger(field.selector, eqIdx, shipper[field.property] || '');
        });
        var $country = $('[name="Country"]:eq(' + eqIdx + ')');
        $country.val('string:' + (shipper.Country || '')).trigger('change');
      }, interval);
    },

    waitAndCopyOriginToReturn: function (viewModel, originCompanyEq, pollIntervalMs) {
      var eqIdx    = originCompanyEq || 2;
      var interval = pollIntervalMs  || 50;
      var self     = this;
      var poller = setInterval(function () {
        var company = $('[name="company"]:eq(' + eqIdx + ')').val();
        if (company && company.length) {
          clearInterval(poller);
          self.copyOriginToReturnAddress(viewModel);
        }
      }, interval);
    },

    postSelectAddressBook: function () {},
  };
})();

// ---------------------------------------------------------------------------
//  ShippingDefaults – Address population, notifications, cost centres
// ---------------------------------------------------------------------------

/**
 * @module ShippingDefaults
 * @see file://Docs/CBRAnalysis/Combined/shipping charges defaulting and return address population.js
 */
const ShippingDefaults = (function () {
  'use strict';

  return {
    populateAddress: function (packageDefaults, section, source) {
      if (!source) return;
      packageDefaults[section] = {
        Code:          source.Code          || '',
        Company:       source.Company       || '',
        Contact:       source.Contact       || '',
        Address1:      source.Address1      || '',
        Address2:      source.Address2      || '',
        Address3:      source.Address3      || '',
        City:          source.City          || '',
        StateProvince: source.StateProvince || '',
        PostalCode:    source.PostalCode    || '',
        Country:       source.Country       || '',
        Email:         source.Email         || '',
        Phone:         source.Phone         || '',
      };
    },

    copyOriginToReturn: function (viewModel) {
      var origin = viewModel?.profile?.UserInformation?.Address;
      if (origin && !$.isEmptyObject(origin)) {
        viewModel.currentShipment.PackageDefaults.ReturnAddress = origin;
        console.log('Origin → Return address: OK');
        return true;
      }
      console.log('Error: Origin address not found.');
      return false;
    },

    setStaticConsignee: function (shipmentRequest, address) {
      shipmentRequest.PackageDefaults.Consignee = Object.assign({}, address);
    },

    setNotificationEmails: function (shipmentRequest, packageIndex, email, options) {
      var pkg  = shipmentRequest.Packages[packageIndex];
      var opts = options || {};
      if (opts.ship !== false) {
        pkg.ShipNotificationEmail        = true;
        pkg.ShipNotificationAddressEmail = email;
      }
      if (opts.delivery !== false) {
        pkg.DeliveryNotificationEmail        = true;
        pkg.DeliveryNotificationAddressEmail = email;
      }
      if (opts.deliveryException !== false) {
        pkg.DeliveryExceptionNotificationEmail        = true;
        pkg.DeliveryExceptionNotificationAddressEmail = email;
      }
    },

    syncNotificationEmailFromDom: function (shipmentRequest, packageIndex) {
      var idx = packageIndex || 0;
      var val = $(
        'input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipNotificationAddressEmail"]'
      ).val();
      shipmentRequest.Packages[idx].ShipNotificationAddressEmail = val;
    },

    loadCostCenter: function (viewModel, customKey) {
      var key        = customKey || 'CostCenter';
      var customData = viewModel?.profile?.UserInformation?.Address?.CustomData;
      if (!customData) return;
      var entry = customData.find(function (e) { return e.Key === key; });
      if (entry && !$.isEmptyObject(entry)) {
        console.log(key + ' detected: ' + JSON.stringify(entry));
        viewModel.currentShipment.Packages[viewModel.packageIndex].ShipperReference = entry.Value;
        $('[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]')
          .prop('disabled', true);
      } else {
        console.log('Error: ' + key + ' not found in user custom data.');
      }
    },

    setReturnDeliveryEmail: function (shipmentRequest, email) {
      (shipmentRequest.Packages || []).forEach(function (pkg) {
        pkg.ReturnDeliveryAddressEmail = email;
      });
    },
  };
})();

// ---------------------------------------------------------------------------
//  ServiceFiltering – Service-symbol mapping, D2M checks
// ---------------------------------------------------------------------------

/**
 * @module ServiceFiltering
 * @see file://Docs/CBRAnalysis/Combined/service filtering.js
 */
const ServiceFiltering = (function () {
  'use strict';

  const SERVICE_MAP = {
    '2dn': 'UPSAPI.UPS.2DA',
    '3dd': 'UPSAPI.UPS.3DS',
    exp:   'UPSAPI.UPS.EXPEDITED',
    ess:   'UPSAPI.UPS.EXPRESS',
    nda:   'UPSAPI.UPS.NDA',
    dae:   'UPSAPI.UPS.NDAEAM',
    grn:   'UPSAPI.UPS.GND',
    uwe:   'UPSAPI.UPS.EXPEDITED',
    wep:   'UPSAPI.UPS.EXPRESSSP',
    swe:   'UPSAPI.UPS.EXPRESS',
    uws:   'UPSAPI.UPS.SAVER',
    std:   'UPSAPI.UPS.STANDARD',
  };

  const D2M_USER_KEY = 'D2M';
  const D2M_ENABLED  = 'ENABLED';
  const US_CODES     = ['US', 'USA'];

  function _findCustomData(customData, key) {
    if (!customData) return undefined;
    return customData.find(function (entry) { return entry.Key === key; });
  }

  return {
    getServiceSymbol: function (shortCode) {
      return SERVICE_MAP[(shortCode || '').toLowerCase()];
    },

    filterServicesByCode: function (profile, codesCsv, suffixes) {
      var codes  = (codesCsv || '').split(',');
      var result = [];
      var sfx    = suffixes || {};
      codes.forEach(function (code) {
        var symbol = SERVICE_MAP[code.trim().toLowerCase()];
        if (!symbol) return;
        for (var i = 0; i < profile.Services.length; i++) {
          if (profile.Services[i].Symbol === symbol) {
            var svc  = Object.assign({}, profile.Services[i]);
            svc.Name = svc.Name.replace('(UPS Adapter)', '').trim();
            Object.keys(sfx).forEach(function (substring) {
              if (svc.Name.includes(substring)) svc.Name += sfx[substring];
            });
            result.push(svc);
            break;
          }
        }
      });
      return result;
    },

    isUserD2MEnabled: function (viewModel) {
      var customData = viewModel?.profile?.UserInformation?.Address?.CustomData;
      var entry      = _findCustomData(customData, D2M_USER_KEY);
      if (!entry || $.isEmptyObject(entry)) return false;
      return (entry.Value || '').toUpperCase() === D2M_ENABLED;
    },

    isShipperD2MEnabled: function (authorisedShippers) {
      var currentSymbol = $('select[ng-change="vm.shipperChange()"]')
        .val().substring(7).toUpperCase();
      return (authorisedShippers || []).some(function (s) {
        return s.ShipperSymbol === currentSymbol;
      });
    },

    isCurrentShipmentD2M: function (viewModel, checkboxId, authorisedShippers) {
      return (
        this.isUserD2MEnabled(viewModel) &&
        $('#' + checkboxId).is(':checked') &&
        this.isShipperD2MEnabled(authorisedShippers)
      );
    },

    isSessionD2MCapable: function (viewModel, authorisedShippers) {
      return (
        this.isUserD2MEnabled(viewModel) &&
        this.isShipperD2MEnabled(authorisedShippers)
      );
    },

    setReturnDeliveryAndInsurance: function (enabled, shipmentRequest) {
      if (!shipmentRequest || !enabled) return;
      var country = (shipmentRequest.PackageDefaults.Consignee.Country || '').toUpperCase();
      if (US_CODES.indexOf(country) === -1) {
        $('select option[value="number:1"]').prop('selected', true);
      }
    },
  };
})();

// ---------------------------------------------------------------------------
//  CommodityHandling – UOM translation, weight-based service selection
// ---------------------------------------------------------------------------

/**
 * @module CommodityHandling
 * @see file://Docs/CBRAnalysis/Combined/commodity handling and international shipment preparation.js
 */
const CommodityHandling = (function () {
  'use strict';

  const UOM_MAP = {
    EACH:  'EA',
    YARDS: 'YD',
    YARD:  'YD',
    METER: 'M',
    SF:    'SFT',
    PAIR:  'PR',
    SR:    'ROL',
  };

  return {
    refreshCommodityList: function () {
      $('div.ui-tab-container > div.ng-isolate-scope > ul > li.active > a').click();
      $('#goods').find('div.ng-table-counts.btn-group').find('button:not(.active):first').click();
    },

    translateUnitOfMeasure: function (rawValue) {
      if (!rawValue) return rawValue;
      var key = rawValue.toUpperCase();
      return UOM_MAP[key] || rawValue;
    },

    selectServiceByWeight: function (shipmentRequest, weightThreshold, expressSymbol, freightSymbol) {
      var threshold  = weightThreshold || 70;
      var expressSvc = expressSymbol   || 'CONNECTSHIP_UPS.UPS.EXPSVR';
      var freightSvc = freightSymbol   || 'CONNECTSHIP_UPS.UPS.EXPFRT';
      var hasHeavy = false, hasMixed = false;

      for (var i = 0; i < shipmentRequest.Packages.length; i++) {
        var weight = parseInt(shipmentRequest.Packages[i].Weight.Amount, 10);
        if (weight >= threshold) { hasHeavy = true; }
        else if (hasHeavy) { hasMixed = true; }
      }
      if (hasMixed) {
        alert('Cannot create one shipment with 2 different service levels.');
        return false;
      }
      var selectedSymbol = hasHeavy ? freightSvc : expressSvc;
      shipmentRequest.PackageDefaults.Service = { Symbol: selectedSymbol };
      for (var j = 0; j < shipmentRequest.Packages.length; j++) {
        if (selectedSymbol === freightSvc) {
          shipmentRequest.Packages[j].Packaging = 'CUSTOMER_PALLET';
        }
        shipmentRequest.Packages[j].ImportDelivery = true;
      }
      return true;
    },

    validateExportRequirements: function (shipmentRequest) {
      var originCountry = shipmentRequest.PackageDefaults.OriginAddress.Country;
      if (originCountry === 'US') {
        var carrierVal = $('select[name="CarrierInstructions"]').val();
        if (carrierVal === '?') {
          alert('Please confirm shipment value');
          throw new Error('Shipment value not confirmed.');
        }
        var consRef = shipmentRequest.Packages[0].ConsigneeReference;
        if ((!consRef || consRef === '') && carrierVal === 'string:Yes') {
          alert('Please Enter ITN / EES number in ConsigneeReference');
          throw new Error('ITN / EES number required.');
        }
      } else if (originCountry && originCountry !== 'undefined') {
        alert(
          'For EU Exports: Shipments above 1000 EUR require a formal export ' +
          'declaration.  The Export Accompanying Document (EAD) with a unique ' +
          'Movement Reference Number (MRN) must accompany the shipment.  UPS ' +
          'is responsible for exit clearance at the EU border.'
        );
      }
      return true;
    },
  };
})();

// ---------------------------------------------------------------------------
//  ShipmentValidation – Reference composition, dimension/weight validation
// ---------------------------------------------------------------------------

/**
 * @module ShipmentValidation
 * @see file://Docs/CBRAnalysis/Combined/shipment conversion and validation.js
 */
const ShipmentValidation = (function () {
  'use strict';

  class Shipment {
    constructor(shipmentRequest) { this.ShipmentRequest = shipmentRequest; }

    get UserRef1()  { return this.GetCustom('Custom1',  'User'); }
    get UserRef2()  { return this.GetCustom('Custom2',  'User'); }
    get UserRef3()  { return this.GetCustom('Custom3',  'User'); }
    get UserRef4()  { return this.GetCustom('Custom4',  'User'); }
    get UserRef5()  { return this.GetCustom('Custom5',  'User'); }
    get UserRef6()  { return this.GetCustom('Custom6',  'User'); }
    get UserRef7()  { return this.GetCustom('Custom7',  'User'); }
    get UserRef8()  { return this.GetCustom('Custom8',  'User'); }
    get UserRef9()  { return this.GetCustom('Custom9',  'User'); }
    get UserRef10() { return this.GetCustom('Custom10', 'User'); }
    get UserEmail() { return this.GetCustom('Email',    'User'); }

    get ToRef1()  { return this.GetCustom('Custom1',  'To'); }
    get ToRef2()  { return this.GetCustom('Custom2',  'To'); }
    get ToRef3()  { return this.GetCustom('Custom3',  'To'); }
    get ToRef4()  { return this.GetCustom('Custom4',  'To'); }
    get ToRef5()  { return this.GetCustom('Custom5',  'To'); }
    get ToRef6()  { return this.GetCustom('Custom6',  'To'); }
    get ToRef7()  { return this.GetCustom('Custom7',  'To'); }
    get ToRef8()  { return this.GetCustom('Custom8',  'To'); }
    get ToRef9()  { return this.GetCustom('Custom9',  'To'); }
    get ToRef10() { return this.GetCustom('Custom10', 'To'); }

    GetCustom(fieldName, customDataType) {
      var returnValue = '';
      var customData  = null;
      if (customDataType === 'User') customData = this.ShipmentRequest?.PackageDefaults?.OriginAddress?.CustomData;
      else if (customDataType === 'To') customData = this.ShipmentRequest?.PackageDefaults?.Consignee?.CustomData;
      if (customData) { customData.forEach(function (field) { if (field.Key === fieldName) returnValue = field.Value; }); }
      return returnValue;
    }
  }

  return {
    Shipment: Shipment,

    buildShipperReference: function (primaryRef, secondaryRef) {
      return (primaryRef || '') + ' ' + (secondaryRef || '');
    },

    setAllPackages: function (shipmentRequest, fieldName, value) {
      for (var i = 0; i < shipmentRequest.Packages.length; i++) {
        shipmentRequest.Packages[i][fieldName] = value;
      }
    },

    copyFromPreviousPackage: function (shipmentRequest, packageIndex, fieldNames) {
      var fields = Array.isArray(fieldNames) ? fieldNames : [fieldNames];
      var prev   = shipmentRequest.Packages[packageIndex - 1];
      var curr   = shipmentRequest.Packages[packageIndex];
      fields.forEach(function (name) { curr[name] = prev[name]; });
    },

    validateAgainstList: function (value, validationList) {
      if (!validationList) return true;
      for (var i = 0; i < validationList.length; i++) {
        if (validationList[i].Value === value) return true;
      }
      throw { message: 'Unable to validate shipment', errorCode: '001' };
    },

    validateAndStoreDimensions: function (shipmentRequest) {
      if (!shipmentRequest?.Packages?.length) return false;
      var validDims = [];
      for (var i = 0; i < shipmentRequest.Packages.length; i++) {
        var dims = shipmentRequest.Packages[i]?.Dimensions;
        if (!dims || dims.Length <= 0 || dims.Width <= 0 || dims.Height <= 0) {
          console.error('Package ' + i + ' has invalid dimensions.');
          return false;
        }
        validDims.push(dims.Length + 'x' + dims.Width + 'x' + dims.Height + ' 0');
      }
      if (validDims.length > 0) {
        shipmentRequest.PackageDefaults.MiscReference5 = validDims.join(', ');
        return true;
      }
      return false;
    },

    validateWeight: function (shipmentRequest, weightLimit) {
      var limit = weightLimit || 50;
      for (var i = 0; i < shipmentRequest.Packages.length; i++) {
        var weight = parseFloat(shipmentRequest.Packages[i].Weight.Amount);
        if (weight >= limit) {
          return confirm(
            'One or more packages is ' + limit +
            'lb or greater.  Are you sure you want to continue?'
          );
        }
      }
      return true;
    },
  };
})();

// ---------------------------------------------------------------------------
//  LoadStateManager – Loader/spinner, PreLoad/PostLoad state
// ---------------------------------------------------------------------------

/**
 * @module LoadStateManager
 * @see file://Docs/CBRAnalysis/Combined/load state management.js
 */
const LoadStateManager = (function () {
  'use strict';

  var _savedShipper   = null;
  var _savedBatchName = null;

  return {
    showLoader: function (callback) {
      $('div.loading').removeClass('ng-hide').addClass('ng-show');
      if (typeof callback === 'function') callback();
    },

    hideLoader: function (callback) {
      $('div.loading').removeClass('ng-show').addClass('ng-hide');
      this.hideOverlay();
      if (typeof callback === 'function') callback();
    },

    showOverlay: function () {
      var $overlay = $('#cbrOverlay');
      if ($overlay.length) $overlay.fadeIn('fast');
    },

    hideOverlay: function () {
      var $overlay = $('#cbrOverlay');
      if ($overlay.length) $overlay.fadeOut('fast');
    },

    preLoad:  function () {},
    postLoad: function () {},

    capturePreLoadState: function (shipmentRequest) {
      _savedShipper   = shipmentRequest.PackageDefaults.Shipper;
      _savedBatchName = shipmentRequest.Packages[0].MiscReference5;
    },

    restorePostLoadState: function (shipmentRequest) {
      if (_savedShipper   !== null) shipmentRequest.PackageDefaults.Shipper        = _savedShipper;
      if (_savedBatchName !== null) shipmentRequest.Packages[0].MiscReference5     = _savedBatchName;
    },

    showLoadErrorIfPresent: function (shipmentRequest) {
      var defaults = shipmentRequest.PackageDefaults;
      if (defaults.ErrorCode === 1) {
        alert(defaults.ErrorMessage);
        defaults.ErrorCode = 0;
      }
    },

    showUserDataAlert: function (shipmentRequest, viewModel) {
      var ud1 = shipmentRequest.Packages[0].UserData1;
      if (!ud1 || ud1.length === 0) return;
      if (ud1.substring(0, 1) === '~') {
        shipmentRequest.AlertTitle   = 'ShipExec Alert';
        shipmentRequest.AlertMessage = ud1.substring(1);
        $('[ng-model="AlertModalNewShipment"]').modal('show');
      } else {
        shipmentRequest.AlertTitle   = 'ShipExec Alert';
        shipmentRequest.AlertMessage = ud1;
        $('[ng-model="AlertModal"]').modal('show');
      }
    },
  };
})();

// ---------------------------------------------------------------------------
//  BatchHistoryOps – Batch/void/history hook helpers
// ---------------------------------------------------------------------------

/**
 * @module BatchHistoryOps
 * @see file://Docs/CBRAnalysis/Combined/batch voiding and history operations.js
 */
const BatchHistoryOps = (function () {
  'use strict';

  return {
    preProcessBatch:  function () {},
    postProcessBatch: function () {},
    preVoid:          function () {},
    postVoid:         function () {},
    preSearchHistory: function () {},
    postSearchHistory: function () {},

    filterHistoryByUser: function (searchCriteria, userCtx, fieldName, operator) {
      searchCriteria.WhereClauses.push({
        FieldName:  fieldName || 'UserId',
        FieldValue: userCtx.UserId,
        Operator:   operator !== undefined ? operator : 0,
      });
    },

    filterHistoryByField: function (searchCriteria, fieldName, fieldValue, operator) {
      searchCriteria.WhereClauses.push({
        FieldName:  fieldName,
        FieldValue: fieldValue,
        Operator:   operator !== undefined ? operator : 5,
      });
    },

    setHistoryStartDate: function (searchCriteria, viewModel, daysBack) {
      var days = daysBack || 90;
      var startDate = new Date();
      startDate.setDate(startDate.getDate() - days);
      viewModel.dtstart = startDate;
      searchCriteria.WhereClauses.filter(function (w) {
        if (w.FieldName === 'Shipdate' && w.Operator === 3) {
          w.FieldValue = startDate.toISOString().slice(0, 10);
        }
      });
    },

    compareByKey: function (key) {
      return function (a, b) {
        if (a[key] > b[key]) return 1;
        if (a[key] < b[key]) return -1;
        return 0;
      };
    },

    anonymiseCosts: function (packages) {
      (packages || []).forEach(function (pkg) {
        pkg.ApportionedTotal = { Amount: null, Currency: null };
      });
    },
  };
})();

// ---------------------------------------------------------------------------
//  PackageHistoryAnon – Cost anonymisation, tracking links, masking
// ---------------------------------------------------------------------------

/**
 * @module PackageHistoryAnon
 * @see file://Docs/CBRAnalysis/Combined/package history anonymization.js
 */
const PackageHistoryAnon = (function () {
  'use strict';

  return {
    anonymiseCosts: function (packages) {
      (packages || []).forEach(function (pkg) {
        pkg.ApportionedTotal = { Amount: null, Currency: null };
      });
    },

    addTrackingLinks: function (tableSelector, pollIntervalMs) {
      var selector = tableSelector || 'table[ng-table="vm.tableParamsForDetailed"]';
      var interval = pollIntervalMs || 100;
      var poller = setInterval(function () {
        if ($(selector + ' tr').length <= 1) return;
        clearInterval(poller);
        $(selector + ' tr:gt(1)')
          .find('td:eq(2) > div:not(.ng-hide)')
          .html(function () {
            var tracking = $(this).text().trim();
            var url;
            if (tracking.startsWith('1Z')) {
              url = 'https://wwwapps.ups.com/WebTracking/track?track=yes&trackNums=' +
                    tracking + '&requester=ST/trackdetails';
            } else {
              url = 'https://online.chrobinson.com/tracking/#/?trackingNumber=' +
                    tracking + '&requester=ST/trackdetails';
            }
            return '<a href="' + url + '" target="_blank">' + tracking + '</a>';
          });
      }, interval);
    },

    maskAccountNumbers: function (expectedLength, maskStart, maskEnd, maskChars) {
      var len   = expectedLength || 18;
      var start = maskStart      || 3;
      var end   = maskEnd        || 9;
      var mask  = maskChars      || 'XXXXXX';
      var observer = new MutationObserver(function () {
        document.querySelectorAll('div.ng-binding').forEach(function (el) {
          var text = el.textContent.trim();
          if (text.length === len) el.textContent = text.substring(0, start) + mask + text.substring(end);
        });
        document.querySelectorAll('td.ng-binding').forEach(function (cell) {
          var text = cell.textContent.trim();
          if (text.length === len) cell.textContent = text.substring(0, start) + mask + text.substring(end);
        });
      });
      observer.observe(document.body, { childList: true, subtree: true });
    },

    flagOldPackagesAsNonPrintable: function (packages, maxDays) {
      var threshold = maxDays || 30;
      var now       = new Date();
      (packages || []).forEach(function (pkg) {
        var sd = pkg.Shipdate;
        if (!sd) return;
        var shipDate = new Date(sd.Year, sd.Month - 1, sd.Day);
        var diffMs   = Math.abs(shipDate - now);
        var diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
        if (diffDays > threshold) pkg.CantPrint = true;
      });
    },

    filterByUser: function (searchCriteria, userId, fieldName, operator) {
      searchCriteria.WhereClauses.push({
        FieldName:  fieldName || 'UserId',
        FieldValue: userId,
        Operator:   operator !== undefined ? operator : 0,
      });
    },

    renameFirstHistoryReport: function (newLabel, pollIntervalMs) {
      var label    = newLabel || 'My History';
      var interval = pollIntervalMs || 50;
      var $reports = $('select[id="reports"]:eq(0)');
      var poller = setInterval(function () {
        if ($reports.length) {
          clearInterval(poller);
          $('select[id="reports"]:eq(0) option:eq(0)').text(label);
        }
      }, interval);
    },
  };
})();

// ---------------------------------------------------------------------------
//  PrintingOutput – ZPL injection, delegate labels, file I/O
// ---------------------------------------------------------------------------

/**
 * @module PrintingOutput
 * @see file://Docs/CBRAnalysis/Combined/printing and document output.js
 */
const PrintingOutput = (function () {
  'use strict';

  return {
    prePrint:  function () {},
    postPrint: function () {},

    injectZplDefaults: function (doc, targetSymbol, defaults) {
      if (doc.DocumentSymbol !== targetSymbol || !doc.RawData) return;
      var printerDefaults = defaults || '^XA^LH0,0^XSY,Y^MD30^XZ\n';
      var decoded = atou(doc.RawData[0]);
      doc.RawData[0] = utoa(printerDefaults + decoded);
    },

    openDelegateLabelPdf: function (shipmentRequest, clientService) {
      var data   = { Data: JSON.stringify(shipmentRequest) };
      var result = clientService
        .thinClientAPIRequest('UserMethod', data, false)
        .responseJSON;
      var decoded   = JSON.parse(atob(result.Data));
      var pdfBase64 = decoded.DocumentResponses[0].PdfData[0];
      var dataUri   = 'data:application/pdf;base64,' + pdfBase64;
      window.open(dataUri, '_blank');
      alert('Delegate Label Created');
    },

    downloadFile: function (fileName, clientService) {
      var payload = { Action: 'downloadFile', Data: fileName };
      var data    = { UserContext: undefined, Data: JSON.stringify(payload) };
      clientService.httpClient('UserMethod', data).then(function (ret) {
        if (ret.ErrorCode !== 0) return;
        var fileData = JSON.parse(atob(ret.Data));
        var linkData = 'data:' + fileData.fileType + ';base64,' + fileData.encodedFile;
        var link       = document.createElement('a');
        link.style.display = 'none';
        link.download      = fileData.fileName;
        link.href          = linkData;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      });
    },

    uploadFile: function (serverFileName, localFile, clientService, onComplete) {
      if (!localFile) return;
      var reader = new FileReader();
      reader.onload = function () {
        var request = {
          Key:   serverFileName,
          Value: JSON.stringify({
            fileType:    localFile.type,
            fileName:    localFile.name,
            encodedFile: reader.result.split(',')[1],
          }),
        };
        var payload = { Action: 'updateFile', Data: request };
        var data    = { UserContext: undefined, Data: JSON.stringify(payload) };
        clientService.httpClient('UserMethod', data).then(function (ret) {
          if (ret.ErrorCode !== 0) console.error('Upload error:', ret.ErrorCode, '-', ret.ErrorMessage);
          if (typeof onComplete === 'function') onComplete();
        });
      };
      reader.readAsDataURL(localFile);
    },
  };
})();

// ---------------------------------------------------------------------------
//  CustomBatchProcessing – Consolidated-order dialog, batch helpers
// ---------------------------------------------------------------------------

/**
 * @module CustomBatchProcessing
 * @see file://Docs/CBRAnalysis/Combined/custom batch processing.js
 */
const CustomBatchProcessing = (function () {
  'use strict';

  const SEL_ORDER_SELECT   = '#selectConsolidatedOrders';
  const SEL_ORDER_INPUT    = '#textOrderNumber';
  const SEL_MODAL          = '#divModalConsolidateShipments';
  const SEL_BATCH_DROPDOWN = '#cboBatches select';

  return {
    showConsolidatedOrderDialog: function () {
      Tools.ShowOverlay();
      var $select = $(SEL_ORDER_SELECT).empty();
      var currentRef = $(
        'input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]'
      ).val();
      var existingOrders = Tools.GetUniqueArrayFromString(currentRef);
      $(existingOrders).each(function (_idx, orderNum) {
        $select.append(
          $('<option />').val(orderNum).text(orderNum)
            .removeClass('not-processed').addClass('processed')
            .css('color', '#265ca1')
        );
      });
      $(SEL_MODAL).fadeIn('fast').promise().done(function () {
        $(SEL_ORDER_INPUT).val('').focus();
      });
    },

    addOrderNumberToList: function () {
      var inputVal = $(SEL_ORDER_INPUT).val();
      if (!inputVal || inputVal.length === 0) return false;
      var exists = false;
      $(SEL_ORDER_SELECT).find('option').each(function () {
        if (this.text === inputVal) { exists = true; return false; }
      });
      if (exists) { $(SEL_ORDER_INPUT).val('').focus(); return false; }
      $(SEL_ORDER_SELECT)
        .append($('<option />').val(inputVal).text(inputVal).addClass('not-processed').trigger('change'))
        .promise().done(function () { $(SEL_ORDER_INPUT).val('').focus(); });
    },

    saveConsolidatedOrderNumbers: function () {
      try {
        var ordersToQuery = '';
        $(SEL_ORDER_SELECT + ' > option.not-processed').each(function () {
          ordersToQuery += $(this).text() + ',';
        });
        $(SEL_MODAL).hide();
        if (ordersToQuery.length > 0) {
          Tools.MakeUserMethodRequest(
            'GetOrderInformation', ordersToQuery, true,
            this._processServerResponse
          );
        } else {
          Tools.ShowErrorAlert(
            'ERROR: No unique orders to pull from the server.  ' +
            'All consolidated orders have already been added to this shipment.'
          );
        }
      } catch (error) {
        CbrLogger.log({ Source: 'CustomBatchProcessing.saveConsolidatedOrderNumbers()', Error: error });
      }
    },

    _processServerResponse: function (jsonData) {
      try {
        $(jsonData).each(function (_idx, order) {
          if (AddressBookLogic.isConsigneeMatch(order)) {
            var currentPackage = Tools.GetCurrentPackage();
            if (!currentPackage) {
              Tools.ShowErrorAlert(
                'ERROR: Could not add commodities to package for order ' +
                order.orderNumber + '.  See error console for details.'
              );
              return false;
            }
            AddressBookLogic.appendOrderNumberToReferences(order.orderNumber);
          } else {
            Tools.ShowErrorAlert(
              'ERROR 500: Could not add commodities for order ' +
              order.orderNumber + '.  Address mismatch.'
            );
          }
        });
      } catch (error) {
        console.error(error);
      }
    },

    getBatches: function (clientService, viewModel) {
      var batches;
      var data = {
        SearchCriteria: null,
        CompanyId: viewModel.profile?.CompanyId || clientService.userContext?.CompanyId,
      };
      clientService
        .thinClientAPIRequest('GetBatches', data, false)
        .done(function (response) { batches = response.Batches; });
      return batches;
    },

    getSelectedBatchName: function () {
      return $(SEL_BATCH_DROPDOWN + ' option:selected').val().replace('string:', '');
    },

    validateItemPacking: function (orderItemList, mode) {
      if (!orderItemList) return true;
      var isOmsMode = (mode || '').toUpperCase() === 'OMS';
      for (var i = 0; i < orderItemList.length; i++) {
        var item = orderItemList[i];
        if (item.qtyordered !== item.qtyshipped) {
          if (isOmsMode) {
            var msg = 'SKU: ' + item.itemsku +
              ' has not been fully packed. Click OK to continue or Cancel to continue packing.';
            if (!confirm(msg)) throw new Error('Ship cancelled – please continue packing.');
          } else {
            throw new Error('SKU: ' + item.itemsku + ' has not been fully packed.');
          }
        }
      }
      return true;
    },
  };
})();

// ---------------------------------------------------------------------------
//  ThirdPartyOptions – Profile CRUD, manifest close-out
// ---------------------------------------------------------------------------

/**
 * @module ThirdPartyOptions
 * @see file://Docs/CBRAnalysis/Combined/third party options.js
 */
const ThirdPartyOptions = (function () {
  'use strict';

  const SEL_ORDER_SELECT = '#selectConsolidatedOrders';
  const SEL_ORDER_INPUT  = '#textOrderNumber';
  const SEL_MODAL        = '#divModalConsolidateShipments';

  return {
    showConsolidatedOrderDialog: function () {
      Tools.ShowOverlay();
      var $select = $(SEL_ORDER_SELECT).empty();
      var currentRef = $(
        'input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]'
      ).val();
      var existing = Tools.GetUniqueArrayFromString(currentRef);
      $(existing).each(function (_i, orderNum) {
        $select.append(
          $('<option />').val(orderNum).text(orderNum)
            .removeClass('not-processed').addClass('processed')
            .css('color', '#265ca1')
        );
      });
      $(SEL_MODAL).fadeIn('fast').promise().done(function () {
        $(SEL_ORDER_INPUT).val('').focus();
      });
    },

    addOrderToList: function () {
      var inputVal = $(SEL_ORDER_INPUT).val();
      if (!inputVal || !inputVal.length) return false;
      var exists = false;
      $(SEL_ORDER_SELECT + ' option').each(function () {
        if (this.text === inputVal) { exists = true; return false; }
      });
      if (exists) { $(SEL_ORDER_INPUT).val('').focus(); return false; }
      $(SEL_ORDER_SELECT)
        .append($('<option />').val(inputVal).text(inputVal).addClass('not-processed').trigger('change'))
        .promise().done(function () { $(SEL_ORDER_INPUT).val('').focus(); });
    },

    saveAndProcessOrders: function () {
      try {
        var csv = '';
        $(SEL_ORDER_SELECT + ' > option.not-processed').each(function () {
          csv += $(this).text() + ',';
        });
        $(SEL_MODAL).hide();
        if (csv.length > 0) {
          Tools.MakeUserMethodRequest(
            'GetOrderInformation', csv, true,
            function (jsonData) {
              try {
                $(jsonData).each(function (_i, order) {
                  if (AddressBookLogic.isConsigneeMatch(order)) {
                    var pkg = Tools.GetCurrentPackage();
                    if (!pkg) { Tools.ShowErrorAlert('Could not add commodities for order ' + order.orderNumber); return false; }
                    AddressBookLogic.appendOrderNumberToReferences(order.orderNumber);
                  } else {
                    Tools.ShowErrorAlert('Address mismatch for order ' + order.orderNumber);
                  }
                });
              } catch (err) { console.error(err); }
            }
          );
        } else {
          Tools.ShowErrorAlert('No unique orders to pull from the server.');
        }
      } catch (error) {
        CbrLogger.log({ Source: 'ThirdPartyOptions.saveAndProcessOrders()', Error: error });
      }
    },

    showModal: function ($modal) { if ($modal) $modal.modal('show'); },
    hideModal: function ($modal) { if ($modal) $modal.modal('hide'); },

    validateReference: function (value, validationList) {
      if (!validationList) return true;
      for (var i = 0; i < validationList.length; i++) {
        if (validationList[i].Value === value) return true;
      }
      throw { message: 'Unable to validate shipment', errorCode: '001' };
    },

    closeAllShippers: function (carrierSymbol, allShippers, clientService, userContext) {
      var apiUrl = clientService.config.ApiUrl;
      var auth   = clientService.authorizationToken();
      (allShippers || []).forEach(function (shipper) {
        var carriers = $.post({
          url: apiUrl + '/api/ShippingService/GetShipperCarriers',
          data: { ShipperId: shipper.Id, CompanyId: userContext.CompanyId },
          async: false, headers: auth,
        }).responseJSON;
        if (!carriers?.Carriers?.length) return;
        var items = $.post({
          url: apiUrl + '/api/ShippingService/GetManifestItems',
          data: {
            Carrier: carrierSymbol, Shipper: shipper.Symbol,
            SearchCriteria: { OrderByClauses: [{ FieldName: 'ShipDate', Direction: 'DESC' }] },
            IncludeImported: false, CompanyId: userContext.CompanyId,
          },
          async: false, headers: auth,
        }).responseJSON;
        if (!items?.ManifestItems?.length) return;
        var manifestItems = items.ManifestItems.map(function (mi) {
          return { Attributes: mi.Attributes, ShipDate: mi.ShipDate, Symbol: mi.Symbol, Name: mi.Name };
        });
        $.post({
          url: apiUrl + '/api/ShippingService/CloseManifest',
          data: {
            Carrier: carrierSymbol, ManifestItems: manifestItems,
            Shipper: shipper.Symbol, Print: true, UserParams: {},
            CompanyId: userContext.CompanyId,
          },
          async: false, headers: auth,
        });
      });
    },
  };
})();

// ---------------------------------------------------------------------------
//  PageInit – Page routing, shipping profiles, return labels, EOD
// ---------------------------------------------------------------------------

/**
 * @module PageInit
 * @see file://Docs/CBRAnalysis/Combined/page initialization and navigation.js
 */
const PageInit = (function () {
  'use strict';

  return {
    onPageLoaded: function (location, viewModel, handlers) {
      if (handlers && typeof handlers[location] === 'function') {
        handlers[location](viewModel);
      }
    },

    loadShippingProfiles: function (viewModel, clientService) {
      var request = { Action: 'L', ShippingProfileName: '' };
      var data    = { Data: JSON.stringify(request) };
      clientService.httpClient('UserMethod', data).then(function (ret) {
        viewModel.shippingProfiles = JSON.parse(atob(ret.Data));
      });
    },

    saveShippingProfile: function (shipmentRequest, viewModel, clientService, $modal) {
      shipmentRequest.Action = 'S';
      shipmentRequest.ShippingProfileName = viewModel.currentShippingProfile;
      if (viewModel.selectedServices && viewModel.selectedServices[0]) {
        shipmentRequest.SelService = viewModel.selectedServices[0].Symbol;
      }
      var data = { Data: JSON.stringify(shipmentRequest) };
      clientService.httpClient('UserMethod', data).then(function (ret) {
        viewModel.shippingProfiles = JSON.parse(atob(ret.Data));
      });
      if ($modal) $modal.modal('hide');
    },

    deleteShippingProfile: function (shipmentRequest, viewModel, clientService, $modal) {
      shipmentRequest.Action = 'D';
      shipmentRequest.ShippingProfileName = viewModel.currentShippingProfile;
      var data = { Data: JSON.stringify(shipmentRequest) };
      clientService.httpClient('UserMethod', data).then(function (ret) {
        viewModel.shippingProfiles = JSON.parse(atob(ret.Data));
      });
      if ($modal) $modal.modal('hide');
    },

    switchShippingProfile: function (viewModel) {
      var profiles = viewModel.shippingProfiles || [];
      var match = profiles.find(function (p) {
        return p.ShippingProfileName === viewModel.currentShippingProfile;
      });
      if (!match) return;
      var profile = structuredClone(match);
      var selsvc  = profile.SelService;
      if (profile.Packages[0].ProactiveRecovery === true) {
        profile.Packages[0].SelectedProactiveRecoveryInstructions = [4096, 2048, 32];
      }
      var today = new Date();
      profile.PackageDefaults.Shipdate = {
        Year: today.getFullYear(), Month: today.getMonth() + 1, Day: today.getDate(),
      };
      viewModel.currentShipment     = profile;
      viewModel.selectedServices    = viewModel.selectedServices || [];
      viewModel.selectedServices[0] = { Symbol: selsvc };
    },

    createReturnLabelFromPrevious: function (viewModel, serviceSymbol) {
      var svc = serviceSymbol || 'CONNECTSHIP_UPS.UPS.2DA';
      viewModel.currentShipment = structuredClone(viewModel.lastShipmentRequest);
      var pkg = viewModel.currentShipment.Packages[0];
      pkg.ReturnDelivery        = true;
      pkg.ReturnDeliveryMethod  = 0;
      pkg.ProactiveRecovery     = false;
      pkg.DirectDelivery        = false;
      pkg.Proof                 = false;
      pkg.ProofRequireSignature = false;
      viewModel.currentShipment.PackageDefaults.Service = { Symbol: svc };
      if (!viewModel.selectedServices) viewModel.selectedServices = [];
      viewModel.selectedServices.push(viewModel.currentShipment.PackageDefaults.Service);
    },

    processEndOfDay: function (shipmentRequest, clientService, $modal) {
      shipmentRequest.Action = 'EOD';
      var data = { Data: JSON.stringify(shipmentRequest) };
      clientService.httpClient('UserMethod', data).then(function () {
        if ($modal) $modal.modal('hide');
      });
    },
  };
})();


// ===========================================================================
//  CLIENT BUSINESS RULES CONSTRUCTOR
// ===========================================================================

/**
 * Main CBR constructor.
 *
 * Every lifecycle hook is wired up as a stub.  Override the hooks you need
 * and call the utility modules from within them.
 *
 * @constructor
 */
function ClientBusinessRules() {

  // Apply default no-op hooks for every lifecycle event.
  EventHandling.applyDefaults(this);

  // -----------------------------------------------------------------------
  //  Page & Navigation
  // -----------------------------------------------------------------------

  this.PageLoaded = function (location) {
    // Route to page-specific handlers:
    // PageInit.onPageLoaded(location, vm, {
    //   '/shipping':    function (vm) { EventHandling.sortShippers(vm); },
    //   '/history':     function (vm) { /* init history page */ },
    //   '/batchdetail': function ()   { /* init batch detail */ },
    // });
  };

  // -----------------------------------------------------------------------
  //  Keystroke
  // -----------------------------------------------------------------------

  this.Keystroke = function (shipmentRequest, event) {
    // Handle keystrokes on the shipping form.
    // Example: bind address code search on Tab:
    // AddressBookLogic.bindAddressCodeTabSearch(
    //   'Consignee Name Address', 'Consignee Name Address'
    // );
  };

  // -----------------------------------------------------------------------
  //  Shipment lifecycle
  // -----------------------------------------------------------------------

  this.NewShipment = function (shipmentRequest) {
    // Fires when a new shipment is created.
    // Example: focus the load input, set up return address, load cost center:
    // EventHandling.focusLoadInput();
    // AddressBookLookup.copyOriginToReturnAddress(vm);
    // ShippingDefaults.loadCostCenter(vm);
  };

  this.PreBuildShipment = function (shipmentRequest) {
    // Fires before shipment assembly.
  };

  this.PostBuildShipment = function (shipmentRequest) {
    // Fires after shipment assembly.
  };

  this.RepeatShipment = function (currentShipment) {
    // Fires when a shipment is duplicated.
  };

  // -----------------------------------------------------------------------
  //  Ship
  // -----------------------------------------------------------------------

  this.PreShip = function (shipmentRequest, userParams) {
    // Fires before a shipment is processed.
    // Example: stamp all packages with timestamp, validate dimensions:
    // TimestampUtils.stampAllPackages(shipmentRequest, 'MiscReference15');
    // ShipmentValidation.validateAndStoreDimensions(shipmentRequest);
    // ShippingDefaults.syncNotificationEmailFromDom(shipmentRequest);
  };

  this.PostShip = function (shipmentRequest, shipmentResponse) {
    // Fires after a shipment is processed.
  };

  // -----------------------------------------------------------------------
  //  Ship Order
  // -----------------------------------------------------------------------

  this.PreShipOrder = function (value, shipmentRequest, userParams) {
    // Fires before an order-based shipment.
  };

  this.PostShipOrder = function (shipmentRequest, shipmentResponse) {
    // Fires after an order-based shipment.
  };

  // -----------------------------------------------------------------------
  //  Load
  // -----------------------------------------------------------------------

  this.PreLoad = function (loadValue, shipmentRequest, userParams) {
    // Fires before a load/scan operation.
    // Example: show loader, capture state:
    // LoadStateManager.showLoader();
    // LoadStateManager.capturePreLoadState(shipmentRequest);
  };

  this.PostLoad = function (loadValue, shipmentRequest) {
    // Fires after a load/scan operation.
    // Example: hide loader, restore state, show errors:
    // LoadStateManager.hideLoader();
    // LoadStateManager.restorePostLoadState(shipmentRequest);
    // LoadStateManager.showLoadErrorIfPresent(shipmentRequest);
  };

  // -----------------------------------------------------------------------
  //  Rate
  // -----------------------------------------------------------------------

  this.PreRate = function (shipmentRequest, userParams) {
    // Fires before rating.
    // Example: sync notification email:
    // ShippingDefaults.syncNotificationEmailFromDom(shipmentRequest);
  };

  this.PostRate = function (shipmentRequest, rateResults) {
    // Fires after rating returns results.
  };

  // -----------------------------------------------------------------------
  //  Batch Processing
  // -----------------------------------------------------------------------

  this.PreProcessBatch = function (batchReference, actions, params) {
    // Fires before batch processing.
  };

  this.PostProcessBatch = function (batchResponse) {
    // Fires after batch processing.
  };

  // -----------------------------------------------------------------------
  //  Void
  // -----------------------------------------------------------------------

  this.PreVoid = function (pkg, userParams) {
    // Fires before voiding a package.
  };

  this.PostVoid = function (pkg) {
    // Fires after voiding a package.
  };

  // -----------------------------------------------------------------------
  //  Print
  // -----------------------------------------------------------------------

  this.PrePrint = function (document, localPort) {
    // Fires before printing a document.
    // Example: inject ZPL printer defaults:
    // PrintingOutput.injectZplDefaults(document, 'UPSAPI.UPS.PACKAGE_LABEL.STANDARD');
  };

  this.PostPrint = function (document) {
    // Fires after printing.
  };

  // -----------------------------------------------------------------------
  //  Manifest
  // -----------------------------------------------------------------------

  this.PreCloseManifest = function (manifestItem, userParams) {
    // Fires before closing a manifest.
  };

  this.PostCloseManifest = function (manifestItem) {
    // Fires after closing a manifest.
  };

  // -----------------------------------------------------------------------
  //  Transmit
  // -----------------------------------------------------------------------

  this.PreTransmit = function (transmitItem, userParams) {
    // Fires before transmitting.
  };

  this.PostTransmit = function (transmitItem) {
    // Fires after transmitting.
  };

  // -----------------------------------------------------------------------
  //  History Search
  // -----------------------------------------------------------------------

  this.PreSearchHistory = function (searchCriteria) {
    // Fires before searching history.
    // Example: filter to current user:
    // UserContextFilter.filterHistoryByUser(searchCriteria, vm);
    // BatchHistoryOps.setHistoryStartDate(searchCriteria, vm, 90);
  };

  this.PostSearchHistory = function (packages) {
    // Fires after history search returns results.
    // Example: anonymise costs, add tracking links:
    // PackageHistoryAnon.anonymiseCosts(packages);
    // PackageHistoryAnon.addTrackingLinks();
    // PackageHistoryAnon.flagOldPackagesAsNonPrintable(packages, 30);
  };

  // -----------------------------------------------------------------------
  //  Address Book
  // -----------------------------------------------------------------------

  this.PostSelectAddressBook = function (shipmentRequest, nameaddress) {
    // Fires after the user selects an address from the address book.
    // Example: sync notification email:
    // ShippingDefaults.syncNotificationEmailFromDom(shipmentRequest);
  };

  // -----------------------------------------------------------------------
  //  Package Operations
  // -----------------------------------------------------------------------

  this.AddPackage = function (shipmentRequest, packageIndex) {
    // Fires when a package is added.
    // Example: copy references from previous package:
    // ShipmentValidation.copyFromPreviousPackage(
    //   shipmentRequest, packageIndex,
    //   ['ShipperReference', 'MiscReference1']
    // );
  };

  this.CopyPackage = function (shipmentRequest, packageIndex) {
    // Fires when a package is copied.
  };

  this.RemovePackage = function (shipmentRequest, packageIndex) {
    // Fires when a package is removed.
  };

  this.PreviousPackage = function (shipmentRequest, packageIndex) {
    // Fires when navigating to the previous package.
  };

  this.NextPackage = function (shipmentRequest, packageIndex) {
    // Fires when navigating to the next package.
  };

  // -----------------------------------------------------------------------
  //  Groups
  // -----------------------------------------------------------------------

  this.PreCreateGroup = function (group, userParams) {
  };

  this.PostCreateGroup = function (group) {
  };

  this.PreModifyGroup = function (group, userParams) {
  };

  this.PostModifyGroup = function (group) {
  };

  this.PreCloseGroup = function (group, userParams) {
  };

  this.PostCloseGroup = function (group) {
  };
}
