/**
 * Alerts — Display error and informational alerts to the user.
 *
 * Patterns found:
 *   - Simple lifecycle-hook alerts (PageLoaded, PreShip, PreRate, RemovePackage)
 *   - Bootstrap-style dismissible error banners (ShowErrorAlert)
 *   - Modal-based alerts with a title and message (ShowAlert)
 *   - PostLoad error display from server response
 */

// ---------------------------------------------------------------------------
// Bootstrap dismissible error banner
// ---------------------------------------------------------------------------

/**
 * Creates a Bootstrap-style dismissible error alert at the bottom of the page.
 *
 * @param {string} errorMessage - The message to display.
 * @param {function} getRandomId - A function that returns a unique element id.
 */
function showErrorAlert(errorMessage, getRandomId) {
  var divAlert = $('<div />')
    .attr('id', getRandomId())
    .addClass('alert alert-dismissible alert-bottom alert-danger ng-scope')
    .css({ 'z-index': 1550, cursor: 'pointer' })
    .attr('role', 'alert')
    .append(
      $('<button />')
        .attr('type', 'button')
        .addClass('close')
        .append($('<span />').attr('aria-hidden', 'false').text('x'))
        .append($('<span />').addClass('sr-only').text('Close'))
    )
    .append(
      $('<div />').append(
        $('<div />')
          .addClass('ng-scope alert-message')
          .text(errorMessage)
          .prepend(
            $('<span />')
              .css('padding-right', '10px')
              .addClass('glyphicon glyphicon-alert custom-error-icon')
          )
      )
    );

  $('body').append(divAlert.show());
}

// ---------------------------------------------------------------------------
// Modal-based alert
// ---------------------------------------------------------------------------

/**
 * Shows a modal alert with a title and message.
 *
 * @param {string} title - The alert title.
 * @param {string} message - The alert body text.
 * @param {object} $alertModal - A jQuery reference to the modal element.
 */
function showModalAlert(title, message, $alertModal) {
  vm.currentShipment.AlertTitle = title;
  vm.currentShipment.AlertMessage = message;
  $alertModal.modal('show');
}

// ---------------------------------------------------------------------------
// PostLoad error display
// ---------------------------------------------------------------------------

/**
 * After loading a shipment, checks for a server-side error code and alerts
 * the user if one is present.
 *
 * @param {object} shipmentRequest - The current shipment request object.
 */
function showPostLoadError(shipmentRequest) {
  if (shipmentRequest.PackageDefaults.ErrorCode === 1) {
    alert(shipmentRequest.PackageDefaults.ErrorMessage);
    shipmentRequest.PackageDefaults.ErrorCode = 0;
  }
}
