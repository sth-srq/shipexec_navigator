/**
 * Logging - Client-side logging framework with console output and optional
 * server-side logging via the UserMethod endpoint.
 *
 * Log levels mirror the server-side SBR nomenclature:
 *   Error, Info, Debug, Trace, Fatal
 */

var LogLevel = {
  Error: 'Error',
  Info:  'Info',
  Debug: 'Debug',
  Trace: 'Trace',
  Fatal: 'Fatal'
};

var Logger = {

  /**
   * Logs a message to the console and optionally to the server.
   *
   * @param {object} logObject - { Source, Error?, Message?, Data?, LogLevel? }
   */
  Log: function (logObject) {
    try {
      if (logObject.Error) {
        logObject.LogLevel = LogLevel.Error;
        logObject.Error = { name: logObject.Error.name, message: logObject.Error.message };
      } else if (!logObject.LogLevel) {
        logObject.LogLevel = LogLevel.Info;
      }

      if (logObject.LogLevel === LogLevel.Error) {
        console.error('Exception encountered in', logObject.Source);
        console.error(logObject.Error.name);
        console.error(logObject.Error.message);
        if (logObject.Data) console.log(logObject.Data);
      } else {
        console.log('Output from', logObject.Source);
        console.log(logObject.Message);
        if (logObject.Data) console.log(logObject.Data);
      }

      if (Tools.GetServerDebugMode() === true) {
        var ajaxObject = {
          UserContext: Tools.GetCurrentUserContext(),
          Data: JSON.stringify({ ServerMethod: 'AddClientEntry', MessageObject: logObject })
        };

        $.ajax({
          url:         'api/ShippingService/UserMethod',
          method:      'POST',
          contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
          dataType:    'json',
          data:        ajaxObject,
          async:       true
        }).fail(function (jqXHR, textStatus) {
          console.log('Unable to log message to server.');
        });
      }

      Tools.HideLoader();
    } catch (error) {
      console.log(error.message);
    }
  }
};

// D2M-specific logging helpers

/**
 * Logs the start of a method with an optional callee chain.
 * @param {string} method
 * @param {string} [calleeMethod]
 */
function logStartMethod(method, calleeMethod) {
  if (calleeMethod) {
    console.log('...STARTING ' + method + ' called by ' + calleeMethod);
  } else {
    console.log('...STARTING ' + method);
  }
}

/**
 * Logs an informational message indented to indicate sub-method detail.
 * @param {string} message
 */
function logMethodInfo(message) {
  console.log('      ' + message);
}

/**
 * Safely decodes a base64-encoded JSON string returned by the server.
 * @param {string} data - The encoded data string.
 * @returns {object|undefined}
 */
function decodeReturnString(data) {
  try {
    return JSON.parse($('<div />').html(data).text());
  } catch (error) {
    console.log('DecodeReturnString Error: ' + error.name + ' ' + error.message);
  }
}
