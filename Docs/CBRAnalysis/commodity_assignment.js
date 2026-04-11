/**
 * Commodity Assignment — Third-party billing based on origin/consignee
 * country, EU membership checks, and international commodity setup.
 */

/**
 * Determines whether third-party billing should be enabled based on the
 * origin and consignee countries, including EU intra-union logic.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {function} isMemberOfEu - Returns true if the country code is in the EU.
 */
function applyThirdPartyBillingRules(shipmentRequest, isMemberOfEu) {
  var shipFrom = shipmentRequest.PackageDefaults.OriginAddress.Country;
  var shipTo   = shipmentRequest.PackageDefaults.Consignee.Country;

  if (!shipTo || shipFrom === 'US' || shipFrom === 'CA') {
    disableThirdPartyBillingElements();
    return;
  }

  var shouldEnable = (shipFrom !== shipTo) ||
                     (isMemberOfEu(shipFrom) && isMemberOfEu(shipTo));

  if (shouldEnable) {
    setThirdPartyBilling(shipmentRequest);
  }

  if (shipFrom !== 'US') {
    $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].Weight.Units"]').val('string:KG').change();
    $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].Dimensions.Units"]').val('number:1').change();
  }
}

/**
 * Enables third-party billing and pre-fills the billing address.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function setThirdPartyBilling(shipmentRequest) {
  $('input[type=button][ng-disabled="!vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('disabled', false).attr('disabled', false);
  $('input[type=checkbox][ng-model="vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('checked', true).attr('checked', true).change();

  shipmentRequest.PackageDefaults.ThirdPartyBillingAddress = {
    Company:       'PRA Global',
    Address1:      '995 Research Park Blvd',
    Address2:      'Suite 300',
    City:          'Charlottesville',
    StateProvince: 'VA',
    PostalCode:    '22911',
    Account:       '883F79',
    Country:       'US'
  };
}

/**
 * Disables and un-checks the third-party billing checkbox and button.
 */
function disableThirdPartyBillingElements() {
  $('input[type=checkbox][ng-model="vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('checked', false).attr('checked', false);
  $('input[type=button][ng-disabled="!vm.currentShipment.PackageDefaults.ThirdPartyBilling"]')
    .prop('disabled', true).attr('disabled', true);
}
