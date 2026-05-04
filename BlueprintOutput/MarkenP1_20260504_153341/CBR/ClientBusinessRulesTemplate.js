function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        if (location == null)
        return;
        
        if (location.ToLowerInvariant().IndexOf("shipping") < 0)
        {
        if (this.vm != null && this.vm.navigate != null)
        this.vm.navigate("/shipping");
        }
    }

    this.NewShipment = function(shipmentRequest) {
        if (this.vm == null || this.vm.shipmentRequest == null)
        return;
        
        var sr = this.vm.shipmentRequest;
        if (sr.PackageDefaults == null)
        sr.PackageDefaults = {};
        
        if (this.vm.profile != null && this.vm.profile.UserInformation != null)
        {
        var ui = this.vm.profile.UserInformation;
        
        if (sr.PackageDefaults.Consignee == null)
        sr.PackageDefaults.Consignee = {};
        
        sr.PackageDefaults.Consignee.Company = ui.Address1 || sr.PackageDefaults.Consignee.Company || "";
        sr.PackageDefaults.Consignee.Address1 = ui.Address1 || sr.PackageDefaults.Consignee.Address1 || "";
        sr.PackageDefaults.Consignee.Address2 = ui.Address2 || sr.PackageDefaults.Consignee.Address2 || "";
        sr.PackageDefaults.Consignee.City = ui.City || sr.PackageDefaults.Consignee.City || "";
        sr.PackageDefaults.Consignee.StateProvince = ui.StateProvince || sr.PackageDefaults.Consignee.StateProvince || "";
        sr.PackageDefaults.Consignee.PostalCode = ui.PostalCode || sr.PackageDefaults.Consignee.PostalCode || "";
        sr.PackageDefaults.Consignee.Country = ui.Country || sr.PackageDefaults.Consignee.Country || "";
        sr.PackageDefaults.Consignee.Phone = ui.Phone || sr.PackageDefaults.Consignee.Phone || "";
        
        sr.PackageDefaults.ShipperReference = ui.Custom2 || sr.PackageDefaults.ShipperReference || "";
        sr.PackageDefaults.MiscReference1 = ui.Custom1 || sr.PackageDefaults.MiscReference1 || "";
        sr.PackageDefaults.MiscReference2 = ui.Custom3 || sr.PackageDefaults.MiscReference2 || "";
        }
        
        sr.PackageDefaults.Description = sr.PackageDefaults.Description || "UN3373 Category B Human Sample";
        sr.PackageDefaults.Service = sr.PackageDefaults.Service || "UPS Express";
        sr.PackageDefaults.Terms = sr.PackageDefaults.Terms || "Prepaid";
        sr.PackageDefaults.DryIceWeightUnits = sr.PackageDefaults.DryIceWeightUnits || "KG";
        sr.PackageDefaults.ReturnDelivery = true;
        sr.PackageDefaults.SaturdayDelivery = true;
    }

    this.PreShip = function(shipmentRequest, userParams) {
        if (this.vm == null || this.vm.shipmentRequest == null)
        return;
        
        var sr = this.vm.shipmentRequest;
        var consigneeCountry = sr.PackageDefaults && sr.PackageDefaults.Consignee ? sr.PackageDefaults.Consignee.Country : null;
        var shipperCountry = sr.PackageDefaults && sr.PackageDefaults.Shipper ? sr.PackageDefaults.Shipper.Country : null;
        
        if (consigneeCountry != null && shipperCountry != null && String(consigneeCountry).toUpperCase() !== String(shipperCountry).toUpperCase())
        {
        if (this.vm.pickup && typeof this.vm.pickup === "function")
        {
        this.vm.pickup();
        }
        }
        
        if (sr.PackageDefaults && String(sr.PackageDefaults.ConsigneeReference || "").toLowerCase() === "frozen" && !sr.PackageDefaults.MiscReference3)
        {
        var dryIce = window.prompt("Enter Dry Ice Weight (kg):", "");
        if (dryIce === null || dryIce === "")
        throw "Dry Ice Weight is required when Temperature is Frozen.";
        
        sr.PackageDefaults.MiscReference3 = dryIce;
        }
        
        if (sr.PackageDefaults && String(sr.PackageDefaults.ConsigneeReference || "").toLowerCase() !== "frozen")
        sr.PackageDefaults.MiscReference3 = null;
    }


The blueprint calls for existing shipping-page CBR hooks rather than new template-specific JavaScript. No additional CBR is strictly required beyond implementing or refining PageLoaded, NewShipment, and PreShip for the biological returns workflow. If the current controller does not already expose vm.pickup, vm.biologicalSampleChanged, or vm.initShippingTemplate, those helpers should be added in the shipping-page CBR, but there is no new standalone CBR file required specifically for template interactions.
}
