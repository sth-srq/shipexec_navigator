/**
 * Commodity Mapping — Build commodity content objects, display an
 * assign-goods slideout panel, and drag commodities between packages.
 */

/**
 * Creates a properly formatted commodity content object from raw order data.
 *
 * @param {object} data - Raw commodity data with skuId, quantity, unitPrice, weightLbs.
 * @returns {object} A CommodityContents-compatible object.
 */
function buildCommodityContentObject(data) {
  return {
    Quantity:             data.quantity,
    ProductCode:          data.skuId,
    QuantityUnitMeasure:  'EA',
    UnitValue:            { Currency: 'USD', Amount: +data.unitPrice },
    UnitWeight:           { Units: 'LB', Amount: +data.weightLbs },
    UniqueId:             data.UniqueId,
    PkgIndex:             data.PkgIndex || 1,
    PVTotalWeight:        +data.quantity * +data.weightLbs,
    PVTotalValue:         +data.quantity * +data.unitPrice
  };
}

/**
 * Shows the assign-goods slideout panel, populates it with commodity items,
 * and assigns all goods to the first package by default.
 *
 * @param {object}   context - The class instance with `packages`, `goods`, `viewModel`.
 * @param {object[]} rawGoods - Array of raw commodity data objects.
 */
function showAssignGoodsPane(context, rawGoods) {
  $('#listItemContainer li:gt(0)').remove();
  $('div.ui-tab-container').toggleClass('col-md-8 col-md-6')
    .siblings('div').toggleClass('col-md-4 col-md-6');

  context.packages = context.viewModel.currentShipment.Packages;
  context.goods    = rawGoods.sort(function (a, b) { return a.skuId - b.skuId; });

  $(context.goods).each(function (index, item) {
    var uniqueKey = 'e' + (Math.random() + 1).toString(36).substring(2);
    item.UniqueId = uniqueKey;

    var $item = $('#liClone').clone(true).data(item).attr('id', uniqueKey);
    $item.find('.goods-sku').html('SKU: <strong>' + item.skuId + '</strong>');
    $item.find('.goods-quantity').html('Quantity: <strong>' + item.quantity + '</strong>');
    $item.find('.goods-declared-value').html('Unit Cost: <strong>$' + item.unitPrice + '</strong>');

    $('#divAssignCommodities ul.list-group').append($item);

    var commodity = buildCommodityContentObject(item);
    context.packages[0].CommodityContents.push(commodity);

    var pkg = context.packages[0];
    pkg.Weight.Amount            = (pkg.Weight.Amount || 0) + commodity.PVTotalWeight;
    pkg.DeclaredValueAmount.Amount = (pkg.DeclaredValueAmount.Amount || 0) +
                                     Math.round(commodity.PVTotalValue * 100) / 100;
    $item.show();
  });

  $('#divAssignCommodities').show().animate({ left: 0 }, 400);
  $('.scan-input:visible').first().focus();
  $('#divAssignCommodities').find('[data-toggle="tooltip"]').tooltip();
}

/**
 * Moves a commodity from its current package to a destination package,
 * updating weights and declared values for both.
 *
 * @param {object}   context - The class instance with `packages`.
 * @param {jQuery}   $li - The list-item element representing the commodity.
 * @param {number}   destBox - 1-based destination package index.
 */
function moveGoodToBox(context, $li, destBox) {
  var data     = $li.data();
  var allGoods = context.packages.map(function (p) { return p.CommodityContents; }).flat();
  var item     = allGoods.find(function (cc) { return cc.UniqueId === data.UniqueId; });

  if (+item.PkgIndex === +destBox) {
    console.log('Item already in box.');
    return;
  }

  /* Remove from current box */
  var srcIndex = item.PkgIndex - 1;
  context.packages[srcIndex].CommodityContents =
    context.packages[srcIndex].CommodityContents.filter(function (p) { return p.UniqueId !== data.UniqueId; });

  /* Add to destination box */
  item.PkgIndex = destBox;
  (context.packages[destBox - 1].CommodityContents || []).push(item);

  $li.find('.pkg-index-text').fadeOut().promise().done(function () {
    $(this).text(destBox).fadeIn();
  });

  /* Recalculate weights and values */
  allGoods = context.packages.map(function (p) { return p.CommodityContents; }).flat();
  allGoods.sort(function (a, b) { return a.PkgIndex - b.PkgIndex; });

  $(allGoods).each(function (idx, cc) {
    var pkg = context.packages[cc.PkgIndex - 1];
    if (idx === 0 || cc.PkgIndex !== allGoods[idx - 1].PkgIndex) {
      pkg.Weight.Amount = 0;
      pkg.DeclaredValueAmount.Amount = 0;
    }
    pkg.Weight.Amount += +cc.PVTotalWeight;
    pkg.DeclaredValueAmount.Amount += +cc.PVTotalValue;
  });
}
