/**
 * Customer Selection — Consolidated order dialog for selecting and adding
 * multiple orders to a single shipment.
 */

/**
 * Opens the consolidated order dialog, pre-populating with any existing
 * order numbers from the ShipperReference field.
 */
function showConsolidatedOrderDialog() {
  Tools.ShowOverlay();
  $('#selectConsolidatedOrders').empty();

  var shipperRefVal = $('input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]').val();
  var existingOrders = Tools.GetUniqueArrayFromString(shipperRefVal);

  $(existingOrders).each(function (i, orderNum) {
    $('#selectConsolidatedOrders').append(
      $('<option />').val(orderNum).text(orderNum)
        .removeClass('not-processed').addClass('processed')
        .css('color', '#265ca1')
    );
  });

  $('#divModalConsolidateShipments').fadeIn('fast').promise().done(function () {
    $('#textOrderNumber').val([]).focus();
  });
}

/**
 * Adds the typed order number to the consolidated orders list, preventing
 * duplicates.
 *
 * @returns {boolean} false if the order already exists.
 */
function addOrderNumberToList() {
  var value   = $('#textOrderNumber').val();
  var exists  = false;

  $('#selectConsolidatedOrders option').each(function () {
    if (this.text === value) {
      exists = true;
      return false;
    }
  });

  if (exists || value.length === 0) {
    $('#textOrderNumber').val([]).focus();
    return false;
  }

  $('#selectConsolidatedOrders').append(
    $('<option />').val(value).text(value).addClass('not-processed').trigger('change')
  ).promise().done(function () {
    $('#textOrderNumber').val([]).focus();
  });
}
