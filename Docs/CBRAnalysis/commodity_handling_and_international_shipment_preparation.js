/**
 * Commodity Handling and International Shipment Preparation —
 * Refreshing commodities in the UI, translating units of measurement,
 * and preparing D2M return delivery configurations.
 */

/**
 * Refreshes the commodity list in the UI by clicking the active tab
 * and toggling the ng-table page-size buttons.
 */
function refreshCommodityDisplay() {
  $('div.ui-tab-container > div.ng-isolate-scope > ul > li.active > a').click();
  $('#goods').find('div.ng-table-counts.btn-group').find('button:not(.active):first').click();
}

/**
 * Translates verbose unit-of-measurement strings to standard abbreviations.
 *
 * @param {string} value - The unit string (e.g. "EACH", "YARDS", "PAIR").
 * @returns {string} The abbreviated code (e.g. "EA", "YD", "PR").
 */
function translateUnitOfMeasurement(value) {
  var map = {
    'EACH':  'EA',
    'YARDS': 'YD',
    'YARD':  'YD',
    'METER': 'M',
    'SF':    'SFT',
    'PAIR':  'PR',
    'SR':    'ROL'
  };
  return map[value.toUpperCase()] || value;
}
