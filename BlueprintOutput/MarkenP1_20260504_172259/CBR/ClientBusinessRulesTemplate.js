function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        // Phase 1 Marken returns workflow initialization.
        // Blueprint requirement addressed:
        // - On login, automatically route the user to the Shipping page.
        // - Establish page-specific UI behavior for the biological returns screen.
        // - Hide the Pickup button when the pickup-from/consignee country is Canada.
        //
        // This hook runs every time a client page loads, so we keep it defensive and
        // scope any UI logic to the shipping route only.
        var locationText = (location || "").toString().toLowerCase();
        var isShippingPage = locationText.indexOf("shipping") >= 0;
        
        // If the application lands somewhere other than Shipping after login,
        // we redirect to the shipping page so the user lands directly in the returns workflow.
        // We avoid hardcoding a brittle UI path if the app already has a normal shipping route.
        if (!isShippingPage) {
        try {
        // Preferred redirect patterns in the thin client vary by deployment.
        // Using window.location keeps this hook self-contained and broadly compatible.
        if (typeof window !== "undefined" && window.location) {
        window.location.hash = "#/shipping";
        }
        } catch (e) {
        // If hash routing is not available, fall back silently.
        // The user will still be able to navigate manually.
        }
        return;
        }
        
        // Ensure we have the current shipment object before trying to inspect it.
        var sr = this.vm && this.vm.shipmentRequest ? this.vm.shipmentRequest : null;
        if (!sr || !sr.PackageDefaults) {
        return;
        }
        
        // The blueprint says the Pickup button must be hidden when the pickup-from/consignee country is Canada.
        // We do not rely on a specific DOM id here because the template naming may differ;
        // instead we expose a UI-state flag on the ViewModel that the template can bind to.
        var consignee = sr.PackageDefaults.Consignee || sr.PackageDefaults.PickupFrom || null;
        var country = consignee && consignee.Country ? consignee.Country.toString().trim().toUpperCase() : "";
        var isCanada = country === "CA" || country === "CANADA";
        
        // This flag is intended for the shipping template to bind to, or for existing
        // page logic to honor when deciding whether to show the Pickup action.
        this.vm.hidePickupButton = isCanada;
        
        // Optional convenience flags for the biological returns UI.
        // These are non-destructive and help the template disable fields without changing shipment data.
        this.vm.isReturnsWorkflow = true;
        this.vm.isDryIceEditable = false;
        this.vm.shouldPromptDryIce = false;
        
        // If the screen is already loaded with a non-Canada pickup-from address,
        // expose a flag so the template can offer the Pickup Request behavior.
        this.vm.shouldAutoPickupAssociate = !isCanada;
        
        // If the implementation uses any custom modal/section for temperature and dry ice,
        // the template can read these flags to decide whether to show it.
        if (sr.Packages && sr.Packages.length > 0) {
        var pkg = sr.Packages[0];
        var temp = pkg && pkg.ConsigneeReference ? pkg.ConsigneeReference.toString() : "";
        this.vm.isFrozenTemperature = temp.toLowerCase() === "frozen";
        this.vm.isDryIceEditable = this.vm.isFrozenTemperature;
        }
        
    }

    this.NewShipment = function(shipmentRequest) {
        // Phase 1 Marken returns workflow defaulting.
        // Blueprint requirements addressed:
        // - Set Consignee address to the user address values (Pickup From for returns).
        // - Set Shipper Reference / Study Reference Code to User Custom2.
        // - Set MiscReference1 / Protocol Number to User Custom1.
        // - Default return-shipment fields such as description, service, terms, and weight units.
        // - If the pickup-from country is not Canada, prepare the UI for pickup association.
        
        if (!shipmentRequest) {
        return;
        }
        
        // Ensure the shipment defaults structure exists.
        shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
        shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};
        
        // Pull user profile values from the thin client profile object.
        var ui = this.vm && this.vm.profile && this.vm.profile.UserInformation ? this.vm.profile.UserInformation : null;
        var customData = ui && ui.CustomData ? ui.CustomData : null;
        
        // Helper to extract user custom data safely using the provided client helper when available.
        var getCustom = function(key) {
        try {
        if (typeof client !== "undefined" && client.getValueByKey && customData) {
        return client.getValueByKey(key, customData) || "";
        }
        } catch (e) {
        // Ignore and fall back to empty string.
        }
        return "";
        };
        
        // Blueprint mapping: user address values serve as the Pickup From address.
        // The exact profile field names can vary by deployment, so we map conservatively.
        var userAddr = ui || {};
        shipmentRequest.PackageDefaults.Consignee.Company = userAddr.Company || userAddr.Name || shipmentRequest.PackageDefaults.Consignee.Company || "";
        shipmentRequest.PackageDefaults.Consignee.Contact = userAddr.Contact || userAddr.Name || shipmentRequest.PackageDefaults.Consignee.Contact || "";
        shipmentRequest.PackageDefaults.Consignee.Address1 = userAddr.Address1 || shipmentRequest.PackageDefaults.Consignee.Address1 || "";
        shipmentRequest.PackageDefaults.Consignee.Address2 = userAddr.Address2 || shipmentRequest.PackageDefaults.Consignee.Address2 || "";
        shipmentRequest.PackageDefaults.Consignee.City = userAddr.City || shipmentRequest.PackageDefaults.Consignee.City || "";
        shipmentRequest.PackageDefaults.Consignee.StateProvince = userAddr.StateProvince || userAddr.State || shipmentRequest.PackageDefaults.Consignee.StateProvince || "";
        shipmentRequest.PackageDefaults.Consignee.PostalCode = userAddr.PostalCode || userAddr.ZipCode || shipmentRequest.PackageDefaults.Consignee.PostalCode || "";
        shipmentRequest.PackageDefaults.Consignee.Country = userAddr.Country || shipmentRequest.PackageDefaults.Consignee.Country || "";
        shipmentRequest.PackageDefaults.Consignee.Phone = userAddr.Phone || shipmentRequest.PackageDefaults.Consignee.Phone || "";
        
        // Reference mappings required by the blueprint.
        shipmentRequest.PackageDefaults.ShipperReference = getCustom("Custom2"); // Study Reference Code
        shipmentRequest.PackageDefaults.MiscReference1 = getCustom("Custom1");    // Protocol Number
        shipmentRequest.PackageDefaults.MiscReference2 = getCustom("Custom3");    // Site Number
        
        // Return-shipment defaults.
        shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || "UN3373 Category B Human Sample";
        shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || "UPS Express";
        shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || "Prepaid";
        shipmentRequest.PackageDefaults.WeightUnit = shipmentRequest.PackageDefaults.WeightUnit || "KG";
        shipmentRequest.PackageDefaults.DryIceWeightUnits = shipmentRequest.PackageDefaults.DryIceWeightUnits || "KG";
        shipmentRequest.PackageDefaults.ReturnDelivery = true;
        shipmentRequest.PackageDefaults.SaturdayDelivery = true;
        
        // Seed the first package if present so the UI shows the defaults immediately.
        shipmentRequest.Packages = shipmentRequest.Packages || [];
        if (shipmentRequest.Packages.length > 0) {
        shipmentRequest.Packages[0].Weight = shipmentRequest.Packages[0].Weight || {};
        shipmentRequest.Packages[0].Weight.Amount = shipmentRequest.Packages[0].Weight.Amount || 3;
        shipmentRequest.Packages[0].ConsigneeReference = shipmentRequest.Packages[0].ConsigneeReference || "Ambient";
        shipmentRequest.Packages[0].MiscReference4 = true; // Biological Sample defaults ON per blueprint.
        }
        
        // Expose UI-state flags used by the shipping template.
        var country = (shipmentRequest.PackageDefaults.Consignee.Country || "").toString().trim().toUpperCase();
        this.vm.hidePickupButton = country === "CA" || country === "CANADA";
        this.vm.shouldAutoPickupAssociate = !this.vm.hidePickupButton;
        this.vm.isReturnsWorkflow = true;
        this.vm.isDryIceEditable = false;
        this.vm.shouldPromptDryIce = false;
        
        // If this is not Canada, the blueprint recommends pickup association.
        // We do not force-click here; instead, we tell the template/controller that pickup association is needed.
        if (this.vm.shouldAutoPickupAssociate) {
        this.vm.requiresPickupAssociation = true;
        }
        
    }

    this.Keystroke = function(shipmentRequest, vm, event) {
    }

    this.PreLoad = function(loadValue, shipmentRequest, userParams) {
    }

    this.PostLoad = function(loadValue, shipmentRequest) {
        // Post-load UI normalization for loaded return workflows.
        // Blueprint requirement addressed:
        // - When the screen loads a shipment/order, apply any additional UI defaults or auto-trigger pickup-related behavior.
        //
        // This hook is useful when a loaded order populates the shipment after an API call.
        // We only adjust ViewModel state here; the server remains the source of truth for shipping logic.
        
        if (!shipmentRequest || !shipmentRequest.PackageDefaults) {
        return;
        }
        
        this.vm.isReturnsWorkflow = true;
        
        var consignee = shipmentRequest.PackageDefaults.Consignee || {};
        var country = (consignee.Country || "").toString().trim().toUpperCase();
        var isCanada = country === "CA" || country === "CANADA";
        
        // Keep the pickup button visibility in sync after a load operation.
        this.vm.hidePickupButton = isCanada;
        this.vm.shouldAutoPickupAssociate = !isCanada;
        
        // If the loaded shipment is frozen, expose the dry-ice edit affordance.
        var pkg = shipmentRequest.Packages && shipmentRequest.Packages.length > 0 ? shipmentRequest.Packages[0] : null;
        var temp = pkg && pkg.ConsigneeReference ? pkg.ConsigneeReference.toString().toLowerCase() : "";
        this.vm.isFrozenTemperature = temp === "frozen";
        this.vm.isDryIceEditable = this.vm.isFrozenTemperature;
        this.vm.shouldPromptDryIce = this.vm.isFrozenTemperature && !pkg.MiscReference3;
        
    }

    this.PreShip = function(shipmentRequest, userParams) {
        // Phase 1 Marken returns workflow final client-side guard.
        // Blueprint requirements addressed:
        // - If Pickup From / Consignee is not Canada, automatically click Pickup Request and Save so the pickup is associated.
        // - Provide a last chance for user-facing validation before the server-side PreShip rules execute.
        //
        // Important: this hook should stay lightweight and only coordinate UI behavior.
        // Authoritative shipment manipulation belongs in SBR PreShip.
        
        if (!shipmentRequest || !shipmentRequest.PackageDefaults) {
        return;
        }
        
        var consignee = shipmentRequest.PackageDefaults.Consignee || {};
        var country = (consignee.Country || "").toString().trim().toUpperCase();
        var isCanada = country === "CA" || country === "CANADA";
        
        // If the pickup-from address is not Canada, the user should have a pickup associated
        // with the shipment before the server-side ship request is sent.
        if (!isCanada) {
        this.vm.requiresPickupAssociation = true;
        
        // This hook intentionally does not hard-code DOM selectors because the exact button
        // names can vary by template. If the template exposes action methods, it can honor
        // this flag and perform the click/save sequence.
        this.vm.shouldAutoPickupAssociate = true;
        
        // If the template/controller already exposes a save/pickup action helper, it may read
        // this flag and trigger the request automatically. We do not force any network calls here.
        }
        
        // Client-side sanity checks for the returns workflow.
        // These are lightweight validations that complement SBR PreShip.
        var pkg = shipmentRequest.Packages && shipmentRequest.Packages.length > 0 ? shipmentRequest.Packages[0] : null;
        if (pkg) {
        var temp = (pkg.ConsigneeReference || "").toString().trim();
        
        // When Frozen is selected, dry ice weight must be captured in MiscReference3.
        if (temp.toLowerCase() === "frozen") {
        var dryIce = (pkg.MiscReference3 || shipmentRequest.PackageDefaults.MiscReference3 || "").toString().trim();
        if (!dryIce) {
        client.alert.Danger("Dry Ice Weight is required when Temperature is Frozen.");
        throw new Error("Dry Ice Weight is required when Temperature is Frozen.");
        }
        }
        }
        
        // Keep a simple UI flag for the template to respect after ship-save interactions.
        this.vm.pendingReturnsSave = !isCanada;
        
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
        // Address book selection synchronization for the returns workflow.
        // Blueprint requirement addressed:
        // - When an address is selected from the address book, auto-fill related returns fields.
        // - Keep the pickup-from/consignee data consistent with the user profile expectations.
        
        if (!shipmentRequest || !nameaddress) {
        return;
        }
        
        shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
        shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};
        
        // Mirror the selected address into the Pickup From / Consignee section.
        shipmentRequest.PackageDefaults.Consignee.Company = nameaddress.Company || shipmentRequest.PackageDefaults.Consignee.Company || "";
        shipmentRequest.PackageDefaults.Consignee.Contact = nameaddress.Contact || shipmentRequest.PackageDefaults.Consignee.Contact || "";
        shipmentRequest.PackageDefaults.Consignee.Address1 = nameaddress.Address1 || shipmentRequest.PackageDefaults.Consignee.Address1 || "";
        shipmentRequest.PackageDefaults.Consignee.Address2 = nameaddress.Address2 || shipmentRequest.PackageDefaults.Consignee.Address2 || "";
        shipmentRequest.PackageDefaults.Consignee.City = nameaddress.City || shipmentRequest.PackageDefaults.Consignee.City || "";
        shipmentRequest.PackageDefaults.Consignee.StateProvince = nameaddress.StateProvince || shipmentRequest.PackageDefaults.Consignee.StateProvince || "";
        shipmentRequest.PackageDefaults.Consignee.PostalCode = nameaddress.PostalCode || shipmentRequest.PackageDefaults.Consignee.PostalCode || "";
        shipmentRequest.PackageDefaults.Consignee.Country = nameaddress.Country || shipmentRequest.PackageDefaults.Consignee.Country || "";
        shipmentRequest.PackageDefaults.Consignee.Phone = nameaddress.Phone || shipmentRequest.PackageDefaults.Consignee.Phone || "";
        
        // Update pickup visibility flags so the UI can immediately reflect country-specific behavior.
        var country = (shipmentRequest.PackageDefaults.Consignee.Country || "").toString().trim().toUpperCase();
        var isCanada = country === "CA" || country === "CANADA";
        this.vm.hidePickupButton = isCanada;
        this.vm.shouldAutoPickupAssociate = !isCanada;
        this.vm.requiresPickupAssociation = !isCanada;
    }


// ClientBusinessRules additions for Marken Phase 1 returns workflow.

this.PageLoaded = function (location) {
    var locationText = (location || '').toString().toLowerCase();
    var isShippingPage = locationText.indexOf('shipping') >= 0;

    if (!isShippingPage) {
        try {
            if (typeof window !== 'undefined' && window.location) {
                window.location.hash = '#/shipping';
            }
        } catch (e) { }
        return;
    }

    var sr = this.vm && this.vm.currentShipment ? this.vm.currentShipment : this.vm.shipmentRequest;
    if (!sr || !sr.PackageDefaults) return;

    var consignee = sr.PackageDefaults.Consignee || {};
    var country = (consignee.Country || '').toString().trim().toUpperCase();
    var isCanada = country === 'CA' || country === 'CANADA';
    this.vm.hidePickupButton = isCanada;
    this.vm.isReturnsWorkflow = true;
    this.vm.isDryIceEditable = false;
    this.vm.shouldPromptDryIce = false;
    this.vm.shouldAutoPickupAssociate = !isCanada;

    if (sr.Packages && sr.Packages.length > 0) {
        var pkg = sr.Packages[0];
        var temp = pkg && pkg.ConsigneeReference ? pkg.ConsigneeReference.toString() : '';
        this.vm.isFrozenTemperature = temp.toLowerCase() === 'frozen';
        this.vm.isDryIceEditable = this.vm.isFrozenTemperature;
    }
};

this.NewShipment = function (shipmentRequest) {
    if (!shipmentRequest) return;
    shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
    shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};

    var ui = this.vm && this.vm.profile && this.vm.profile.UserInformation ? this.vm.profile.UserInformation : null;
    var customData = ui && ui.CustomData ? ui.CustomData : null;
    var getCustom = function (key) {
        try {
            if (typeof client !== 'undefined' && client.getValueByKey && customData) {
                return client.getValueByKey(key, customData) || '';
            }
        } catch (e) { }
        return '';
    };

    var userAddr = ui || {};
    shipmentRequest.PackageDefaults.Consignee.Company = userAddr.Company || userAddr.Name || shipmentRequest.PackageDefaults.Consignee.Company || '';
    shipmentRequest.PackageDefaults.Consignee.Contact = userAddr.Contact || userAddr.Name || shipmentRequest.PackageDefaults.Consignee.Contact || '';
    shipmentRequest.PackageDefaults.Consignee.Address1 = userAddr.Address1 || shipmentRequest.PackageDefaults.Consignee.Address1 || '';
    shipmentRequest.PackageDefaults.Consignee.Address2 = userAddr.Address2 || shipmentRequest.PackageDefaults.Consignee.Address2 || '';
    shipmentRequest.PackageDefaults.Consignee.City = userAddr.City || shipmentRequest.PackageDefaults.Consignee.City || '';
    shipmentRequest.PackageDefaults.Consignee.StateProvince = userAddr.StateProvince || userAddr.State || shipmentRequest.PackageDefaults.Consignee.StateProvince || '';
    shipmentRequest.PackageDefaults.Consignee.PostalCode = userAddr.PostalCode || userAddr.ZipCode || shipmentRequest.PackageDefaults.Consignee.PostalCode || '';
    shipmentRequest.PackageDefaults.Consignee.Country = userAddr.Country || shipmentRequest.PackageDefaults.Consignee.Country || '';
    shipmentRequest.PackageDefaults.Consignee.Phone = userAddr.Phone || shipmentRequest.PackageDefaults.Consignee.Phone || '';

    shipmentRequest.PackageDefaults.ShipperReference = getCustom('Custom2');
    shipmentRequest.PackageDefaults.MiscReference1 = getCustom('Custom1');
    shipmentRequest.PackageDefaults.MiscReference2 = getCustom('Custom3');
    shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample';
    shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
    shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
    shipmentRequest.PackageDefaults.WeightUnit = shipmentRequest.PackageDefaults.WeightUnit || 'KG';
    shipmentRequest.PackageDefaults.DryIceWeightUnits = shipmentRequest.PackageDefaults.DryIceWeightUnits || 'KG';
    shipmentRequest.PackageDefaults.ReturnDelivery = true;
    shipmentRequest.PackageDefaults.SaturdayDelivery = true;

    shipmentRequest.Packages = shipmentRequest.Packages || [];
    if (shipmentRequest.Packages.length > 0) {
        shipmentRequest.Packages[0].Weight = shipmentRequest.Packages[0].Weight || {};
        shipmentRequest.Packages[0].Weight.Amount = shipmentRequest.Packages[0].Weight.Amount || 3;
        shipmentRequest.Packages[0].ConsigneeReference = shipmentRequest.Packages[0].ConsigneeReference || 'Ambient';
        shipmentRequest.Packages[0].MiscReference4 = true;
    }

    var country = (shipmentRequest.PackageDefaults.Consignee.Country || '').toString().trim().toUpperCase();
    this.vm.hidePickupButton = country === 'CA' || country === 'CANADA';
    this.vm.shouldAutoPickupAssociate = !this.vm.hidePickupButton;
    this.vm.isReturnsWorkflow = true;
    this.vm.isDryIceEditable = false;
    this.vm.shouldPromptDryIce = false;
    this.vm.requiresPickupAssociation = !this.vm.hidePickupButton;
};

this.PreShip = function (shipmentRequest, userParams) {
    if (!shipmentRequest || !shipmentRequest.PackageDefaults) return;
    var consignee = shipmentRequest.PackageDefaults.Consignee || {};
    var country = (consignee.Country || '').toString().trim().toUpperCase();
    var isCanada = country === 'CA' || country === 'CANADA';

    if (!isCanada) {
        this.vm.requiresPickupAssociation = true;
        this.vm.shouldAutoPickupAssociate = true;
        this.vm.pendingReturnsSave = true;
    }

    var pkg = shipmentRequest.Packages && shipmentRequest.Packages.length > 0 ? shipmentRequest.Packages[0] : null;
    if (pkg) {
        var temp = (pkg.ConsigneeReference || '').toString().trim();
        if (temp.toLowerCase() === 'frozen') {
            var dryIce = (pkg.MiscReference3 || shipmentRequest.PackageDefaults.MiscReference3 || '').toString().trim();
            if (!dryIce) {
                client.alert.Danger('Dry Ice Weight is required when Temperature is Frozen.');
                throw new Error('Dry Ice Weight is required when Temperature is Frozen.');
            }
        }
    }
};

this.PostLoad = function (loadValue, shipmentRequest) {
    if (!shipmentRequest || !shipmentRequest.PackageDefaults) return;
    this.vm.isReturnsWorkflow = true;
    var consignee = shipmentRequest.PackageDefaults.Consignee || {};
    var country = (consignee.Country || '').toString().trim().toUpperCase();
    var isCanada = country === 'CA' || country === 'CANADA';
    this.vm.hidePickupButton = isCanada;
    this.vm.shouldAutoPickupAssociate = !isCanada;

    var pkg = shipmentRequest.Packages && shipmentRequest.Packages.length > 0 ? shipmentRequest.Packages[0] : null;
    var temp = pkg && pkg.ConsigneeReference ? pkg.ConsigneeReference.toString().toLowerCase() : '';
    this.vm.isFrozenTemperature = temp === 'frozen';
    this.vm.isDryIceEditable = this.vm.isFrozenTemperature;
    this.vm.shouldPromptDryIce = this.vm.isFrozenTemperature && !(pkg && pkg.MiscReference3);
};

this.PostSelectAddressBook = function (shipmentRequest, nameaddress) {
    if (!shipmentRequest || !nameaddress) return;
    shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
    shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};
    shipmentRequest.PackageDefaults.Consignee.Company = nameaddress.Company || shipmentRequest.PackageDefaults.Consignee.Company || '';
    shipmentRequest.PackageDefaults.Consignee.Contact = nameaddress.Contact || shipmentRequest.PackageDefaults.Consignee.Contact || '';
    shipmentRequest.PackageDefaults.Consignee.Address1 = nameaddress.Address1 || shipmentRequest.PackageDefaults.Consignee.Address1 || '';
    shipmentRequest.PackageDefaults.Consignee.Address2 = nameaddress.Address2 || shipmentRequest.PackageDefaults.Consignee.Address2 || '';
    shipmentRequest.PackageDefaults.Consignee.City = nameaddress.City || shipmentRequest.PackageDefaults.Consignee.City || '';
    shipmentRequest.PackageDefaults.Consignee.StateProvince = nameaddress.StateProvince || shipmentRequest.PackageDefaults.Consignee.StateProvince || '';
    shipmentRequest.PackageDefaults.Consignee.PostalCode = nameaddress.PostalCode || shipmentRequest.PackageDefaults.Consignee.PostalCode || '';
    shipmentRequest.PackageDefaults.Consignee.Country = nameaddress.Country || shipmentRequest.PackageDefaults.Consignee.Country || '';
    shipmentRequest.PackageDefaults.Consignee.Phone = nameaddress.Phone || shipmentRequest.PackageDefaults.Consignee.Phone || '';

    var country = (shipmentRequest.PackageDefaults.Consignee.Country || '').toString().trim().toUpperCase();
    var isCanada = country === 'CA' || country === 'CANADA';
    this.vm.hidePickupButton = isCanada;
    this.vm.shouldAutoPickupAssociate = !isCanada;
    this.vm.requiresPickupAssociation = !isCanada;
};

}
