/**
 * Thin Client API Request — AJAX wrappers for communicating with the
 * ShipExec service API (via config.json or direct URL), UserMethod
 * requests, and user-context initialisation.
 *
 * Patterns found:
 *   - Config-based: Read ShipExecServiceUrl from config.json, attach Bearer token
 *   - Direct: Use client.config.ShipExecServiceUrl with getAuthorizationToken()
 *   - MakeUserMethodRequest: Full AJAX wrapper with loader, callbacks, and error handling
 *   - SetCurrentUserContext: Populate CompanyId/UserId from vm or GET /api/usercontext
 *   - ajaxGet: Simple synchronous GET helper
 */

// ---------------------------------------------------------------------------
// Config-based API request
// ---------------------------------------------------------------------------

/**
 * Makes an API request by first reading the service URL from config.json,
 * then performing a POST with a Bearer token.
 *
 * @param {string}  method - The API method path (e.g. "UserMethod").
 * @param {object}  data   - The POST data.
 * @param {boolean} isAsync - Whether the request is asynchronous.
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

// ---------------------------------------------------------------------------
// Direct API request
// ---------------------------------------------------------------------------

/**
 * Makes a POST request to the ShipExec service using a pre-configured
 * client object.
 *
 * @param {string}  method - The API method path.
 * @param {object}  data   - The POST data.
 * @param {boolean} isAsync - Whether the request is asynchronous.
 * @param {object}  client  - The client service with `config.ShipExecServiceUrl` and `getAuthorizationToken()`.
 * @returns {jqXHR} The jQuery AJAX promise.
 */
function thinClientApiRequest(method, data, isAsync, client) {
  return $.post({
    url:     client.config.ShipExecServiceUrl + '/' + method,
    data:    data,
    async:   isAsync,
    headers: client.getAuthorizationToken()
  });
}

// ---------------------------------------------------------------------------
// UserMethod request with loader and callbacks
// ---------------------------------------------------------------------------

/**
 * Makes a UserMethod request with loader display, error handling, and an
 * optional callback that receives the parsed response.
 *
 * @param {string}   requestMethod - The server method name.
 * @param {string}   requestData   - The data payload.
 * @param {boolean}  [isAsync=true] - Whether the request is asynchronous.
 * @param {function} [callback]    - Called with the parsed response JSON.
 * @returns {object|undefined} The parsed response (only available for sync calls).
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

// ---------------------------------------------------------------------------
// User context initialisation
// ---------------------------------------------------------------------------

var _currentUserContext = {};

/**
 * Populates the current user context (CompanyId, UserId) from the
 * view model or via a synchronous GET to /api/usercontext.
 *
 * @param {object}  viewModel   - The AngularJS view model.
 * @param {boolean} includeUser - If true, fetches UserId via AJAX if not available.
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

// ---------------------------------------------------------------------------
// Simple synchronous GET helper
// ---------------------------------------------------------------------------

/**
 * Performs a synchronous AJAX GET request and returns the response.
 *
 * @param {string} url - The URL to GET.
 * @returns {object} The response data.
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

// ---------------------------------------------------------------------------
// Loader helpers (referenced by other functions)
// ---------------------------------------------------------------------------

function showLoader() {
  $('div.loading').removeClass('ng-hide').addClass('ng-show');
}

function hideLoader() {
  $('div.loading').removeClass('ng-show').addClass('ng-hide');
}
