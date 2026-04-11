/**
 * Shipment Commodity Contents Initialization — D2M return-delivery
 * configuration, shipper-to-return-address copy, description helpers,
 * and commodity tab refresh.
 *
 * This file is a cross-reference. The canonical implementations live in:
 *   - return_delivery_address.js (setReturnDeliveryConfiguration,
 *     clearReturnDeliveryConfiguration, loadReturnAddressFromShipper)
 *   - commodity_mapping.js (commodity content object building)
 *   - loading.js (applyShippingProfile for AllCare profile CRUD)
 *
 * Unique content:
 *   - UpdateCommodities: Refreshes the commodity list UI after adding items
 *   - OnShipperChange: Toggles return-delivery config based on D2M state
 */

/**
 * Refreshes the commodity-contents UI by clicking the active tab and
 * the first inactive pagination button.
 */
function refreshCommodityUI() {
  $('div.ui-tab-container > div.ng-isolate-scope > ul > li.active > a').click();
  $('#goods').find('div.ng-table-counts.btn-group').find('button:not(.active):first').click();
}

/**
 * Handles shipper change events in D2M mode: enables return-delivery
 * configuration when the D2M shipper is selected, clears it otherwise.
 *
 * @param {string}  currentShipper   - The newly selected shipper symbol.
 * @param {string}  d2mShipperSymbol - The authorized D2M shipper symbol.
 * @param {boolean} isD2MCapable     - Whether the session supports D2M.
 * @param {object}  vm               - The view model.
 */
function onShipperChangeD2M(currentShipper, d2mShipperSymbol, isD2MCapable, vm) {
  if (isD2MCapable && currentShipper === d2mShipperSymbol) {
    setDescription('Returned Goods');
    loadReturnAddressFromShipper(vm);
  } else {
    setDescription('');
    setReturnDeliveryEmail('');
    $('select[ng-model="vm.currentShipment.Packages[vm.packageIndex].CommercialInvoiceMethod"]').val('');
  }
}

/* Helpers referenced from return_delivery_address.js */
function setDescription(value) {
  $('textarea[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]').val(value);
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]').val(value);
}

function setReturnDeliveryEmail(value) {
  $('input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ReturnDeliveryAddressEmail"]').val(value);
}
