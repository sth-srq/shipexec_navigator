function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        var vm = this.vm;
        if (!vm || !vm.shipmentRequest) {
        return;
        }
        
        var route = (location || '').toString().toLowerCase();
        if (route.indexOf('shipping') === -1) {
        if (vm.goToPage) {
        vm.goToPage('shipping');
        } else if (vm.navigateTo) {
        vm.navigateTo('shipping');
        }
        }
        
        var shipment = vm.shipmentRequest;
        var defaults = shipment.PackageDefaults || {};
        var consignee = defaults.Consignee || {};
        var consigneeCountry = (consignee.Country || '').toString().trim().toUpperCase();
        var isCanada = consigneeCountry === 'CA' || consigneeCountry === 'CAN' || consigneeCountry === 'CANADA';
        var tempValue = (defaults.ConsigneeReference || defaults.Temperature || '').toString().trim().toLowerCase();
        var isFrozen = tempValue === 'frozen';
        
        if (vm.setFieldCaption) {
        vm.setFieldCaption('Consignee', 'Pickup From');
        vm.setFieldCaption('ConsigneeReference', 'Temperature');
        vm.setFieldCaption('ShipperReference', 'Study Reference Guide');
        vm.setFieldCaption('MiscReference1', 'Protocol Number');
        vm.setFieldCaption('MiscReference2', 'Site Number');
        vm.setFieldCaption('MiscReference3', 'Dry Ice Weight (kg)');
        vm.setFieldCaption('MiscReference4', 'Biological Sample');
        }
        
        if (vm.setFieldVisible) {
        vm.setFieldVisible('RateButton', false);
        vm.setFieldVisible('PickupButton', !isCanada);
        vm.setFieldVisible('MiscReference3', isFrozen);
        }
        
        if (vm.setFieldDisabled) {
        vm.setFieldDisabled('MiscReference3', !isFrozen);
        }
        
        if (vm.setFieldValue) {
        if (!defaults.Description) {
        vm.setFieldValue('Description', 'UN3373 Category B Human Sample');
        }
        vm.setFieldValue('ReturnDelivery', true);
        vm.setFieldValue('SaturdayDelivery', true);
        vm.setFieldValue('BiologicalSample', true);
        vm.setFieldValue('WeightUnit', 'KG');
        }
        
        var tempWeight = 0;
        switch (tempValue) {
        case 'ambient':
        tempWeight = 3;
        break;
        case 'frozen':
        tempWeight = 6;
        break;
        case 'refrigerated':
        tempWeight = 5;
        break;
        case 'ambient/refrigerated combo box':
        tempWeight = 6;
        break;
        }
        
        if (tempWeight > 0 && shipment.Packages && shipment.Packages.length > 0 && shipment.Packages[0].Weight) {
        shipment.Packages[0].Weight.Amount = tempWeight;
        if (vm.setFieldValue) {
        vm.setFieldValue('Weight', tempWeight);
        }
        }
        
        if (!isCanada && vm.clickButton) {
        vm.clickButton('Pickup');
        }
    }

    this.NewShipment = function(shipmentRequest) {
        var vm = this.vm;
        if (!vm || !shipmentRequest) {
        return;
        }
        
        var profile = vm.profile || {};
        var userInfo = profile.UserInformation || {};
        var shipment = shipmentRequest;
        var defaults = shipment.PackageDefaults || {};
        var userAddress = userInfo.Address || userInfo.UserAddress || userInfo.ShipperAddress || null;
        
        if (!defaults.Consignee) {
        defaults.Consignee = {};
        }
        
        if (userAddress && typeof userAddress === 'object') {
        defaults.Consignee.Company = userAddress.Company || defaults.Consignee.Company;
        defaults.Consignee.Contact = userAddress.Contact || defaults.Consignee.Contact;
        defaults.Consignee.Address1 = userAddress.Address1 || defaults.Consignee.Address1;
        defaults.Consignee.Address2 = userAddress.Address2 || defaults.Consignee.Address2;
        defaults.Consignee.Address3 = userAddress.Address3 || defaults.Consignee.Address3;
        defaults.Consignee.City = userAddress.City || defaults.Consignee.City;
        defaults.Consignee.StateProvince = userAddress.StateProvince || userAddress.State || defaults.Consignee.StateProvince;
        defaults.Consignee.PostalCode = userAddress.PostalCode || userAddress.Zip || defaults.Consignee.PostalCode;
        defaults.Consignee.Country = userAddress.Country || defaults.Consignee.Country;
        defaults.Consignee.Phone = userAddress.Phone || defaults.Consignee.Phone;
        }
        
        var customValue = function(key) {
        if (client && client.getValueByKey) {
        return client.getValueByKey(key, userInfo.CustomData || userInfo.CustomFields || []);
        }
        return userInfo[key] || '';
        };
        
        if (vm.setFieldValue) {
        vm.setFieldValue('ShipperReference', customValue('Custom2'));
        vm.setFieldValue('MiscReference1', customValue('Custom1'));
        vm.setFieldValue('MiscReference2', customValue('Custom3'));
        vm.setFieldValue('MiscReference4', true);
        vm.setFieldValue('Description', 'UN3373 Category B Human Sample');
        vm.setFieldValue('Service', 'UPS Express');
        vm.setFieldValue('Terms', 'Prepaid');
        vm.setFieldValue('ReturnDelivery', true);
        vm.setFieldValue('SaturdayDelivery', true);
        vm.setFieldValue('WeightUnit', 'KG');
        }
        
        shipment.PackageDefaults = defaults;
        
        if (shipment.Packages && shipment.Packages.length > 0 && shipment.Packages[0].Weight && !shipment.Packages[0].Weight.Amount) {
        shipment.Packages[0].Weight.Amount = 3;
        }
        
        if (defaults.ConsigneeReference && defaults.ConsigneeReference.toString().toLowerCase() === 'frozen' && vm.setFieldVisible) {
        vm.setFieldVisible('MiscReference3', true);
        if (vm.setFieldDisabled) {
        vm.setFieldDisabled('MiscReference3', false);
        }
        }
    }

    this.PreShip = function(shipmentRequest, userParams) {
        var vm = this.vm;
        if (!vm || !shipmentRequest) {
        return;
        }
        
        var shipment = shipmentRequest;
        var defaults = shipment.PackageDefaults || {};
        var consignee = defaults.Consignee || {};
        var shipper = defaults.Shipper || {};
        var consigneeCountry = (consignee.Country || '').toString().trim().toUpperCase();
        var shipperCountry = (shipper.Country || '').toString().trim().toUpperCase();
        var isCanada = consigneeCountry === 'CA' || consigneeCountry === 'CAN' || consigneeCountry === 'CANADA';
        var isFrozen = (defaults.ConsigneeReference || '').toString().trim().toLowerCase() === 'frozen';
        
        if (!isCanada && vm.clickButton) {
        vm.clickButton('Pickup Request');
        vm.clickButton('Save');
        }
        
        if (isFrozen && (!defaults.MiscReference3 || defaults.MiscReference3 === '')) {
        if (client && client.alert && client.alert.Danger) {
        client.alert.Danger('Dry Ice Weight is required when Temperature is Frozen.');
        }
        throw new Error('Dry Ice Weight is required when Temperature is Frozen.');
        }
        
        if (!defaults.Description && vm.setFieldValue) {
        vm.setFieldValue('Description', 'UN3373 Category B Human Sample');
        }
        
        if (shipperCountry && consigneeCountry && shipperCountry !== consigneeCountry && vm.setFieldValue) {
        vm.setFieldValue('ReturnDelivery', true);
        }
        
    }

}
