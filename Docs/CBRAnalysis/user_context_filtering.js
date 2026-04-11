/**
 * User Context Filtering — PreSearchHistory user/traveler filters,
 * GetUserContext, setMailroomOrTraveler, GetCustom data accessors,
 * and cost-center / origin-address helpers.
 *
 * Patterns found:
 *   - ABB Optical / AIG / COREBRIDGE / Applied Material: Filter history by UserId
 *   - AlstonBird: Filter history by MiscReference20 for traveler profiles
 *   - Amplifon: Load cost center from CustomData, copy origin address to return address
 *   - Avery / APPNEXUS: Shipment helper class with UserRef/ToRef getters
 *   - ASEA: getUserContext via GET to /api/usercontext
 */

// ---------------------------------------------------------------------------
// User-context retrieval
// ---------------------------------------------------------------------------

/**
 * Returns the user context from the view model (checks both common locations).
 *
 * @param {object} vm - The view model.
 * @returns {object|undefined} { CompanyId, UserId }
 */
function getUserContext(vm) {
  if (vm.userContext)      return vm.userContext;
  if (vm.UserInformation) return vm.UserInformation;
}

/**
 * Fetches the user context from the server via a GET request.
 *
 * @param {object} client - The client service (client.config.ShipExecServiceUrl, client.getAuthorizationToken).
 */
function fetchUserContext(client) {
  var url   = client.config.ShipExecServiceUrl.replace('ShippingService', 'usercontext/GET');
  var token = client.getAuthorizationToken();
  client.userContext = $.get({ url: url, headers: token, async: false }).responseJSON;
}

// ---------------------------------------------------------------------------
// Mailroom / Traveler detection
// ---------------------------------------------------------------------------

/**
 * Determines whether the profile is Mailroom or Traveler and returns
 * configuration flags.
 *
 * @param {string} profileName - The current profile name.
 * @returns {object} { IsMailroom, IsTraveler, LoadRadioButtonsIsHidden }
 */
function setMailroomOrTraveler(profileName) {
  return {
    IsMailroom:               profileName !== 'Traveler Profile',
    IsTraveler:               profileName === 'Traveler Profile',
    LoadRadioButtonsIsHidden: true
  };
}

// ---------------------------------------------------------------------------
// Custom data accessors
// ---------------------------------------------------------------------------

/**
 * Extracts a custom field value from either the User (OriginAddress) or
 * Consignee custom data.
 *
 * @param {string} fieldName       - The key to look up (e.g. "Custom1").
 * @param {string} source          - "User" or "To".
 * @param {object} shipmentRequest - The current shipment request.
 * @returns {string} The matching value, or empty string.
 */
function getCustom(fieldName, source, shipmentRequest) {
  var customData = null;
  if (source === 'User') customData = shipmentRequest?.PackageDefaults?.OriginAddress?.CustomData;
  if (source === 'To')   customData = shipmentRequest?.PackageDefaults?.Consignee?.CustomData;

  if (!customData) return '';
  var result = '';
  customData.forEach(function (item) {
    if (item.Key === fieldName) result = item.Value;
  });
  return result;
}

/**
 * Shipment helper class providing convenient getters for CustomData fields
 * (UserRef1 through UserRef10, ToRef1 through ToRef10).
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function Shipment(shipmentRequest) {
  this.ShipmentRequest = shipmentRequest;
}

/** @returns {string} */
Shipment.prototype.GetCustom = function (fieldName, source) {
  return getCustom(fieldName, source, this.ShipmentRequest);
};

/* Generate convenience getters for Custom1..Custom10 for both User and To */
[1,2,3,4,5,6,7,8,9,10].forEach(function (n) {
  Object.defineProperty(Shipment.prototype, 'UserRef' + n, {
    get: function () { return this.GetCustom('Custom' + n, 'User'); }
  });
  Object.defineProperty(Shipment.prototype, 'ToRef' + n, {
    get: function () { return this.GetCustom('Custom' + n, 'To'); }
  });
});

// ---------------------------------------------------------------------------
// Cost center and origin address (Amplifon)
// ---------------------------------------------------------------------------

/**
 * Loads a cost-center value from the user's CustomData and sets it as the
 * ShipperReference, disabling the field.
 *
 * @param {object} vm         - The view model.
 * @param {string} customKey  - The CustomData key (e.g. "CostCenter").
 */
function loadCostCenter(vm, customKey) {
  var customData = vm?.profile?.UserInformation?.Address?.CustomData;
  if (!customData) return;

  var entry = customData.find(function (item) { return item.Key === customKey; });
  if (!entry) return;

  vm.currentShipment.Packages[vm.packageIndex].ShipperReference = entry.Value;
  $('[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]').prop('disabled', true);
}

/**
 * Copies the user's origin address to the shipment's return address.
 *
 * @param {object} vm - The view model.
 */
function copyOriginToReturnAddress(vm) {
  var originAddress = vm?.profile?.UserInformation?.Address;
  if (!originAddress || $.isEmptyObject(originAddress)) return;
  vm.currentShipment.PackageDefaults.ReturnAddress = originAddress;
}
