function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        if (location && location.toString().toLowerCase().indexOf('/shipping') === -1) {
        if (window && window.location) {
        window.location.hash = '#/shipping';
        }
        return;
        }
        
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest) {
        return;
        }
        
        var shipmentRequest = vm.shipmentRequest;
        var pkgDefaults = shipmentRequest.PackageDefaults || {};
        var consignee = pkgDefaults.Consignee || {};
        var country = (consignee.Country || '').toString().trim().toUpperCase();
        var isCanada = country === 'CA';
        
        vm.showPickupButton = !isCanada;
        vm.disableDryIceWeight = (pkgDefaults.ConsigneeReference || '').toString() !== 'Frozen';
        
        if (!isCanada && vm.actions && typeof vm.actions.pickup === 'function') {
        try {
        vm.actions.pickup();
        } catch (e) {
        if (client && client.alert && client.alert.Danger) {
        client.alert.Danger('Pickup association could not be triggered automatically.');
        }
        }
        }
    }

    this.NewShipment = function(shipmentRequest) {
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest) {
        return;
        }
        
        var shipmentRequest = vm.shipmentRequest;
        shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
        shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};
        shipmentRequest.PackageDefaults.Shipper = shipmentRequest.PackageDefaults.Shipper || {};
        
        var userInfo = (vm.profile && vm.profile.UserInformation) ? vm.profile.UserInformation : {};
        var customData = userInfo.CustomData || [];
        
        function getCustomValue(key) {
        return client && client.getValueByKey ? client.getValueByKey(key, customData) : '';
        }
        
        var pickupFrom = shipmentRequest.PackageDefaults.Consignee;
        pickupFrom.Company = userInfo.Company || pickupFrom.Company || '';
        pickupFrom.Contact = userInfo.Contact || pickupFrom.Contact || '';
        pickupFrom.Address1 = userInfo.Address1 || pickupFrom.Address1 || '';
        pickupFrom.Address2 = userInfo.Address2 || pickupFrom.Address2 || '';
        pickupFrom.Address3 = userInfo.Address3 || pickupFrom.Address3 || '';
        pickupFrom.City = userInfo.City || pickupFrom.City || '';
        pickupFrom.StateProvince = userInfo.StateProvince || pickupFrom.StateProvince || '';
        pickupFrom.PostalCode = userInfo.PostalCode || pickupFrom.PostalCode || '';
        pickupFrom.Country = userInfo.Country || pickupFrom.Country || '';
        pickupFrom.Phone = userInfo.Phone || pickupFrom.Phone || '';
        
        shipmentRequest.PackageDefaults.ShipperReference = getCustomValue('Custom2') || shipmentRequest.PackageDefaults.ShipperReference || '';
        shipmentRequest.PackageDefaults.MiscReference1 = getCustomValue('Custom1') || shipmentRequest.PackageDefaults.MiscReference1 || '';
        shipmentRequest.PackageDefaults.MiscReference2 = getCustomValue('Custom3') || shipmentRequest.PackageDefaults.MiscReference2 || '';
        shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample';
        shipmentRequest.PackageDefaults.ReturnDelivery = true;
        shipmentRequest.PackageDefaults.SaturdayDelivery = true;
        shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
        shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
        shipmentRequest.PackageDefaults.WeightUnit = shipmentRequest.PackageDefaults.WeightUnit || 'KG';
        
        var isCanada = (pickupFrom.Country || '').toString().trim().toUpperCase() === 'CA';
        vm.showPickupButton = !isCanada;
        vm.disableDryIceWeight = true;
        
        if (!isCanada && vm.actions && typeof vm.actions.pickup === 'function') {
        try {
        vm.actions.pickup();
        } catch (e) {
        if (client && client.alert && client.alert.Danger) {
        client.alert.Danger('Automatic pickup association was not completed.');
        }
        }
        }
    }

    this.Keystroke = function(shipmentRequest, vm, event) {
    }

    this.PreLoad = function(loadValue, shipmentRequest, userParams) {
    }

    this.PostLoad = function(loadValue, shipmentRequest) {
    }

    this.PreShip = function(shipmentRequest, userParams) {
    }

    this.PostShip = function(shipmentRequest, shipmentResponse) {
    }

    this.PreRate = function(shipmentRequest, userParams) {
    }

    this.PostRate = function(shipmentRequest, rateResults) {
    }

    this.PreVoid = function(pkg, userParams) {
    }

    this.PostVoid = function(pkg) {
    }

    this.PrePrint = function(document, localPort) {
    }

    this.PostPrint = function(document) {
    }

    this.PreBuildShipment = function(shipmentRequest) {
    }

    this.PostBuildShipment = function(shipmentRequest) {
    }

    this.RepeatShipment = function(currentShipment) {
    }

    this.PreProcessBatch = function(batchReference, actions, params, vm) {
    }

    this.PostProcessBatch = function(batchResponse, vm) {
    }

    this.PreSearchHistory = function(searchCriteria) {
    }

    this.PostSearchHistory = function(packages) {
    }

    this.PreCloseManifest = function(manifestItem, userParams) {
    }

    this.PostCloseManifest = function(manifestItem) {
    }

    this.PreTransmit = function(transmitItem, userParams) {
    }

    this.PostTransmit = function(transmitItem) {
    }

    this.PreCreateGroup = function(groupRequest, userParams) {
    }

    this.PostCreateGroup = function(groupRequest) {
    }

    this.PreModifyGroup = function(groupRequest, userParams) {
    }

    this.PostModifyGroup = function(groupRequest) {
    }

    this.PreCloseGroup = function(groupRequest, userParams) {
    }

    this.PostCloseGroup = function(groupRequest) {
    }

    this.AddPackage = function(shipmentRequest, packageIndex) {
    }

    this.CopyPackage = function(shipmentRequest, packageIndex) {
    }

    this.RemovePackage = function(shipmentRequest, packageIndex) {
    }

    this.PostSelectAddressBook = function(shipmentRequest, nameaddress) {
    }


null
}
