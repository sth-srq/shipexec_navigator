function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        /*
        * PageLoaded hook for the biological returns shipping template.
        * This hook exists to satisfy the blueprint requirement that the user should be taken to the shipping page on login,
        * and to initialize page-level UI behavior for the custom specimen return workflow.
        *
        * Blueprint requirements fulfilled:
        * 1) On login, automatically route the user to the shipping page.
        * 2) Hide the Rate button on the biological returns template.
        * 3) Prepare shipping-page-specific UI rules that depend on the current shipment state.
        *
        * Process flow:
        * 1) Check the current location/route.
        * 2) If we are not already on the shipping page, redirect there.
        * 3) Apply visual rules that are safe to perform as soon as the page loads.
        * 4) Coordinate with NewShipment and PreShip, which handle defaulting and preflight business actions.
        */
        var currentLocation = (location || "").toString().toLowerCase();

        if (currentLocation.indexOf("shipping") === -1) {
            window.location.href = "/shipping";
            return;
        }

        var rateButton = document.querySelector("button[data-action='rate'], #btnRate, [name='Rate']");
        if (rateButton) {
            rateButton.style.display = "none";
        }

        if (!this.vm || !this.vm.shipmentRequest) {
            return;
        }

        var temperatureValue = "";
        if (this.vm.shipmentRequest.PackageDefaults && this.vm.shipmentRequest.PackageDefaults.ConsigneeReference != null) {
            temperatureValue = this.vm.shipmentRequest.PackageDefaults.ConsigneeReference.toString();
        }

        var dryIceInput = document.querySelector("input[name='MiscReference3'], #MiscReference3, [data-field='MiscReference3']");
        if (dryIceInput) {
            dryIceInput.disabled = !(temperatureValue.toLowerCase() === "frozen");
        }

        var pickupButton = document.querySelector("button[data-action='pickup'], #btnPickup, [name='Pickup']");
        var pickupCountry = "";
        if (this.vm.shipmentRequest.PackageDefaults && this.vm.shipmentRequest.PackageDefaults.Consignee && this.vm.shipmentRequest.PackageDefaults.Consignee.Country != null) {
            pickupCountry = this.vm.shipmentRequest.PackageDefaults.Consignee.Country.toString().trim().toUpperCase();
        }

        if (pickupButton) {
            pickupButton.style.display = pickupCountry === "CA" ? "none" : "";
        }
    };

    this.NewShipment = function(shipmentRequest) {
        /*
        * NewShipment hook for the biological returns workflow.
        * This hook exists to prepopulate a new return shipment from the user profile, so the user starts
        * with the correct pickup-from address and reference data every time a new shipment is created.
        *
        * Blueprint requirements fulfilled:
        * 1) Set the Consignee/Pickup From address to the user address values from the profile spreadsheet.
        * 2) Set Shipper Reference / Study Reference Code from user Custom2.
        * 3) Set MiscReference1 / Protocol Number from user Custom1.
        * 4) Prepare the form for the return-label experience with the correct default behavior.
        */
        if (!shipmentRequest) {
            return;
        }

        if (!shipmentRequest.PackageDefaults) {
            shipmentRequest.PackageDefaults = {};
        }

        var profileUserInfo = this.vm && this.vm.profile && this.vm.profile.UserInformation ? this.vm.profile.UserInformation : null;
        if (profileUserInfo) {
            shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};
            if (profileUserInfo.Address1) shipmentRequest.PackageDefaults.Consignee.Address1 = profileUserInfo.Address1;
            if (profileUserInfo.Address2) shipmentRequest.PackageDefaults.Consignee.Address2 = profileUserInfo.Address2;
            if (profileUserInfo.Address3) shipmentRequest.PackageDefaults.Consignee.Address3 = profileUserInfo.Address3;
            if (profileUserInfo.City) shipmentRequest.PackageDefaults.Consignee.City = profileUserInfo.City;
            if (profileUserInfo.StateProvince) shipmentRequest.PackageDefaults.Consignee.StateProvince = profileUserInfo.StateProvince;
            if (profileUserInfo.PostalCode) shipmentRequest.PackageDefaults.Consignee.PostalCode = profileUserInfo.PostalCode;
            if (profileUserInfo.Country) shipmentRequest.PackageDefaults.Consignee.Country = profileUserInfo.Country;
            if (profileUserInfo.Phone) shipmentRequest.PackageDefaults.Consignee.Phone = profileUserInfo.Phone;
            if (profileUserInfo.Company) shipmentRequest.PackageDefaults.Consignee.Company = profileUserInfo.Company;
            if (profileUserInfo.Contact) shipmentRequest.PackageDefaults.Consignee.Contact = profileUserInfo.Contact;

            if (profileUserInfo.Custom2 != null) {
                shipmentRequest.PackageDefaults.ShipperReference = profileUserInfo.Custom2;
            }

            if (profileUserInfo.Custom1 != null) {
                shipmentRequest.PackageDefaults.MiscReference1 = profileUserInfo.Custom1;
            }

            if (profileUserInfo.Custom3 != null) {
                shipmentRequest.PackageDefaults.MiscReference2 = profileUserInfo.Custom3;
            }
        }

        shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || "UN3373 Category B Human Sample";
        shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || "Prepaid";
        shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || "UPS Express";
        shipmentRequest.PackageDefaults.WeightUnit = shipmentRequest.PackageDefaults.WeightUnit || "KG";

        if (shipmentRequest.PackageDefaults.MiscReference4 == null || shipmentRequest.PackageDefaults.MiscReference4 === "") {
            shipmentRequest.PackageDefaults.MiscReference4 = true;
        }
    };

    this.Keystroke = function(shipmentRequest, vm, event) {
        /*
        * Keystroke hook for the biological returns shipping template.
        * This hook exists because the blueprint requires immediate UI feedback when the Temperature field changes:
        * it must set package weight defaults based on the selected temperature and control Dry Ice Weight editability.
        */
        if (!shipmentRequest || !shipmentRequest.PackageDefaults) {
            return;
        }

        var selectedTemperature = "";
        if (shipmentRequest.PackageDefaults.ConsigneeReference != null) {
            selectedTemperature = shipmentRequest.PackageDefaults.ConsigneeReference.toString().trim();
        }

        var weightByTemperature = {
            "ambient": 3,
            "frozen": 6,
            "refrigerated": 5,
            "ambient/refrigerated combo box": 6
        };

        var isTemperatureInteraction = false;
        if (event && event.target) {
            var targetName = (event.target.name || event.target.id || "").toString().toLowerCase();
            isTemperatureInteraction = targetName.indexOf("consigneereference") >= 0 || targetName.indexOf("temperature") >= 0;
        }

        if (isTemperatureInteraction) {
            var lookupKey = selectedTemperature.toLowerCase();
            if (Object.prototype.hasOwnProperty.call(weightByTemperature, lookupKey)) {
                shipmentRequest.PackageDefaults.Weight = shipmentRequest.PackageDefaults.Weight || {};
                shipmentRequest.PackageDefaults.Weight.Amount = weightByTemperature[lookupKey];
            }
        }

        var dryIceInput = document.querySelector("input[name='MiscReference3'], #MiscReference3, [data-field='MiscReference3']");
        if (dryIceInput) {
            dryIceInput.disabled = !(selectedTemperature.toLowerCase() === "frozen");
        }

        if (selectedTemperature.toLowerCase() === "frozen") {
            if (shipmentRequest.PackageDefaults.MiscReference3 == null || shipmentRequest.PackageDefaults.MiscReference3 === "") {
                var dryIceValue = window.prompt("Enter Dry Ice Weight (kg):", "");
                if (dryIceValue != null && dryIceValue !== "") {
                    shipmentRequest.PackageDefaults.MiscReference3 = dryIceValue;
                }
            }
        }
    };

    this.PreLoad = function(loadValue, shipmentRequest, userParams) {
    };

    this.PostLoad = function(loadValue, shipmentRequest) {
    };

    this.PreShip = function(shipmentRequest, userParams) {
        /*
        * PreShip hook for the biological returns shipping template.
        * This client-side hook exists to provide immediate convenience actions before the shipment request is sent:
        * it attempts to associate a Pickup for non-Canada shipments and ensures Dry Ice Weight is captured when needed.
        */
        if (!shipmentRequest || !shipmentRequest.PackageDefaults) {
            return;
        }

        var country = "";
        if (shipmentRequest.PackageDefaults.Consignee && shipmentRequest.PackageDefaults.Consignee.Country != null) {
            country = shipmentRequest.PackageDefaults.Consignee.Country.toString().trim().toUpperCase();
        }

        if (country !== "CA") {
            var pickupButton = document.querySelector("button[data-action='pickup'], #btnPickup, [name='Pickup']");
            var saveButton = document.querySelector("button[data-action='save'], #btnSave, [name='Save']");

            if (pickupButton && typeof pickupButton.click === "function") {
                pickupButton.click();
            }

            if (saveButton && typeof saveButton.click === "function") {
                saveButton.click();
            }
        }

        var tempValue = "";
        if (shipmentRequest.PackageDefaults.ConsigneeReference != null) {
            tempValue = shipmentRequest.PackageDefaults.ConsigneeReference.toString().trim().toLowerCase();
        }

        if (tempValue === "frozen" && (shipmentRequest.PackageDefaults.MiscReference3 == null || shipmentRequest.PackageDefaults.MiscReference3 === "")) {
            var enteredDryIce = window.prompt("Frozen shipment detected. Enter Dry Ice Weight (kg):", "");
            if (enteredDryIce != null && enteredDryIce !== "") {
                shipmentRequest.PackageDefaults.MiscReference3 = enteredDryIce;
            } else {
                throw new Error("Dry Ice Weight (kg) is required when Temperature is Frozen.");
            }
        }
    };

    this.PostShip = function(shipmentRequest, shipmentResponse) {
    };

    this.PreRate = function(shipmentRequest, userParams) {
    };

    this.PostRate = function(shipmentRequest, rateResults) {
    };

    this.PreVoid = function(pkg, userParams) {
    };

    this.PostVoid = function(pkg) {
    };

    this.PrePrint = function(document, localPort) {
    };

    this.PostPrint = function(document) {
    };

    this.PreBuildShipment = function(shipmentRequest) {
    };

    this.PostBuildShipment = function(shipmentRequest) {
    };

    this.RepeatShipment = function(currentShipment) {
    };

    this.PreProcessBatch = function(batchReference, actions, params, vm) {
    };

    this.PostProcessBatch = function(batchResponse, vm) {
    };

    this.PreSearchHistory = function(searchCriteria) {
    };

    this.PostSearchHistory = function(packages) {
    };

    this.PreCloseManifest = function(manifestItem, userParams) {
    };

    this.PostCloseManifest = function(manifestItem) {
    };

    this.PreTransmit = function(transmitItem, userParams) {
    };

    this.PostTransmit = function(transmitItem) {
    };

    this.PreCreateGroup = function(groupRequest, userParams) {
    };

    this.PostCreateGroup = function(groupRequest) {
    };

    this.PreModifyGroup = function(groupRequest, userParams) {
    };

    this.PostModifyGroup = function(groupRequest) {
    };

    this.PreCloseGroup = function(groupRequest, userParams) {
    };

    this.PostCloseGroup = function(groupRequest) {
    };

    this.AddPackage = function(shipmentRequest, packageIndex) {
    };

    this.CopyPackage = function(shipmentRequest, packageIndex) {
    };

    this.RemovePackage = function(shipmentRequest, packageIndex) {
    };

    this.PostSelectAddressBook = function(shipmentRequest, nameaddress) {
    };

}
