/**
 * Commodity Splitting — Add/remove commodities from specific packages
 * and refresh the commodity table.
 */

/**
 * Adds a commodity to a specific package's CommodityContents array
 * and refreshes the UI.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {number}   packageIndex - The 0-based package index.
 * @param {object}   commodity - The commodity object to add.
 * @param {function} refreshFn - Function to refresh the commodity display.
 */
function addCommodityToPackage(shipmentRequest, packageIndex, commodity, refreshFn) {
  try {
    if (!shipmentRequest.Packages[packageIndex].CommodityContents) {
      shipmentRequest.Packages[packageIndex].CommodityContents = [];
    }
    shipmentRequest.Packages[packageIndex].CommodityContents.push(commodity);
    refreshFn(packageIndex);
  } catch (error) {
    Logger.Log({ Source: 'addCommodityToPackage()', Error: error });
  }
}

/**
 * Removes the last commodity from a specific package's CommodityContents
 * array and refreshes the UI.
 *
 * @param {object}   shipmentRequest - The current shipment request.
 * @param {number}   packageIndex - The 0-based package index.
 * @param {function} refreshFn - Function to refresh the commodity display.
 */
function removeCommodityFromPackage(shipmentRequest, packageIndex, refreshFn) {
  try {
    if (!shipmentRequest.Packages[packageIndex].CommodityContents) {
      shipmentRequest.Packages[packageIndex].CommodityContents = [];
    }
    shipmentRequest.Packages[packageIndex].CommodityContents.pop();
    refreshFn(packageIndex);
  } catch (error) {
    Logger.Log({ Source: 'removeCommodityFromPackage()', Error: error });
  }
}
