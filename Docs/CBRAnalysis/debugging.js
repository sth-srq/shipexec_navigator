/**
 * Debugging — Logger utility with configurable server-side logging
 * and console output by log level.
 */

var LogLevel = { Error: 'Error', Info: 'Info', Debug: 'Debug', Trace: 'Trace', Fatal: 'Fatal' };
var serverLogging = false;

/**
 * Enables or disables server-side debug logging.
 *
 * @param {boolean} enabled - True to write log entries to the server.
 */
function setServerDebugMode(enabled) {
  console.log('Logging to server:', enabled);
  serverLogging = enabled;
}

/**
 * Logs a message to the console and optionally to the server.
 *
 * Usage examples:
 *   log({ Source: 'fn()', Error: error })
 *   log({ Source: 'fn()', Message: 'info text', Data: someObject })
 *   log({ Source: 'fn()', LogLevel: 'Debug', Message: 'debug text' })
 *
 * @param {object} entry - Log entry with Source, and optionally Error, Message, Data, LogLevel.
 */
function log(entry) {
  try {
    if (entry.Error) {
      entry.LogLevel = LogLevel.Error;
      entry.Error    = { name: entry.Error.name, message: entry.Error.message };
    } else if (!entry.LogLevel) {
      entry.LogLevel = LogLevel.Info;
    }

    /* Console output */
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

    /* Server-side logging */
    if (serverLogging) {
      var ajaxData = {
        UserContext: Tools.GetCurrentUserContext(),
        Data: JSON.stringify({ ServerMethod: 'AddClientEntry', MessageObject: entry })
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

    Tools.HideLoader();
  } catch (error) {
    console.log(error.message);
  }
}
