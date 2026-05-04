function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        if (location && location.toString().toLowerCase().indexOf('shipping') === -1) {
        if (this.vm && this.vm.userContext) {
        var shippingUrl = '/shipping';
        if (window && window.location && window.location.pathname.toLowerCase().indexOf('/shipping') === -1) {
        window.location.href = shippingUrl;
        }
        }
        return;
        }
        
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest || !vm.shipmentRequest.PackageDefaults) return;
        
        var pkg = vm.shipmentRequest.PackageDefaults;
        var consignee = pkg.Consignee || {};
        var country = (consignee.Country || '').toString().trim().toUpperCase();
        var pickupBtnVisible = country !== 'CA' && country !== 'CANADA';
        
        vm.showPickupButton = pickupBtnVisible;
        vm.hidePickupButton = !pickupBtnVisible;
        
        if (pkg.ConsigneeReference && pkg.ConsigneeReference.toString().trim().toLowerCase() === 'frozen') {
        vm.showDryIceWeight = true;
        } else {
        vm.showDryIceWeight = false;
        }
        
    }

    this.NewShipment = function(shipmentRequest) {
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest || !vm.shipmentRequest.PackageDefaults) return;
        
        var pkg = vm.shipmentRequest.PackageDefaults;
        var user = vm.profile && vm.profile.UserInformation ? vm.profile.UserInformation : {};
        
        pkg.Consignee = pkg.Consignee || {};
        pkg.Shipper = pkg.Shipper || {};
        
        pkg.Consignee.Company = user.Company || pkg.Consignee.Company || '';
        pkg.Consignee.Contact = user.Name || user.FullName || pkg.Consignee.Contact || '';
        pkg.Consignee.Address1 = user.Address1 || pkg.Consignee.Address1 || '';
        pkg.Consignee.Address2 = user.Address2 || pkg.Consignee.Address2 || '';
        pkg.Consignee.Address3 = user.Address3 || pkg.Consignee.Address3 || '';
        pkg.Consignee.City = user.City || pkg.Consignee.City || '';
        pkg.Consignee.StateProvince = user.StateProvince || pkg.Consignee.StateProvince || '';
        pkg.Consignee.PostalCode = user.PostalCode || pkg.Consignee.PostalCode || '';
        pkg.Consignee.Country = user.Country || pkg.Consignee.Country || '';
        pkg.Consignee.Phone = user.Phone || pkg.Consignee.Phone || '';
        
        pkg.ShipperReference = client.getValueByKey('Custom2', user.CustomData) || pkg.ShipperReference || '';
        pkg.MiscReference1 = client.getValueByKey('Custom1', user.CustomData) || pkg.MiscReference1 || '';
        pkg.MiscReference2 = client.getValueByKey('Custom3', user.CustomData) || pkg.MiscReference2 || '';
        pkg.MiscReference4 = true;
        
        if (!pkg.Description) pkg.Description = 'UN3373 Category B Human Sample';
        if (!pkg.DryIceWeightUnits) pkg.DryIceWeightUnits = 'KG';
        if (!pkg.ReturnDelivery) pkg.ReturnDelivery = true;
        if (!pkg.SaturdayDelivery) pkg.SaturdayDelivery = true;
        if (!pkg.Service) pkg.Service = 'UPS Express';
        if (!pkg.Terms) pkg.Terms = 'Prepaid';
        if (!pkg.WeightUnit) pkg.WeightUnit = 'KG';
        
        var pickupCountry = (pkg.Consignee.Country || '').toString().trim().toUpperCase();
        var isCanada = pickupCountry === 'CA' || pickupCountry === 'CANADA';
        vm.showPickupButton = !isCanada;
        vm.hidePickupButton = isCanada;
        vm.showDryIceWeight = (pkg.ConsigneeReference || '').toString().trim().toLowerCase() === 'frozen';
        
    }

    this.PreShip = function(shipmentRequest, userParams) {
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest || !vm.shipmentRequest.PackageDefaults) return;
        
        var pkg = vm.shipmentRequest.PackageDefaults;
        var consignee = pkg.Consignee || {};
        var country = (consignee.Country || '').toString().trim().toUpperCase();
        var isCanada = country === 'CA' || country === 'CANADA';
        
        if (!isCanada) {
        vm.showPickupButton = true;
        try {
        if (typeof vm.clickPickupRequest === 'function') {
        vm.clickPickupRequest();
        } else if (typeof vm.pickupRequest === 'function') {
        vm.pickupRequest();
        }
        if (typeof vm.save === 'function') {
        vm.save();
        }
        } catch (e) {
        client.alert.Danger('Pickup association could not be completed on the client. The server will attempt a fallback during shipping.');
        }
        } else {
        vm.showPickupButton = false;
        vm.hidePickupButton = true;
        }
        
        var temp = (pkg.ConsigneeReference || '').toString().trim().toLowerCase();
        if (temp === 'frozen') {
        var dryIce = pkg.MiscReference3;
        if (dryIce === null || dryIce === undefined || dryIce === '') {
        client.alert.Danger('Dry Ice Weight is required when Temperature is Frozen.');
        throw new Error('Dry Ice Weight is required when Temperature is Frozen.');
        }
        } else {
        pkg.MiscReference3 = '';
        vm.showDryIceWeight = false;
        }
        
    }


null
}
