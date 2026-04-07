/**
 * Close Manifest Processing — Consolidated order saving, EOD processing,
 * and close-manifest workflows.
 *
 * All individual PreCloseManifest / PostCloseManifest stubs were empty.
 * The meaningful code is consolidated order saving and EOD processing.
 */

/**
 * Gathers unprocessed order numbers from the consolidated orders dialog
 * and submits them to the server for processing.
 *
 * @param {function} makeUserMethodRequest - The Tools.MakeUserMethodRequest function.
 * @param {function} processCallback - Callback to handle the returned order data.
 */
function saveConsolidatedOrderNumbers(makeUserMethodRequest, processCallback) {
  try {
    var ordersToQuery = '';
    $('#selectConsolidatedOrders > option.not-processed').each(function () {
      ordersToQuery += $(this).text() + ',';
    });

    $('#divModalConsolidateShipments').hide();

    if (ordersToQuery.length > 0) {
      makeUserMethodRequest('GetOrderInformation', ordersToQuery, true, processCallback);
    } else {
      Tools.ShowErrorAlert('ERROR: No unique orders to pull from the server.');
    }
  } catch (error) {
    Logger.Log({ Source: 'saveConsolidatedOrderNumbers()', Error: error });
  }
}

/**
 * Processes order data returned from the server after a consolidated order request.
 * Validates each order's consignee matches the current shipment.
 *
 * @param {object[]} orders - Array of order objects from the server.
 */
function processConsolidatedOrderData(orders) {
  try {
    $(orders).each(function (index, order) {
      if (ConsolidatedOrders.CheckForMatchingConsignee(order)) {
        var currentPackage = Tools.GetCurrentPackage();
        if (!currentPackage) {
          Tools.ShowErrorAlert('ERROR: Could not add commodities for order ' + order.orderNumber);
          return false;
        }
        ConsolidatedOrders.UpdateReferencesWithOrderNumbers(order.orderNumber);
      } else {
        Tools.ShowErrorAlert('ERROR 500: Address mismatch for order ' + order.orderNumber);
      }
    });
  } catch (error) {
    console.log(error);
  }
}

/**
 * Closes the consolidated orders dialog without saving and hides the loader.
 */
function closeDialogWithoutSaving() {
  $('#selectConsolidatedOrders').empty();
  $('#divModalConsolidateShipments').hide();
  Tools.HideLoader();
}

/**
 * Submits an End-of-Day (EOD) processing request to the server.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {function} httpClient - The HTTP client function (e.g. client.httpClient).
 * @param {object}   $modal - jQuery reference to the EOD modal to hide on success.
 */
function processEndOfDay(shipmentRequest, httpClient, $modal) {
  shipmentRequest.Action = 'EOD';
  var data = { Data: JSON.stringify(shipmentRequest) };

  httpClient('UserMethod', data).then(function () {
    $modal.modal('hide');
  });
}
