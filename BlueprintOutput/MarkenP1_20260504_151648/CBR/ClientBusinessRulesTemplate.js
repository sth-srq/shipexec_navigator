function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        this.PageLoaded = function (location) {
        if (!location)
        return;
        
        var path = location.toString().toLowerCase();
        if (path.indexOf('/shipping') >= 0 || path === '/' || path.indexOf('login') >= 0) {
        var self = this;
        setTimeout(function () {
        if (self.vm && self.vm.route && typeof self.vm.route.go === 'function') {
        self.vm.route.go('/shipping');
        }
        }, 0);
        }
        };
    }

    this.NewShipment = function(shipmentRequest) {
        this.NewShipment = function (shipmentRequest) {
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest;
        if (!shipmentRequest)
        return shipmentRequest;
        
        var userInfo = (this.vm && this.vm.profile && this.vm.profile.UserInformation) ? this.vm.profile.UserInformation : {};
        shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
        shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};
        
        shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample';
        shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
        shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
        shipmentRequest.PackageDefaults.SaturdayDelivery = true;
        shipmentRequest.PackageDefaults.DryIceWeightUnits = shipmentRequest.PackageDefaults.DryIceWeightUnits || 'KG';
        shipmentRequest.PackageDefaults.ReturnDelivery = true;
        shipmentRequest.PackageDefaults.MiscReference4 = true;
        shipmentRequest.PackageDefaults.ShipperReference = userInfo.Custom2 || shipmentRequest.PackageDefaults.ShipperReference;
        shipmentRequest.PackageDefaults.MiscReference1 = userInfo.Custom1 || shipmentRequest.PackageDefaults.MiscReference1;
        shipmentRequest.PackageDefaults.MiscReference2 = userInfo.Custom3 || shipmentRequest.PackageDefaults.MiscReference2;
        
        return shipmentRequest;
        };
    }

    this.PreShip = function(shipmentRequest, userParams) {
        this.PreShip = function (shipmentRequest) {
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest;
        if (!shipmentRequest || !shipmentRequest.PackageDefaults || !shipmentRequest.PackageDefaults.Consignee)
        return shipmentRequest;
        
        var consigneeCountry = (shipmentRequest.PackageDefaults.Consignee.Country || '').toString().trim().toUpperCase();
        if (consigneeCountry && consigneeCountry !== 'CA') {
        var pickupButton = $('button, input, a').filter(function () {
        var text = ($(this).text() || $(this).val() || '').toString().toLowerCase();
        return text.indexOf('pickup') >= 0;
        }).first();
        
        if (pickupButton && pickupButton.length) {
        pickupButton.click();
        }
        }
        
        return shipmentRequest;
        };
    }


this.PageLoaded = function (location) {
    if (!location)
        return;

    var path = location.toString().toLowerCase();
    if (path.indexOf('/shipping') >= 0 || path === '/' || path.indexOf('login') >= 0) {
        var self = this;
        setTimeout(function () {
            if (self.vm && self.vm.route && typeof self.vm.route.go === 'function') {
                self.vm.route.go('/shipping');
            }
        }, 0);
    }
};

this.NewShipment = function (shipmentRequest) {
    shipmentRequest = shipmentRequest || this.vm.shipmentRequest;
    if (!shipmentRequest)
        return shipmentRequest;

    var userInfo = (this.vm && this.vm.profile && this.vm.profile.UserInformation) ? this.vm.profile.UserInformation : {};
    shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
    shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};

    shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample';
    shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
    shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
    shipmentRequest.PackageDefaults.SaturdayDelivery = true;
    shipmentRequest.PackageDefaults.DryIceWeightUnits = shipmentRequest.PackageDefaults.DryIceWeightUnits || 'KG';
    shipmentRequest.PackageDefaults.ReturnDelivery = true;
    shipmentRequest.PackageDefaults.MiscReference4 = true;
    shipmentRequest.PackageDefaults.ShipperReference = userInfo.Custom2 || shipmentRequest.PackageDefaults.ShipperReference;
    shipmentRequest.PackageDefaults.MiscReference1 = userInfo.Custom1 || shipmentRequest.PackageDefaults.MiscReference1;
    shipmentRequest.PackageDefaults.MiscReference2 = userInfo.Custom3 || shipmentRequest.PackageDefaults.MiscReference2;

    return shipmentRequest;
};

this.PreShip = function (shipmentRequest) {
    shipmentRequest = shipmentRequest || this.vm.shipmentRequest;
    if (!shipmentRequest || !shipmentRequest.PackageDefaults || !shipmentRequest.PackageDefaults.Consignee)
        return shipmentRequest;

    var consigneeCountry = (shipmentRequest.PackageDefaults.Consignee.Country || '').toString().trim().toUpperCase();
    if (consigneeCountry && consigneeCountry !== 'CA') {
        var pickupButton = $('button, input, a').filter(function () {
            var text = ($(this).text() || $(this).val() || '').toString().toLowerCase();
            return text.indexOf('pickup') >= 0;
        }).first();

        if (pickupButton && pickupButton.length) {
            pickupButton.click();
        }
    }

    return shipmentRequest;
};
}
