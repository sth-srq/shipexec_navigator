/**
 * UI — Show/hide loader, show/hide overlay, and Bootstrap dismissible
 * error alerts.
 *
 * These utilities are part of the "Tools" namespace and provide visual
 * feedback during async operations and error conditions.
 *
 * See also: alerts.js for the modal-based alert pattern.
 */

// ---------------------------------------------------------------------------
// Loader
// ---------------------------------------------------------------------------

/**
 * Shows the global loading spinner by toggling AngularJS CSS classes.
 *
 * @param {function} [callback] - Optional callback after showing.
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

// ---------------------------------------------------------------------------
// Overlay
// ---------------------------------------------------------------------------

/**
 * Shows a semi-transparent overlay over the page content, creating the
 * overlay element if it doesn't already exist.
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

// ---------------------------------------------------------------------------
// Dismissible error alert
// ---------------------------------------------------------------------------

/**
 * Creates and displays a Bootstrap-style dismissible error alert at the
 * bottom of the page.
 *
 * @param {string} errorMessage - The error message to display.
 */
function showErrorAlert(errorMessage) {
  var id = 'alert-' + Math.random().toString(36).substring(2, 10);

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

// ---------------------------------------------------------------------------
// Random string helper
// ---------------------------------------------------------------------------

/**
 * Generates a random alphanumeric string suitable for DOM element IDs.
 *
 * @returns {string}
 */
function getRandomAlphaNumericString() {
  return 'id-' + Math.random().toString(36).substring(2, 10);
}
