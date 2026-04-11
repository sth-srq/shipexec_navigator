/**
 * Load State Management — Preserving and restoring shipment state across
 * PreLoad / PostLoad lifecycle hooks.
 *
 * Patterns found:
 *   - Saving shipper and batch name before load, restoring after
 *   - Showing / hiding loader during load
 *   - PostLoad error display
 *   - PostLoad alert from server-side UserData1
 *   - PostLoad email restoration
 *   - PostLoad test-data detection
 */

// ---------------------------------------------------------------------------
// Preserve and restore fields across a load
// ---------------------------------------------------------------------------

/**
 * Saves transient shipment fields before the load so they can be restored
 * afterward (e.g. the active shipper and batch name are overwritten by the
 * server response).
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @returns {object} A snapshot of the fields to restore.
 */
function capturePreLoadState(shipmentRequest) {
  return {
    shipper:   shipmentRequest.PackageDefaults.Shipper,
    batchName: shipmentRequest.Packages[0].MiscReference5,
    batchId:   $('#cboBatches select option:selected').val().replace('string:', '')
  };
}

/**
 * Restores previously saved fields after a load completes.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {object} savedState      - The snapshot returned by capturePreLoadState.
 */
function restorePostLoadState(shipmentRequest, savedState) {
  shipmentRequest.PackageDefaults.Shipper   = savedState.shipper;
  shipmentRequest.Packages[0].MiscReference5 = savedState.batchName;
}

// ---------------------------------------------------------------------------
// PostLoad alerts
// ---------------------------------------------------------------------------

/**
 * After a load, checks the server-side ErrorCode and displays the error
 * message if present.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function showPostLoadError(shipmentRequest) {
  if (shipmentRequest.PackageDefaults.ErrorCode === 1) {
    alert(shipmentRequest.PackageDefaults.ErrorMessage);
    shipmentRequest.PackageDefaults.ErrorCode = 0;
  }
}

/**
 * After a load, checks UserData1 for an alert message from the server.
 * A leading "~" displays a "new shipment" modal; otherwise a standard modal.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {jQuery} $alertModal     - The standard alert modal element.
 * @param {jQuery} $newShipModal   - The new-shipment alert modal element.
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

// ---------------------------------------------------------------------------
// PostLoad email restoration
// ---------------------------------------------------------------------------

/**
 * After a void, restores the consignee email into the UI field.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function restoreConsigneeEmail(shipmentRequest) {
  $('input[ng-model="nameaddress.Email"]').val(
    shipmentRequest.PackageDefaults.Consignee.Email
  );
}
