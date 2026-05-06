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
        // Step 1: Normalize the location value so our routing comparison is predictable.
        var currentLocation = (location || "").toString().toLowerCase();

        // Step 2: If the user did not land on the shipping page, send them there immediately.
        // This satisfies the blueprint requirement that login should take the user directly into shipping.
        if (currentLocation.indexOf("shipping") === -1) {
            // Step 3: Use the browser location to move to the shipping screen.
            window.location.href = "/shipping";
            // Step 4: Exit early because the page is about to change and no further UI work is needed here.
            return;
        }

        // Step 5: Because the Rate button must be hidden in this template, find it by common label/id patterns.
        // We intentionally keep this defensive because the rendered control IDs can vary by template version.
        var rateButton = document.querySelector("button[data-action='rate'], #btnRate, [name='Rate']");

        // Step 6: If the button exists, hide it so users cannot trigger rate shopping from the UI.
        if (rateButton) {
            // The blueprint explicitly requires that the Rate button not be visible for biological returns.
            rateButton.style.display = "none";
        }

        // Step 7: Prepare for temperature-driven UI behavior by ensuring the shipment request object exists.
        // This does not set business values; it simply makes the page safer for later hooks and UI logic.
        if (!this.vm || !this.vm.shipmentRequest) {
            // Some routes may load before a shipment object is initialized, so we avoid null-reference issues.
            return;
        }

        // Step 8: Keep the Dry Ice Weight field disabled unless the selected temperature is Frozen.
        // The actual enable/disable behavior is reinforced again in Keystroke so it stays in sync as the user changes values.
        var temperatureValue = "";
        if (this.vm.shipmentRequest.PackageDefaults && this.vm.shipmentRequest.PackageDefaults.ConsigneeReference != null) {
            // Convert the temperature field to a string for a stable comparison.
            temperatureValue = this.vm.shipmentRequest.PackageDefaults.ConsigneeReference.toString();
        }

        // Step 9: Locate the Dry Ice Weight input if the template exposes it by a stable identifier.
        var dryIceInput = document.querySelector("input[name='MiscReference3'], #MiscReference3, [data-field='MiscReference3']");

        // Step 10: Disable the Dry Ice Weight field unless Frozen is selected, because only frozen shipments need dry ice entry.
        if (dryIceInput) {
            // Frozen means the user can edit the field; all other values keep it read-only.
            dryIceInput.disabled = !(temperatureValue.toLowerCase() === "frozen");
        }

        // Step 11: Hide the Pickup button when the return address is Canada, because the blueprint says pickup is only needed otherwise.
        var pickupButton = document.querySelector("button[data-action='pickup'], #btnPickup, [name='Pickup']");

        // Step 12: Determine the address country from the current shipment request so the UI can react immediately.
        var pickupCountry = "";
        if (this.vm.shipmentRequest.PackageDefaults && this.vm.shipmentRequest.PackageDefaults.Consignee && this.vm.shipmentRequest.PackageDefaults.Consignee.Country != null) {
            // Convert to string and normalize to uppercase for country comparison.
            pickupCountry = this.vm.shipmentRequest.PackageDefaults.Consignee.Country.toString().trim().toUpperCase();
        }

        // Step 13: Apply the Canada-specific rule to hide the Pickup button; otherwise keep it visible.
        if (pickupButton) {
            // Canada shipments do not need the pickup button shown, per blueprint requirements.
            pickupButton.style.display = pickupCountry === "CA" ? "none" : "";
        }

        // Step 14: No return value is needed because PageLoaded is a UI initialization hook.
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
        *
        * Process flow:
        * 1) Read the logged-in user profile data.
        * 2) Map custom user fields into the shipment references.
        * 3) Map the user address into the shipment consignee/pickup-from address.
        * 4) Keep the defaults aligned with the biological returns template so SBR PreShip can enforce carrier rules later.
        */
        // Step 1: Make sure the shipment object exists before we write defaults into it.
        if (!shipmentRequest) {
            // If the shipping screen has not created a shipment object yet, there is nothing to default.
            return;
        }

        // Step 2: Ensure the package defaults object exists so the code can safely write to address and reference fields.
        if (!shipmentRequest.PackageDefaults) {
            // Create the default shipment container expected by ShipExec.
            shipmentRequest.PackageDefaults = {};
        }

        // Step 3: Read the current user's profile information from the ViewModel because the blueprint says user spreadsheet data drives return labels.
        var profileUserInfo = this.vm && this.vm.profile && this.vm.profile.UserInformation ? this.vm.profile.UserInformation : null;

        // Step 4: If user profile data is available, map it into the shipment request.
        if (profileUserInfo) {
            // Step 4a: Copy the user's address into the pickup-from / consignee address.
            // The exact property names may vary by deployment, so we assign defensively only when values exist.
            shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};

            // Step 4b: Copy address fields commonly used by ShipExec from the profile user object when present.
            // These values come from the user spreadsheet and are intended to serve as the return pickup address.
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

            // Step 4c: Copy Custom2 into Shipper Reference because the blueprint maps that value to Study Reference Code.
            if (profileUserInfo.Custom2 != null) {
                // Store the value exactly as provided so the downstream shipping request preserves the study reference.
                shipmentRequest.PackageDefaults.ShipperReference = profileUserInfo.Custom2;
            }

            // Step 4d: Copy Custom1 into MiscReference1 because the blueprint maps that value to Protocol Number.
            if (profileUserInfo.Custom1 != null) {
                // MiscReference1 carries the protocol number for the return shipment.
                shipmentRequest.PackageDefaults.MiscReference1 = profileUserInfo.Custom1;
            }

            // Step 4e: Copy Custom3 into MiscReference2 because the blueprint maps that value to Site Number.
            if (profileUserInfo.Custom3 != null) {
                // This preserves the site number in the reference field requested by the blueprint.
                shipmentRequest.PackageDefaults.MiscReference2 = profileUserInfo.Custom3;
            }
        }

        // Step 5: Apply the template defaults that are part of the biological returns design.
        // These defaults are safe to set here because NewShipment is specifically the place for initial values.
        shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || "UN3373 Category B Human Sample";
        shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || "Prepaid";
        shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || "UPS Express";
        shipmentRequest.PackageDefaults.WeightUnit = shipmentRequest.PackageDefaults.WeightUnit || "KG";

        // Step 6: Default the biological sample flag to true, because the blueprint says this checkbox should start ON.
        if (shipmentRequest.PackageDefaults.MiscReference4 == null || shipmentRequest.PackageDefaults.MiscReference4 === "") {
            // The checkbox is a true/false field, so we default it to true for the return-label workflow.
            shipmentRequest.PackageDefaults.MiscReference4 = true;
        }

        // Step 7: If the return address is not Canada, the pickup button may be needed later; the actual auto-click is handled in PreShip.
        // We intentionally do not force any UI click here because NewShipment is for data defaults rather than interaction.
    };

    this.Keystroke = function(shipmentRequest, vm, event) {
        /*
        * Keystroke hook for the biological returns shipping template.
        * This hook exists because the blueprint requires immediate UI feedback when the Temperature field changes:
        * it must set package weight defaults based on the selected temperature and control Dry Ice Weight editability.
        *
        * Blueprint requirements fulfilled:
        * 1) When Temperature/ConsigneeReference changes, set package weight based on the selected option.
        * 2) When Temperature is Frozen, make Dry Ice Weight editable and allow the user to enter MiscReference3.
        * 3) When Temperature is anything other than Frozen, make Dry Ice Weight read-only.
        *
        * Process flow:
        * 1) Inspect the keypress to determine whether the user is interacting with the Temperature field.
        * 2) Apply the correct package weight default for the selected temperature option.
        * 3) Toggle the Dry Ice Weight field enabled state based on the Frozen condition.
        * 4) Keep this hook lightweight so it complements, rather than replaces, the more authoritative server-side PreShip logic.
        */
        // Step 1: Ensure we have the shipment request object before trying to modify any fields.
        if (!shipmentRequest || !shipmentRequest.PackageDefaults) {
            // Without a shipment request there is no UI state to update.
            return;
        }

        // Step 2: Read the current temperature selection from the shipment request.
        var selectedTemperature = "";
        if (shipmentRequest.PackageDefaults.ConsigneeReference != null) {
            // Convert to a string so comparisons are stable no matter how the UI bound the value.
            selectedTemperature = shipmentRequest.PackageDefaults.ConsigneeReference.toString().trim();
        }

        // Step 3: Define the weight defaults required by the blueprint for each temperature option.
        // These values drive the quick-entry behavior on the client side.
        var weightByTemperature = {
            "ambient": 3,
            "frozen": 6,
            "refrigerated": 5,
            "ambient/refrigerated combo box": 6
        };

        // Step 4: If the user is changing the Temperature field, apply the corresponding package weight default.
        // We look at the event text defensively because the exact key mapping can vary across template implementations.
        var isTemperatureInteraction = false;
        if (event && event.target) {
            // Determine whether the user is interacting with the temperature-related input control.
            var targetName = (event.target.name || event.target.id || "").toString().toLowerCase();
            isTemperatureInteraction = targetName.indexOf("consigneereference") >= 0 || targetName.indexOf("temperature") >= 0;
        }

        // Step 5: If the current interaction is temperature-related, push the corresponding default package weight.
        if (isTemperatureInteraction) {
            // Normalize the selected value to lower case for easy lookup.
            var lookupKey = selectedTemperature.toLowerCase();

            // If we know the selected temperature, apply the matching package weight immediately.
            if (weightByTemperature.hasOwnProperty(lookupKey)) {
                // Ensure the package defaults include a weight object before assigning the amount.
                shipmentRequest.PackageDefaults.Weight = shipmentRequest.PackageDefaults.Weight || {};
                // The blueprint defines these temperature-specific starter weights for user convenience.
                shipmentRequest.PackageDefaults.Weight.Amount = weightByTemperature[lookupKey];
            }
        }

        // Step 6: Locate the Dry Ice Weight field so we can enable or disable it as the user changes temperature.
        var dryIceInput = document.querySelector("input[name='MiscReference3'], #MiscReference3, [data-field='MiscReference3']");

        // Step 7: Only allow editing of Dry Ice Weight when the temperature is Frozen.
        if (dryIceInput) {
            // Frozen shipments require dry ice entry; all other temperatures should keep the field locked.
            dryIceInput.disabled = !(selectedTemperature.toLowerCase() === "frozen");
        }

        // Step 8: If Frozen is selected, prompt the user to enter the dry ice value so it can later be sent in MiscReference3.
        // We keep the prompt lightweight and client-side because the blueprint explicitly asks for a modal or field capture.
        if (selectedTemperature.toLowerCase() === "frozen") {
            // Only prompt when the field is empty so the user is not repeatedly interrupted while typing.
            if (shipmentRequest.PackageDefaults.MiscReference3 == null || shipmentRequest.PackageDefaults.MiscReference3 === "") {
                // Use a browser prompt as a simple implementation pattern for the required capture modal.
                var dryIceValue = window.prompt("Enter Dry Ice Weight (kg):", "");
                // If the user entered a value, store it in MiscReference3 so SBR PreShip can convert and apply it.
                if (dryIceValue != null && dryIceValue !== "") {
                    shipmentRequest.PackageDefaults.MiscReference3 = dryIceValue;
                }
            }
        }

        // Step 9: No explicit return value is required; the hook mutates the current UI shipment object in place.
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
        *
        * Blueprint requirements fulfilled:
        * 1) When Pickup From/Consignee address is not Canada, automatically click the Pickup Request button and Save so a Pickup object is associated.
        * 2) If the client-side Pickup workflow is not reliable, the server-side SBR Ship hook provides a backup strategy.
        * 3) When Temperature is Frozen, collect Dry Ice Weight and store it in MiscReference3 so SBR PreShip can apply dry ice rules.
        *
        * Process flow:
        * 1) Read the current shipment state from the ViewModel.
        * 2) Determine whether the pickup-from country is Canada.
        * 3) If the shipment is not Canada-based, attempt the pickup association workflow by simulating the expected UI action.
        * 4) Ensure dry ice data exists when the shipment is frozen.
        * 5) Let the server-side SBR PreShip hook perform the authoritative carrier/business-rule enforcement.
        */
        // Step 1: Make sure the current shipment request exists because this hook operates on the active form data.
        if (!shipmentRequest || !shipmentRequest.PackageDefaults) {
            // If there is no shipment to inspect, there is nothing to preflight.
            return;
        }

        // Step 2: Read the pickup-from/consignee country so we can decide whether the Pickup button should be triggered.
        var country = "";
        if (shipmentRequest.PackageDefaults.Consignee && shipmentRequest.PackageDefaults.Consignee.Country != null) {
            // Normalize the country code to uppercase for consistent comparison.
            country = shipmentRequest.PackageDefaults.Consignee.Country.toString().trim().toUpperCase();
        }

        // Step 3: When the pickup-from address is not Canada, try to trigger the Pickup Request workflow before shipping.
        if (country !== "CA") {
            // Find the Pickup button using a few common selectors because template IDs can vary.
            var pickupButton = document.querySelector("button[data-action='pickup'], #btnPickup, [name='Pickup']");
            // Find a Save button because the blueprint says the user should click Pickup Request and Save to associate the object.
            var saveButton = document.querySelector("button[data-action='save'], #btnSave, [name='Save']");

            // If the Pickup button exists, click it to initiate pickup association.
            if (pickupButton && typeof pickupButton.click === "function") {
                // This is the client-side convenience action requested by the blueprint.
                pickupButton.click();
            }

            // If a Save button exists, click it as well so the Pickup object is persisted with the shipment.
            if (saveButton && typeof saveButton.click === "function") {
                // Saving is required so the pickup association survives the ship request.
                saveButton.click();
            }
        }

        // Step 4: If the shipment is Frozen, make sure Dry Ice Weight exists before the request is sent to the server.
        var tempValue = "";
        if (shipmentRequest.PackageDefaults.ConsigneeReference != null) {
            // Read the temperature field as text for comparison.
            tempValue = shipmentRequest.PackageDefaults.ConsigneeReference.toString().trim().toLowerCase();
        }

        // Step 5: If Frozen and Dry Ice Weight has not been captured yet, prompt the user now so SBR PreShip can use it.
        if (tempValue === "frozen" && (shipmentRequest.PackageDefaults.MiscReference3 == null || shipmentRequest.PackageDefaults.MiscReference3 === "")) {
            // Ask for the required kilogram value right before shipping to keep the workflow simple for the user.
            var enteredDryIce = window.prompt("Frozen shipment detected. Enter Dry Ice Weight (kg):", "");
            // Store the entered value so the server-side rule can read and convert it later.
            if (enteredDryIce != null && enteredDryIce !== "") {
                shipmentRequest.PackageDefaults.MiscReference3 = enteredDryIce;
            } else {
                // If the user cancels, stop the ship flow by throwing an error so the missing value cannot slip through.
                throw new Error("Dry Ice Weight (kg) is required when Temperature is Frozen.");
            }
        }

        // Step 6: This hook intentionally does not perform carrier logic; it only prepares the client-side data.
        // The authoritative service, paperless invoice, and dry ice conversion rules are enforced in SBR PreShip.
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
