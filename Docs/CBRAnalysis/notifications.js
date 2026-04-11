/**
 * Notifications — Mailroom vs Traveler NewShipment configuration,
 * email notification defaults, and D2M enable/disable via UserMethod.
 *
 * Patterns found:
 *   - AlstonBird mailroom/traveler mode detection and NewShipment setup
 *   - Default email notification fields to current user's email
 *   - D2M EnableD2MShipping UserMethod call
 *
 * See also: alerts.js for ShowErrorAlert, d2m.js for D2M authorization checks.
 */

// ---------------------------------------------------------------------------
// Mailroom vs Traveler mode
// ---------------------------------------------------------------------------

/**
 * Determines whether the current profile is a "Mailroom" or "Traveler" profile.
 *
 * @param {string} profileName - The current profile name.
 * @returns {object} { IsMailroom: boolean, IsTraveler: boolean, LoadRadioButtonsIsHidden: boolean }
 */
function setMailroomOrTraveler(profileName) {
  return {
    IsMailroom:               profileName !== 'Traveler Profile',
    IsTraveler:               profileName === 'Traveler Profile',
    LoadRadioButtonsIsHidden: true
  };
}

// ---------------------------------------------------------------------------
// Email notification defaults
// ---------------------------------------------------------------------------

/**
 * Sets all notification email fields on a package to the given email address.
 *
 * @param {object} pkg   - The package object.
 * @param {string} email - The email address to populate.
 */
function setNotificationDefaults(pkg, email) {
  pkg.ShipNotificationEmail                    = true;
  pkg.ShipNotificationAddressEmail             = email;
  pkg.DeliveryNotificationEmail                = true;
  pkg.DeliveryNotificationAddressEmail         = email;
  pkg.DeliveryExceptionNotificationEmail       = true;
  pkg.DeliveryExceptionNotificationAddressEmail = email;
}

/**
 * Binds notification checkbox clicks to auto-populate the corresponding
 * email field with the user's email.
 *
 * @param {object} vm        - The view model.
 * @param {function} getUserName - Function returning the current user's email.
 */
function bindNotificationCheckboxes(vm, getUserName) {
  var mappings = {
    '[property="ShipNotificationEmail"]':              '[name="shipNotificationAddressEmail"]',
    '[property="DeliveryExceptionNotificationEmail"]': '[name="DeliveryExceptionNotificationAddressEmail"]',
    '[property="DeliveryNotificationEmail"]':          '[name="DeliveryNotificationEmail"]'
  };

  Object.keys(mappings).forEach(function (checkboxSelector) {
    $('body').delegate(checkboxSelector, 'click', function () {
      var emailField = mappings[checkboxSelector];
      $(emailField).val(getUserName()).change();
    });
  });
}

// ---------------------------------------------------------------------------
// D2M enable via UserMethod
// ---------------------------------------------------------------------------

/**
 * Enables D2M (Direct-to-Mobile) shipping by calling the server-side
 * UserMethod and refreshing the profile.
 *
 * @param {function} thinClientAPIRequest - The API request function.
 * @param {object}   userContext          - The current user context.
 * @param {object}   vm                   - The view model (vm.profile is updated).
 */
function enableD2MShipping(thinClientAPIRequest, userContext, vm) {
  var params = { ActionMessage: 'EnableD2MShipping' };
  var data   = { Data: JSON.stringify(params), UserContext: userContext };
  var response = thinClientAPIRequest('UserMethod', data, false);

  if (response && response.responseJSON.ErrorCode !== 0) {
    alert('Error while executing UserMethod: ' + response.responseJSON.ErrorCode);
  } else {
    vm.profile = decodeReturnString(response.responseJSON.Data);
  }
}
