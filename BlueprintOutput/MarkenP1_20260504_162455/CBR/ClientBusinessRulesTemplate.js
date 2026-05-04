function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        if (location && location.toLowerCase && location.toLowerCase().indexOf('/shipping') === -1) {
        if (client && client.config && client.config.ShipExecServiceUrl && this.thinClientAPIRequest) {
        // Redirect the user into the shipping workflow after login.
        window.location.href = '/shipping';
        }
        return;
        }
        
        var vm = this.vm || {};
        var sr = vm.shipmentRequest || {};
        var pd = sr.PackageDefaults || {};
        var consignee = pd.Consignee || {};
        var country = (consignee.Country || '').toString().trim().toUpperCase();
        var temp = (pd.ConsigneeReference || '').toString().trim();
        var isCanada = country === 'CA';
        
        // Hide Pickup button for Canada; keep it available otherwise.
        if (vm.controls && vm.controls.PickupButton) {
        vm.controls.PickupButton.visible = !isCanada;
        }
        
        // Dry ice is only editable when Frozen is selected.
        if (vm.controls && vm.controls.MiscReference3) {
        vm.controls.MiscReference3.enabled = temp === 'Frozen';
        }
        
        // Basic temperature-driven defaults for the visible UI field.
        if (temp === 'Ambient') {
        pd.WeightUnit = pd.WeightUnit || 'KG';
        } else if (temp === 'Frozen') {
        pd.WeightUnit = pd.WeightUnit || 'KG';
        } else if (temp === 'Refrigerated') {
        pd.WeightUnit = pd.WeightUnit || 'KG';
        } else if (temp === 'Ambient/Refrigerated Combo Box') {
        pd.WeightUnit = pd.WeightUnit || 'KG';
        }
        
        sr.PackageDefaults = pd;
        vm.shipmentRequest = sr;
    }

    this.NewShipment = function(shipmentRequest) {
        var vm = this.vm || {};
        var sr = shipmentRequest || vm.shipmentRequest || {};
        var pd = sr.PackageDefaults || {};
        var userInfo = (vm.profile && vm.profile.UserInformation) ? vm.profile.UserInformation : {};
        var customData = userInfo.CustomData || [];
        
        function getCustom(key) {
        return client.getValueByKey(key, customData);
        }
        
        // Default return label sender/pickup-from using the user's address.
        pd.Consignee = pd.Consignee || {};
        pd.Consignee.Company = userInfo.Company || pd.Consignee.Company || '';
        pd.Consignee.Contact = userInfo.Contact || pd.Consignee.Contact || userInfo.Name || '';
        pd.Consignee.Address1 = userInfo.Address1 || pd.Consignee.Address1 || '';
        pd.Consignee.Address2 = userInfo.Address2 || pd.Consignee.Address2 || '';
        pd.Consignee.City = userInfo.City || pd.Consignee.City || '';
        pd.Consignee.StateProvince = userInfo.StateProvince || pd.Consignee.StateProvince || '';
        pd.Consignee.PostalCode = userInfo.PostalCode || pd.Consignee.PostalCode || '';
        pd.Consignee.Country = userInfo.Country || pd.Consignee.Country || '';
        pd.Consignee.Phone = userInfo.Phone || pd.Consignee.Phone || '';
        
        // Default mapped reference fields from user custom data.
        pd.ShipperReference = getCustom('Custom2') || pd.ShipperReference || '';
        pd.MiscReference1 = getCustom('Custom1') || pd.MiscReference1 || '';
        pd.MiscReference2 = getCustom('Custom3') || pd.MiscReference2 || '';
        pd.MiscReference4 = (pd.MiscReference4 === undefined || pd.MiscReference4 === null || pd.MiscReference4 === '') ? true : pd.MiscReference4;
        
        // Default shipment values for the returns template.
        pd.Description = pd.Description || 'UN3373 Category B Human Sample';
        pd.Service = pd.Service || 'UPS Express';
        pd.Terms = pd.Terms || 'Prepaid';
        pd.DryIceWeightUnits = pd.DryIceWeightUnits || 'KG';
        pd.ReturnDelivery = (pd.ReturnDelivery === undefined || pd.ReturnDelivery === null) ? true : pd.ReturnDelivery;
        pd.SaturdayDelivery = (pd.SaturdayDelivery === undefined || pd.SaturdayDelivery === null) ? true : pd.SaturdayDelivery;
        pd.WeightUnit = pd.WeightUnit || 'KG';
        
        sr.PackageDefaults = pd;
        vm.shipmentRequest = sr;
        
        // Update UI state for pickup availability.
        var country = (pd.Consignee && pd.Consignee.Country ? pd.Consignee.Country : '').toString().trim().toUpperCase();
        if (vm.controls && vm.controls.PickupButton) {
        vm.controls.PickupButton.visible = country !== 'CA';
        }
    }

    this.PreShip = function(shipmentRequest, userParams) {
        var vm = this.vm || {};
        var sr = shipmentRequest || vm.shipmentRequest || {};
        var pd = sr.PackageDefaults || {};
        var consignee = pd.Consignee || {};
        var country = (consignee.Country || '').toString().trim().toUpperCase();
        
        // Ensure dry ice weight entered in the UI is preserved for SBR.
        if (pd.ConsigneeReference === 'Frozen') {
        if (pd.MiscReference3 === undefined || pd.MiscReference3 === null || pd.MiscReference3 === '') {
        throw new Error('Dry Ice Weight is required when Temperature is Frozen.');
        }
        }
        
        // If the pickup-from address is not Canada, attempt to trigger pickup association before ship.
        if (country !== 'CA' && vm.controls && vm.controls.PickupButton) {
        try {
        if (typeof vm.controls.PickupButton.click === 'function') {
        vm.controls.PickupButton.click();
        }
        } catch (e) {
        // Ignore client-side pickup automation errors; the server-side fallback can handle it.
        }
        }
        
        sr.PackageDefaults = pd;
        vm.shipmentRequest = sr;
    }


null
}
