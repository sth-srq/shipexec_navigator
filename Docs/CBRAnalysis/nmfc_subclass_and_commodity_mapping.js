/**
 * NMFC Sub-class and Commodity Mapping — Builds commodity content objects
 * for international FedEx shipments, deduplicating by description and
 * combining quantities.
 *
 * Used by Gem Group for consolidating commodity lines across packages
 * before PreShip on international ShipConsole shipments.
 */

/**
 * Consolidates commodity contents across all packages for an international
 * FedEx shipment. Duplicate descriptions have their quantities summed.
 * The consolidated list is placed on the first package.
 *
 * @param {object} shipmentRequest - The current shipment request.
 */
function consolidateInternationalCommodities(shipmentRequest) {
  var isInternational = shipmentRequest.PackageDefaults.Consignee.Country !== 'US';
  if (!isInternational) return;

  var descriptionIndex = new Map();
  var consolidated = [];

  shipmentRequest.Packages.forEach(function (pkg) {
    if (!pkg.CommodityContents) return;

    pkg.CommodityContents.forEach(function (commodity) {
      if (descriptionIndex.has(commodity.Description)) {
        var existing = consolidated[descriptionIndex.get(commodity.Description)];
        existing.Quantity = String(parseInt(existing.Quantity) + parseInt(commodity.Quantity));
      } else {
        descriptionIndex.set(commodity.Description, consolidated.length);
        consolidated.push(buildCommodityForExport(commodity));
      }
    });

    pkg.CommodityContents = [];
  });

  shipmentRequest.Packages[0].CommodityContents = consolidated;
}

/**
 * Adds a commodity to a specific package's CommodityContents array.
 * Supports both table-row cell arrays and structured bundle objects.
 *
 * @param {object}  vm        - The view model (to access currentShipment).
 * @param {object}  cellsOrBundle - Table row cells array or bundle data object.
 * @param {number}  packageIndex  - The 0-based package index.
 * @param {boolean} isBundle      - True if cellsOrBundle is a bundle object.
 */
function addToCommodityList(vm, cellsOrBundle, packageIndex, isBundle) {
  if (!cellsOrBundle || cellsOrBundle === '') {
    console.log('Error: Commodity data unavailable');
    return;
  }

  var commodity;

  if (isBundle) {
    var price = (cellsOrBundle.InternationalInfo && cellsOrBundle.InternationalInfo.ExportUnitPrice)
      ? cellsOrBundle.InternationalInfo.ExportUnitPrice
      : cellsOrBundle.SaleUnitPrice;

    commodity = {
      UnitValue:        { Currency: cellsOrBundle.InternationalInfo.ExportCurrencyCode, Amount: price },
      UnitWeight:       { Units: 'LB', Amount: cellsOrBundle.UnitWeight },
      QuantityUnitMeasure: 'PC',
      OriginCountry:    cellsOrBundle.InternationalInfo.CountryOfOrigin,
      ProductCode:      cellsOrBundle.KitComponentPart,
      HarmonizedCode:   cellsOrBundle.InternationalInfo.ExportTariffCode,
      Quantity:         cellsOrBundle.UnitsPerAssembly,
      Description:      cellsOrBundle.KitComponentPartDesc,
      packageIndex:     packageIndex
    };
  } else {
    var cells = cellsOrBundle;
    var price = (cells[17].textContent || cells[17].textContent !== '')
      ? cells[17].textContent
      : cells[4].textContent;

    commodity = {
      UnitValue:        { Currency: cells[15].textContent, Amount: price },
      UnitWeight:       { Units: 'LB', Amount: cells[7].textContent },
      QuantityUnitMeasure: 'PC',
      OriginCountry:    cells[14].textContent,
      ProductCode:      cells[1].textContent,
      HarmonizedCode:   cells[16].textContent,
      Quantity:         cells[10].textContent,
      Description:      cells[5].textContent,
      packageIndex:     packageIndex
    };
  }

  var existing = vm.currentShipment.Packages[packageIndex].CommodityContents || [];
  existing.push(commodity);
  vm.currentShipment.Packages[packageIndex].CommodityContents = existing;
}

// ---------------------------------------------------------------------------
// Internal helper
// ---------------------------------------------------------------------------

function buildCommodityForExport(source) {
  return {
    UnitValue:                  source.UnitValue,
    UnitWeight:                 source.UnitWeight,
    QuantityUnitMeasure:        'PC',
    ExportQuantityUnitMeasure1: 'PC',
    ExportQuantityUnitMeasure2: 'PC',
    LicenseUnitValue:           { Currency: 'USD' },
    DdtcUnitMeasure:            'PC',
    OriginCountry:              source.OriginCountry,
    ProductCode:                source.ProductCode,
    HarmonizedCode:             source.HarmonizedCode,
    Quantity:                   source.Quantity,
    Description:                source.Description,
    LicenseExpirationDate:      null,
    NaftaRvcAvgStartDate:       null,
    nAFTARVCAvgEndDate:         null
  };
}
