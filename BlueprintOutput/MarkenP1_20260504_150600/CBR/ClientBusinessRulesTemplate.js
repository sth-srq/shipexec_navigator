function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        this.PageLoaded = function (location) {
        if (location && location.toLowerCase().indexOf("shipping") >= 0) {
        var self = this;
        setTimeout(function () {
        if (window.location.pathname.toLowerCase().indexOf("shipping") < 0) {
        try {
        window.location.hash = "#/shipping";
        } catch (e) { }
        }
        }, 0);
        }
        };
    }

    this.NewShipment = function(shipmentRequest) {
        this.NewShipment = function (shipmentRequest) {
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest;
        if (!shipmentRequest || !this.vm || !this.vm.profile || !this.vm.profile.UserInformation)
        return;
        
        var u = this.vm.profile.UserInformation;
        
        if (shipmentRequest.PackageDefaults && shipmentRequest.PackageDefaults.Consignee) {
        shipmentRequest.PackageDefaults.Consignee.Company = u.Address1 || shipmentRequest.PackageDefaults.Consignee.Company;
        shipmentRequest.PackageDefaults.Consignee.Contact = u.Name || shipmentRequest.PackageDefaults.Consignee.Contact;
        shipmentRequest.PackageDefaults.Consignee.Address1 = u.Address1 || shipmentRequest.PackageDefaults.Consignee.Address1;
        shipmentRequest.PackageDefaults.Consignee.Address2 = u.Address2 || shipmentRequest.PackageDefaults.Consignee.Address2;
        shipmentRequest.PackageDefaults.Consignee.Address3 = u.Address3 || shipmentRequest.PackageDefaults.Consignee.Address3;
        shipmentRequest.PackageDefaults.Consignee.City = u.City || shipmentRequest.PackageDefaults.Consignee.City;
        shipmentRequest.PackageDefaults.Consignee.StateProvince = u.StateProvince || shipmentRequest.PackageDefaults.Consignee.StateProvince;
        shipmentRequest.PackageDefaults.Consignee.PostalCode = u.PostalCode || shipmentRequest.PackageDefaults.Consignee.PostalCode;
        shipmentRequest.PackageDefaults.Consignee.Country = u.Country || shipmentRequest.PackageDefaults.Consignee.Country;
        shipmentRequest.PackageDefaults.Consignee.Phone = u.Phone || shipmentRequest.PackageDefaults.Consignee.Phone;
        }
        
        shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
        shipmentRequest.PackageDefaults.ShipperReference = u.Custom2 || shipmentRequest.PackageDefaults.ShipperReference;
        shipmentRequest.PackageDefaults.MiscReference1 = u.Custom1 || shipmentRequest.PackageDefaults.MiscReference1;
        shipmentRequest.PackageDefaults.ShipDate = shipmentRequest.PackageDefaults.ShipDate || new Date();
        
        if (shipmentRequest.Packages && shipmentRequest.Packages.length > 0) {
        for (var i = 0; i < shipmentRequest.Packages.length; i++) {
        if (shipmentRequest.Packages[i])
        shipmentRequest.Packages[i].Weight = shipmentRequest.Packages[i].Weight || 0;
        }
        }
        };
    }

    this.PreShip = function(shipmentRequest, userParams) {
        this.PreShip = function (shipmentRequest) {
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest;
        if (!shipmentRequest)
        return;
        
        var consignee = shipmentRequest.PackageDefaults && shipmentRequest.PackageDefaults.Consignee ? shipmentRequest.PackageDefaults.Consignee : null;
        if (!consignee)
        return;
        
        var country = (consignee.Country || "").toString().trim().toUpperCase();
        var isCanada = country === "CA" || country === "CANADA";
        
        if (!isCanada) {
        try {
        var pickupBtn = document.querySelector('button[title*="Pickup"],button[id*="Pickup"],input[value*="Pickup"]');
        if (pickupBtn) {
        pickupBtn.click();
        setTimeout(function () {
        var saveBtn = document.querySelector('button[title*="Save"],button[id*="Save"],input[value*="Save"]');
        if (saveBtn) saveBtn.click();
        }, 250);
        }
        } catch (e) {
        client.alert.Danger("Unable to automatically associate Pickup. Please click Pickup Request and Save manually.");
        }
        }
        };
    }

    this.PostLoad = function(loadValue, shipmentRequest) {
        this.PostLoad = function (loadValue, shipmentRequest) {
        if (!shipmentRequest || !shipmentRequest.PackageDefaults || !shipmentRequest.PackageDefaults.Consignee)
        return;
        
        var country = (shipmentRequest.PackageDefaults.Consignee.Country || "").toString().trim().toUpperCase();
        var isCanada = country === "CA" || country === "CANADA";
        
        try {
        var pickupBtn = document.querySelector('button[title*="Pickup"],button[id*="Pickup"],input[value*="Pickup"]');
        if (pickupBtn) pickupBtn.style.display = isCanada ? "none" : "";
        } catch (e) { }
        };
    }

    this.PostShip = function(shipmentRequest, shipmentResponse) {
        this.PostShip = function (shipmentRequest, shipmentResponse) {
        this.vm.shipmentRequest = shipmentRequest;
        if (shipmentResponse && shipmentResponse.PackageDefaults && shipmentResponse.PackageDefaults.ErrorCode === 0) {
        client.alert.Success("Shipment processed successfully.");
        }
        };
    }


{ "PageLoaded": "Redirect to the shipping page on login; evaluate consignee/pickup country and hide or auto-click the Pickup button as required; ensure pickup-from behavior is applied for non-Canada addresses.", "NewShipment": "Populate Consignee from the user address values, set Shipper Reference from User Custom2, set MiscReference1 from User Custom1, default MiscReference4/Biological Sample to true, and apply other profile defaults.", "PreShip": "If Temperature/ConsigneeReference is Frozen, require/use MiscReference3 as Dry Ice Weight; convert KG to shipment dry ice values and package weight adjustment; for non-Frozen values, make Dry Ice Weight read-only or cleared; if pickup association cannot be completed client-side, trigger save/pickup logic as fallback.", "FieldMappings": ["Consignee tab caption -> Pickup From", "ConsigneeReference -> Temperature", "ShipperReference -> Study Reference Guide", "MiscReference1 -> Protocol Number", "MiscReference2 -> Site Number", "MiscReference3 -> Dry Ice Weight (kg)", "MiscReference4 -> Biological Sample"], "Notes": "Rate button hiding should be done in template or via FieldOptions/DOM if the button is not already field-option driven." }
}
