/**
 * NMFC — LTL dimension matching, NMFC description/class assignment,
 * BOL comment generation, and dry-ice handling.
 *
 * Patterns found:
 *   - Interstate-McBee: LTL dimension table lookup, NMFC class assignment,
 *     BOL comment appending, parent container code generation
 *   - OSU: Dry-ice weight/regulation/purpose configuration
 *   - Reference concatenation for shipper/consignee references
 */

// ---------------------------------------------------------------------------
// LTL dimension matching
// ---------------------------------------------------------------------------

/**
 * Standard dimension pairs used for LTL BOL comment matching.
 * Each entry is [longestSide, secondSide].
 */
var LTL_DIMENSION_TABLE = [
  [34, 32], [48, 40], [52, 36],
  [12, 52], [14, 44], [18, 32],
  [18, 52], [28, 44], [37, 37]
];

/**
 * Matches package dimensions against the LTL dimension table and
 * returns a formatted dimension string for use in BOL comments.
 *
 * Dimensions are sorted descending before matching.
 *
 * @param {number} length - Package length.
 * @param {number} width  - Package width.
 * @param {number} height - Package height.
 * @returns {string} Formatted dimension string (e.g. "48x40x12") or "0x0x0".
 */
function matchLtlDimensions(length, width, height) {
  var dims = [length || 0, width || 0, height || 0];
  dims.sort(function (a, b) { return b - a; });

  if (dims[0] === 0 && dims[1] === 0 && dims[2] === 0) return '0x0x0';

  for (var i = 0; i < LTL_DIMENSION_TABLE.length; i++) {
    var x = LTL_DIMENSION_TABLE[i][0];
    var y = LTL_DIMENSION_TABLE[i][1];

    if (i < 3) {
      if (dims[0] === x && dims[1] === y) return x + 'x' + y + 'x' + dims[2];
      if (dims[1] === x && dims[2] === y) return x + 'x' + y + 'x' + dims[0];
    } else {
      if (dims[0] === y && dims[1] === x) return x + 'x' + y + 'x' + dims[2];
      if (dims[0] === y && dims[2] === x) return x + 'x' + y + 'x' + dims[1];
      if (dims[1] === y && dims[2] === x) return x + 'x' + y + 'x' + dims[0];
    }
  }

  return dims.join('x');
}

// ---------------------------------------------------------------------------
// LTL package defaults (Interstate-McBee)
// ---------------------------------------------------------------------------

/**
 * Configures an LTL package with NMFC description, BOL comment containing
 * matched dimensions, parent container code, and waybill number.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {number} packageIndex    - The 0-based index of the new package.
 * @param {string} nmfcDescription - The NMFC description text (e.g. "DIESEL ENGINE PARTS NMFC # 133300, SUB 4, Class 60").
 */
function configureLtlPackage(shipmentRequest, packageIndex, nmfcDescription) {
  var pkg = shipmentRequest.Packages[packageIndex - 1];
  if (!pkg) return;

  /* Match dimensions */
  var d = pkg.Dimensions || {};
  var dimString = matchLtlDimensions(d.Length, d.Width, d.Height);
  if (dimString !== '0x0x0') {
    pkg.BolComment = ((pkg.BolComment || '') + ' ' + dimString).trim();
  }

  /* Set NMFC description */
  pkg.Description = nmfcDescription + ' L' + packageIndex;

  /* Default waybill BOL number from MiscReference20 */
  if (!pkg.WaybillBolNumber) {
    pkg.WaybillBolNumber = pkg.MiscReference20;
  }

  /* Set parent container code when MiscReference20 is present */
  if (pkg.MiscReference20 && !shipmentRequest.Packages[packageIndex]?.ParentContainerCode) {
    shipmentRequest.Packages[packageIndex] = shipmentRequest.Packages[packageIndex] || {};
    if (!shipmentRequest.Packages[packageIndex].ParentContainerCode) {
      shipmentRequest.Packages[packageIndex].ParentContainerCode = 'P' + pkg.MiscReference20 + '-' + packageIndex;
    }
  }
}

// ---------------------------------------------------------------------------
// Dry-ice handling (OSU)
// ---------------------------------------------------------------------------

/**
 * Validates and configures dry-ice settings on all packages.
 * Throws if multiple packages are used with dry ice or if the weight is missing.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @throws {object} Error with message and errorCode.
 */
function configureDryIce(shipmentRequest) {
  var dryIceWeightRaw = returnPropertyValue(shipmentRequest.Packages[0].MiscReference8);
  var consigneeCountry = shipmentRequest.PackageDefaults.Consignee.Country;

  shipmentRequest.Packages.forEach(function (pkg) {
    if (!pkg.MiscReference7) return;

    if (shipmentRequest.Packages.length > 1) {
      throw { message: 'Multiple Packages Not Supported with Dry Ice', errorCode: '' };
    }
    if (returnPropertyValue(pkg.MiscReference8) === '') {
      throw { message: 'Missing Dry Ice Weight', errorCode: '' };
    }

    pkg.DryIceWeight  = { Amount: dryIceWeightRaw.replace('kgs', ''), Units: 'KG' };
    pkg.DryIceRegulationSet = (consigneeCountry === 'US') ? 1 : 2;
    pkg.DryIcePurpose = 2;
  });
}

// ---------------------------------------------------------------------------
// Reference concatenation (OSU)
// ---------------------------------------------------------------------------

/**
 * Builds shipper and consignee reference strings from MiscReference fields
 * and applies them to all packages along with notification emails.
 *
 * @param {object} shipmentRequest - The current shipment request.
 * @param {string} userId          - The current user's ID.
 */
function applyOsuReferences(shipmentRequest, userId) {
  var pkg0 = shipmentRequest.Packages[0];
  var refs = [1, 2, 3, 4, 5, 6].map(function (n) {
    return returnPropertyValue(pkg0['MiscReference' + n]);
  });

  var shipperRef   = refs.slice(0, 4).join('~');
  var consigneeRef = refs.slice(4, 6).join('~');

  shipmentRequest.Packages.forEach(function (pkg) {
    pkg.ShipperReference   = shipperRef;
    pkg.ConsigneeReference = consigneeRef;
    pkg.MiscReference20    = userId;
    pkg.ShipNotificationAddressEmail = '';
  });
}

/**
 * Returns the value unchanged if truthy, or an empty string if null/undefined.
 *
 * @param {*} value
 * @returns {string}
 */
function returnPropertyValue(value) {
  return value || '';
}
