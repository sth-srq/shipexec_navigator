function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        if (!location)
        return;
        
        var route = location.toString().toLowerCase();
        if (route.indexOf("shipping") >= 0 || route === "/" || route.indexOf("login") >= 0)
        {
        if (window && window.location && route.indexOf("shipping") < 0)
        window.location.hash = "#/shipping";
        }
    }

    this.NewShipment = function(shipmentRequest) {
        if (!this.vm || !this.vm.shipmentRequest || !this.vm.profile || !this.vm.profile.UserInformation)
        return;
        
        var sr = this.vm.shipmentRequest;
        var user = this.vm.profile.UserInformation;
        
        sr.PackageDefaults = sr.PackageDefaults || {};
        sr.PackageDefaults.Consignee = sr.PackageDefaults.Consignee || {};
        
        var consignee = sr.PackageDefaults.Consignee;
        consignee.Company = user.Address1 || consignee.Company;
        consignee.Contact = user.FullName || consignee.Contact;
        consignee.Address1 = user.Address1 || consignee.Address1;
        consignee.Address2 = user.Address2 || consignee.Address2;
        consignee.City = user.City || consignee.City;
        consignee.StateProvince = user.State || consignee.StateProvince;
        consignee.PostalCode = user.PostalCode || consignee.PostalCode;
        consignee.Country = user.Country || consignee.Country;
        consignee.Phone = user.Phone || consignee.Phone;
        
        sr.PackageDefaults.ShipperReference = user.Custom2 || sr.PackageDefaults.ShipperReference;
        sr.PackageDefaults.MiscReference1 = user.Custom1 || sr.PackageDefaults.MiscReference1;
        sr.PackageDefaults.MiscReference2 = user.Custom3 || sr.PackageDefaults.MiscReference2;
        sr.PackageDefaults.MiscReference4 = true;
        
        if (sr.Packages && sr.Packages.length > 0 && (sr.Packages[0].Weight === null || sr.Packages[0].Weight === undefined))
        sr.Packages[0].Weight = 3;
    }

    this.PreShip = function(shipmentRequest, userParams) {
        if (!this.vm || !this.vm.shipmentRequest || !this.vm.shipmentRequest.PackageDefaults)
        return;
        
        var sr = this.vm.shipmentRequest;
        var pd = sr.PackageDefaults;
        var temp = pd.ConsigneeReference || "";
        
        if (temp === "Frozen" && (pd.MiscReference3 === null || pd.MiscReference3 === undefined || pd.MiscReference3 === ""))
        {
        client.alert.Danger("Dry Ice Weight is required when Temperature is Frozen.");
        throw new Error("Dry Ice Weight is required when Temperature is Frozen.");
        }
        
        var country = "";
        if (pd.Consignee && pd.Consignee.Country)
        country = pd.Consignee.Country.toString().toUpperCase();
        
        if (country && country !== "CA")
        {
        if (this.vm && typeof this.vm.pickup === "function")
        {
        this.vm.pickup();
        }
        }
    }


Use the shipping-page CBR hooks already identified in the hooks analysis to implement the workflow behavior: PageLoaded for redirecting to shipping after login, NewShipment for seeding return-label defaults from the user profile, and PreShip for enforcing frozen dry-ice requirements and pickup association logic. No additional template-specific CBR beyond that is required.
}
