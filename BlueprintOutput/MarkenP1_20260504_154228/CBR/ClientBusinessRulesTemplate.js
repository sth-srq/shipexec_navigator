function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        if (location && location.toLowerCase().indexOf("/shipping") < 0)
        {
        window.location.href = "/shipping";
        }
        
    }

    this.NewShipment = function(shipmentRequest) {
        if (!this.vm || !this.vm.shipmentRequest) return;
        
        var sr = this.vm.shipmentRequest;
        sr.packageDefaults = sr.packageDefaults || {};
        sr.packageDefaults.description = sr.packageDefaults.description || "UN3373 Category B Human Sample";
        sr.packageDefaults.saturdayDelivery = true;
        sr.packageDefaults.service = sr.packageDefaults.service || "UPS Express";
        sr.packageDefaults.terms = sr.packageDefaults.terms || "Prepaid";
        sr.packageDefaults.weightUnit = sr.packageDefaults.weightUnit || "KG";
        
        var userInfo = this.vm.profile && this.vm.profile.UserInformation ? this.vm.profile.UserInformation : null;
        if (userInfo)
        {
        var custom1 = client.getValueByKey("Custom1", userInfo.CustomData || []);
        var custom2 = client.getValueByKey("Custom2", userInfo.CustomData || []);
        var custom3 = client.getValueByKey("Custom3", userInfo.CustomData || []);
        
        sr.packageDefaults.shipperReference = custom2 || sr.packageDefaults.shipperReference;
        sr.packageDefaults.miscReference1 = custom1 || sr.packageDefaults.miscReference1;
        sr.packageDefaults.miscReference2 = custom3 || sr.packageDefaults.miscReference2;
        
        if (userInfo.Address1)
        {
        sr.packageDefaults.consignee = sr.packageDefaults.consignee || {};
        sr.packageDefaults.consignee.company = userInfo.Company || sr.packageDefaults.consignee.company;
        sr.packageDefaults.consignee.address1 = userInfo.Address1 || sr.packageDefaults.consignee.address1;
        sr.packageDefaults.consignee.address2 = userInfo.Address2 || sr.packageDefaults.consignee.address2;
        sr.packageDefaults.consignee.city = userInfo.City || sr.packageDefaults.consignee.city;
        sr.packageDefaults.consignee.stateProvince = userInfo.State || sr.packageDefaults.consignee.stateProvince;
        sr.packageDefaults.consignee.postalCode = userInfo.PostalCode || sr.packageDefaults.consignee.postalCode;
        sr.packageDefaults.consignee.country = userInfo.Country || sr.packageDefaults.consignee.country;
        }
        }
        
        if (sr.packages && sr.packages.length > 0)
        {
        sr.packages[0].weight = sr.packages[0].weight || {};
        if (!sr.packages[0].weight.amount) sr.packages[0].weight.amount = 0;
        }
        
    }

    this.PreShip = function(shipmentRequest, userParams) {
        if (!this.vm || !this.vm.shipmentRequest) return;
        
        var sr = this.vm.shipmentRequest;
        var temperature = sr.packageDefaults ? sr.packageDefaults.consigneeReference : null;
        var weightMap = {
        "Ambient": 3,
        "Frozen": 6,
        "Refrigerated": 5,
        "Ambient/Refrigerated Combo Box": 6
        };
        
        if (temperature && weightMap.hasOwnProperty(temperature) && sr.packages && sr.packages.length > 0)
        {
        sr.packages[0].weight = sr.packages[0].weight || {};
        sr.packages[0].weight.amount = weightMap[temperature];
        }
        
        var consigneeCountry = sr.packageDefaults && sr.packageDefaults.consignee ? sr.packageDefaults.consignee.country : null;
        if (consigneeCountry && consigneeCountry !== "CA")
        {
        if (typeof document !== "undefined")
        {
        var pickupBtn = document.querySelector("button[data-action='pickup'], button[title*='Pickup'], input[value*='Pickup']");
        if (pickupBtn && typeof pickupBtn.click === "function")
        pickupBtn.click();
        }
        }
        
    }

    this.PostLoad = function(loadValue, shipmentRequest) {
        if (!this.vm || !this.vm.shipmentRequest) return;
        
        var sr = this.vm.shipmentRequest;
        sr.packageDefaults = sr.packageDefaults || {};
        sr.packageDefaults.description = sr.packageDefaults.description || "UN3373 Category B Human Sample";
        sr.packageDefaults.saturdayDelivery = true;
        sr.packageDefaults.service = sr.packageDefaults.service || "UPS Express";
        sr.packageDefaults.terms = sr.packageDefaults.terms || "Prepaid";
        
    }


if (location && location.toLowerCase().indexOf('/shipping') >= 0 && this.vm && this.vm.newShipment) {
    this.vm.newShipment();
}

this.PageLoaded = function(location) {
    if (location && location.toLowerCase().indexOf('/shipping') >= 0) {
        if (window && window.location && window.location.pathname.toLowerCase().indexOf('/shipping') < 0) {
            window.location.href = '/shipping';
        }
    }
};

this.NewShipment = function(shipmentRequest) {
    if (!shipmentRequest || !shipmentRequest.PackageDefaults) return;
    shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample';
    shipmentRequest.PackageDefaults.SaturdayDelivery = true;
    shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
    shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
    shipmentRequest.PackageDefaults.WeightUnit = shipmentRequest.PackageDefaults.WeightUnit || 'KG';
};

this.PreShip = function(shipmentRequest, userParams) {
    if (!shipmentRequest || !shipmentRequest.PackageDefaults || !shipmentRequest.Packages || !shipmentRequest.Packages.length) return;

    var temperature = shipmentRequest.PackageDefaults.ConsigneeReference;
    var weightMap = { 'Ambient': 3, 'Frozen': 6, 'Refrigerated': 5, 'Ambient/Refrigerated Combo Box': 6 };
    if (temperature && weightMap.hasOwnProperty(temperature)) {
        shipmentRequest.Packages[0].Weight = shipmentRequest.Packages[0].Weight || {};
        shipmentRequest.Packages[0].Weight.Amount = weightMap[temperature];
    }

    var consigneeCountry = shipmentRequest.PackageDefaults.Consignee && shipmentRequest.PackageDefaults.Consignee.Country;
    if (consigneeCountry && consigneeCountry !== 'CA' && this.vm && this.vm.pickup) {
        this.vm.pickup();
    }
};

this.PostLoad = function(loadValue, shipmentRequest) {
    if (!shipmentRequest || !shipmentRequest.PackageDefaults) return;
    shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample';
    shipmentRequest.PackageDefaults.SaturdayDelivery = true;
    shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
    shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
};
}
