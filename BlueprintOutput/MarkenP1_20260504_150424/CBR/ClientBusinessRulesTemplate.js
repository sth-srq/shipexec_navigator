function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        this.PageLoaded = function (location) {
        if (!location) return;
        
        if (location.toLowerCase().indexOf('/shipping') >= 0 || location === '/' || location.toLowerCase().indexOf('shipping') >= 0) {
        window.setTimeout(function () {
        try {
        if (window.location.hash && window.location.hash.toLowerCase().indexOf('shipping') < 0) {
        window.location.hash = '#/shipping';
        }
        else if (location !== '/shipping') {
        window.location.href = '#/shipping';
        }
        } catch (e) { }
        }, 0);
        }
        
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest || !vm.shipmentRequest.PackageDefaults) return;
        
        var pd = vm.shipmentRequest.PackageDefaults;
        var temperature = pd.ConsigneeReference;
        
        if (temperature) {
        this._applyTemperatureRules(temperature);
        }
        
        this._syncPickupButtonVisibility();
        };
        
        this._applyTemperatureRules = function (temperature) {
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest || !vm.shipmentRequest.Packages || !vm.shipmentRequest.Packages.length) return;
        
        var pkg = vm.shipmentRequest.Packages[vm.packageIndex || 0];
        if (!pkg) return;
        
        var weightByTemp = {
        'ambient': 3,
        'frozen': 6,
        'refrigerated': 5,
        'ambient/refrigerated combo box': 6
        };
        
        var key = (temperature || '').toString().trim().toLowerCase();
        if (weightByTemp.hasOwnProperty(key)) {
        pkg.Weight = pkg.Weight || {};
        pkg.Weight.Amount = weightByTemp[key];
        }
        
        var dryIceEditable = key === 'frozen';
        if (!dryIceEditable) {
        if (vm.shipmentRequest.PackageDefaults) {
        vm.shipmentRequest.PackageDefaults.MiscReference3 = null;
        }
        }
        
        vm.dryIceEditable = dryIceEditable;
        };
        
        this._syncPickupButtonVisibility = function () {
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest || !vm.shipmentRequest.PackageDefaults) return;
        
        var consigneeCountry = (vm.shipmentRequest.PackageDefaults.Consignee && vm.shipmentRequest.PackageDefaults.Consignee.Country) || '';
        vm.hidePickupButton = consigneeCountry.toString().trim().toUpperCase() === 'CA';
        };
    }

    this.NewShipment = function(shipmentRequest) {
        this.NewShipment = function (shipmentRequest) {
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest;
        if (!shipmentRequest || !shipmentRequest.PackageDefaults) return;
        
        var vm = this.vm;
        var userInfo = vm && vm.profile && vm.profile.UserInformation ? vm.profile.UserInformation : null;
        
        if (userInfo) {
        shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};
        shipmentRequest.PackageDefaults.Consignee.Company = userInfo.Company || shipmentRequest.PackageDefaults.Consignee.Company;
        shipmentRequest.PackageDefaults.Consignee.Contact = userInfo.Name || userInfo.UserName || shipmentRequest.PackageDefaults.Consignee.Contact;
        shipmentRequest.PackageDefaults.Consignee.Address1 = userInfo.Address1 || shipmentRequest.PackageDefaults.Consignee.Address1;
        shipmentRequest.PackageDefaults.Consignee.Address2 = userInfo.Address2 || shipmentRequest.PackageDefaults.Consignee.Address2;
        shipmentRequest.PackageDefaults.Consignee.City = userInfo.City || shipmentRequest.PackageDefaults.Consignee.City;
        shipmentRequest.PackageDefaults.Consignee.StateProvince = userInfo.StateProvince || shipmentRequest.PackageDefaults.Consignee.StateProvince;
        shipmentRequest.PackageDefaults.Consignee.PostalCode = userInfo.PostalCode || shipmentRequest.PackageDefaults.Consignee.PostalCode;
        shipmentRequest.PackageDefaults.Consignee.Country = userInfo.Country || shipmentRequest.PackageDefaults.Consignee.Country;
        
        shipmentRequest.PackageDefaults.ShipperReference = this._getCustomDataValue(userInfo.CustomData, 'Custom2') || shipmentRequest.PackageDefaults.ShipperReference;
        shipmentRequest.PackageDefaults.MiscReference1 = this._getCustomDataValue(userInfo.CustomData, 'Custom1') || shipmentRequest.PackageDefaults.MiscReference1;
        shipmentRequest.PackageDefaults.MiscReference2 = this._getCustomDataValue(userInfo.CustomData, 'Custom3') || shipmentRequest.PackageDefaults.MiscReference2;
        }
        
        shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample';
        shipmentRequest.PackageDefaults.ReturnDelivery = true;
        shipmentRequest.PackageDefaults.SaturdayDelivery = true;
        shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
        shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
        shipmentRequest.PackageDefaults.WeightUnit = shipmentRequest.PackageDefaults.WeightUnit || 'KG';
        
        this._syncPickupButtonVisibility();
        };
        
        this._getCustomDataValue = function (customData, key) {
        if (!customData || !key) return null;
        for (var i = 0; i < customData.length; i++) {
        var item = customData[i];
        if (item && ((item.Key && item.Key.toString().toLowerCase() === key.toString().toLowerCase()) || (item.key && item.key.toString().toLowerCase() === key.toString().toLowerCase()))) {
        return item.Value || item.value || null;
        }
        }
        return null;
        };
    }

    this.PreShip = function(shipmentRequest, userParams) {
        this.PreShip = function (shipmentRequest) {
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest;
        if (!shipmentRequest || !shipmentRequest.PackageDefaults) return shipmentRequest;
        
        var pd = shipmentRequest.PackageDefaults;
        var temperature = (pd.ConsigneeReference || '').toString().trim().toLowerCase();
        
        if (temperature === 'frozen' && pd.MiscReference3 == null) {
        var val = window.prompt('Enter Dry Ice Weight (kg):', pd.MiscReference3 || '');
        if (val === null) {
        throw new Error('Dry Ice Weight is required when Temperature is Frozen.');
        }
        pd.MiscReference3 = val;
        }
        
        if (temperature !== 'frozen') {
        pd.MiscReference3 = null;
        }
        
        var consigneeCountry = pd.Consignee && pd.Consignee.Country ? pd.Consignee.Country.toString().trim().toUpperCase() : '';
        if (consigneeCountry && consigneeCountry !== 'CA') {
        this._syncPickupButtonVisibility();
        }
        
        return shipmentRequest;
        };
    }


// ClientBusinessRules additions for the biological returns workflow
this.PageLoaded = function (location) {
    if (location === '/shipping') {
        if (this.vm && this.vm.newShipment) {
            // optional: auto-route to shipping page handled by app shell; keep this hook for initialization
        }
    }
};

this.NewShipment = function (shipmentRequest) {
    if (!shipmentRequest || !shipmentRequest.PackageDefaults) return;

    // Populate defaults from user profile/custom values when available
    var user = this.vm && this.vm.profile && this.vm.profile.UserInformation ? this.vm.profile.UserInformation : null;
    if (user) {
        shipmentRequest.PackageDefaults.Consignee = user.Address || shipmentRequest.PackageDefaults.Consignee;
        shipmentRequest.PackageDefaults.ShipperReference = user.Custom2 || shipmentRequest.PackageDefaults.ShipperReference;
        shipmentRequest.PackageDefaults.MiscReference1 = user.Custom1 || shipmentRequest.PackageDefaults.MiscReference1;
        shipmentRequest.PackageDefaults.MiscReference2 = user.Custom3 || shipmentRequest.PackageDefaults.MiscReference2;
        shipmentRequest.PackageDefaults.MiscReference4 = true;
    }

    shipmentRequest.PackageDefaults.Description = 'UN3373 Category B Human Sample';
    shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
    shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
    shipmentRequest.PackageDefaults.ThirdPartyBilling = false;
};

this.PreShip = function (shipmentRequest, userParams) {
    if (!shipmentRequest || !shipmentRequest.PackageDefaults) return;

    var pd = shipmentRequest.PackageDefaults;

    // Set dry ice from MiscReference3 when frozen
    if (pd.ConsigneeReference === 'Frozen' && pd.MiscReference3) {
        var kg = parseFloat(pd.MiscReference3);
        if (!isNaN(kg)) {
            pd.DryIceWeight = kg;
        }
    }

    // Ensure biological sample flag is present
    if (pd.MiscReference4 === undefined || pd.MiscReference4 === null) {
        pd.MiscReference4 = true;
    }
};

this.Keystroke = function (shipmentRequest, vm, event) {
    if (!event || !event.key) return;
    if (event.key === 'F9' && vm && vm.profile && vm.profile.ProfileSetting && vm.profile.ProfileSetting.DisableServiceChanges) {
        event.preventDefault();
    }
};
}
