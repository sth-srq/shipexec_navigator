/**
 * Shipment Timestamp Stamping — Adding date/time stamps to MiscReference15
 * before ship and load, and shipping-profile change with date stamping.
 *
 * Patterns found:
 *   - CMR: Stamp MiscReference15 with dateTimeStamp() on PreShip
 *   - CMR: Stamp MiscReference15 with today's date on PreLoad
 *   - AllCare: Update Shipdate to current date on profile change
 *   - Clarios: CloseAllShippers (duplicated from close_manifest_processing.js)
 */

/**
 * Returns the current date/time as a locale string with timezone abbreviation.
 *
 * @returns {string} e.g. "4/7/2026, 14:30:00 EDT"
 */
function dateTimeStamp() {
  return new Date().toLocaleString('en-US', { timeZoneName: 'short', hour12: false });
}

/**
 * Returns today's date formatted for the locale.
 *
 * @returns {string}
 */
function todayString() {
  return new Date().toLocaleDateString();
}

/**
 * Stamps MiscReference15 on the current package with the provided value.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {number} packageIndex    - The current package index.
 * @param {string} stampValue      - The value to stamp (from dateTimeStamp or todayString).
 */
function stampMiscReference15(shipmentRequest, packageIndex, stampValue) {
  shipmentRequest.Packages[packageIndex].MiscReference15 = stampValue;
}

/**
 * Creates a shipdate object for the current date in the format expected
 * by the shipment model.
 *
 * @returns {object} { Year, Month, Day }
 */
function currentShipdate() {
  var today = new Date();
  return {
    Year:  today.getFullYear(),
    Month: today.getMonth() + 1,
    Day:   today.getDate()
  };
}
