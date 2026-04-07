/**
 * Batch Lookup — Retrieve and select batches, assign custom batch events,
 * and pre-populate batch-related references.
 *
 * Patterns found:
 *   - GetBatches via thinClientAPIRequest
 *   - Selected batch name from a dropdown
 *   - Custom batch processing button click handler
 *   - Shipping profile change with batch/service selection
 */

/**
 * Retrieves the list of batches from the server.
 *
 * @param {function} apiRequest - The thinClientAPIRequest function.
 * @param {string}   companyId - The company ID to filter batches.
 * @returns {object[]} The list of batch objects (synchronous).
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
 * Reads the currently selected batch name from the custom dropdown.
 *
 * @returns {string} The selected batch name with the "string:" prefix removed.
 */
function getSelectedBatchName() {
  return $('#cboBatches select option:selected').val().replace('string:', '');
}

/**
 * Binds a click handler to the custom batch processing button that
 * calls the server-side UserMethod.
 *
 * @param {object} clientConfig - Object with ShipExecServiceUrl property.
 * @param {object} userContext - The current user context.
 * @param {function} getAuthToken - Returns the authorization token header object.
 */
function assignCustomBatchProcessingEvent(clientConfig, userContext, getAuthToken) {
  $('#btnCustomBatchProcessing').on('click', function () {
    var params = { ActionMessage: 'CustomBatchProcessing' };
    var data   = { Data: JSON.stringify(params), UserContext: userContext };

    $.post({
      url:     clientConfig.ShipExecServiceUrl + '/UserMethod',
      data:    data,
      async:   false,
      headers: getAuthToken()
    });
  });
}
