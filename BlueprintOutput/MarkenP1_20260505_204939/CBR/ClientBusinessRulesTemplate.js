function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        /*
        * PageLoaded is the first client-side hook that runs when a ShipExec page finishes loading.
        * This implementation exists because the blueprint requires the user to be sent directly
        * to the shipping page on login and also requires location-based UI behavior for the
        * specimen-return workflow, especially Pickup button visibility.
        *
        * Blueprint requirements fulfilled:
        * 1) On login, automatically take the user to the shipping page.
        * 2) When Pickup From / Consignee country is Canada, hide the Pickup button.
        * 3) When Pickup From / Consignee country is not Canada, the workflow should allow pickup association.
        *
        * How this hook fits into the chain:
        * - PageLoaded runs before NewShipment, PostLoad, and user-driven edits.
        * - It is the best place to initialize screen behavior that should exist before any shipment data is entered.
        *
        * Process flow:
        * Step 1: Detect the current page location.
        * Step 2: Redirect the user to the shipping page if they are not already there.
        * Step 3: Inspect the current shipment country so we can hide/show the Pickup button.
        * Step 4: Leave the rest of the shipment workflow to NewShipment/PostLoad/PreShip.
        */
        var currentLocation = (location || '').toString(); // Normalize the location value so string checks are safe.

        // Step 1: Force navigation to the shipping page when the application first loads.
        if (currentLocation === '/' || currentLocation === '' || currentLocation.indexOf('/shipping') === -1) {
            // Use a simple location assignment so the browser moves directly into the specimen-return shipping screen.
            window.location = '/shipping';
        }

        // Step 2: Read the current shipment country from the UI model when it is already available.
        var shipmentRequest = this.vm && this.vm.shipmentRequest ? this.vm.shipmentRequest : null; // Guard against missing ViewModel data.
        var consigneeCountry = '';
        if (shipmentRequest && shipmentRequest.PackageDefaults && shipmentRequest.PackageDefaults.Consignee) {
            // The blueprint repurposes Consignee as "Pickup From", so country-based button control must look here.
            consigneeCountry = (shipmentRequest.PackageDefaults.Consignee.Country || '').toString();
        }

        // Step 3: Hide the Pickup button when the pickup-from country is Canada.
        if (consigneeCountry.toUpperCase() === 'CA') {
            // Find the pickup button by common ShipExec button text and hide it so the user cannot request pickup for Canada.
            var pickupButtons = document.querySelectorAll('button, input[type="button"], input[type="submit"]'); // Broad selector to cover template variations.
            for (var i = 0; i < pickupButtons.length; i++) {
                // Inspect button text/value in a case-insensitive way because templates can render buttons differently.
                var buttonLabel = ((pickupButtons[i].innerText || pickupButtons[i].value || '') + '').toLowerCase();
                if (buttonLabel.indexOf('pickup') !== -1) {
                    // Hide matching pickup controls because the blueprint explicitly says Pickup should be hidden for Canada.
                    pickupButtons[i].style.display = 'none';
                }
            }
        }

        // Step 4: If the country is not Canada, do not force pickup here; NewShipment/PostLoad/PreShip will continue the workflow.
    };

    this.NewShipment = function(shipmentRequest) {
        /*
        * NewShipment runs whenever ShipExec creates a brand-new shipment object on the client.
        * This is the primary defaulting hook for the Marken specimen-return workflow because
        * the blueprint requires the current user profile and custom data to seed the shipment
        * with Pickup From data, reference mappings, default description, service, terms, and return settings.
        *
        * Blueprint requirements fulfilled:
        * 1) Set Consignee address to the user address values so it behaves like "Pickup From".
        * 2) Default ShipperReference to the user's Custom2 value (Study Reference Code).
        * 3) Default MiscReference1 to the user's Custom1 value (Protocol Number).
        * 4) Prepare other specimen-return defaults such as description, service, terms, return delivery, and weights.
        *
        * How this hook fits into the chain:
        * - NewShipment runs after a new shipment is created but before the user starts typing.
        * - PostLoad can later refine UI behavior based on selection changes.
        * - PreShip performs the final validation before the request is transmitted to the server.
        *
        * Process flow:
        * Step 1: Read the logged-in user's profile data.
        * Step 2: Copy the user address into the shipment consignee/pickup-from location.
        * Step 3: Map user custom fields into the correct shipping reference fields.
        * Step 4: Apply the blueprint defaults for description, service, terms, and return settings.
        */
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest; // Accept the passed object or fall back to the ViewModel.
        if (!shipmentRequest) {
            return; // Nothing to initialize if the shipment object does not exist yet.
        }

        // Step 1: Make sure the shipment defaults container exists before we write any defaults.
        if (!shipmentRequest.PackageDefaults) {
            shipmentRequest.PackageDefaults = {}; // Create the container when the template has not supplied one yet.
        }

        // Step 2: Read user profile and user address data because the blueprint says the user's address is the Pickup From address.
        var userInfo = this.vm && this.vm.profile && this.vm.profile.UserInformation ? this.vm.profile.UserInformation : null; // Guard for missing profile objects.
        var userAddress = userInfo && userInfo.Address ? userInfo.Address : null; // The user's address holds the reference custom data.

        if (userAddress) {
            // Step 3: Clone the user address onto Consignee so the screen behaves as the Pickup From location.
            shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {}; // Preserve any existing object if the template already created one.
            shipmentRequest.PackageDefaults.Consignee.Company = userAddress.Company || ''; // Use user address company for Pickup From.
            shipmentRequest.PackageDefaults.Consignee.Contact = userAddress.Contact || ''; // Preserve contact details for return label pickup origin.
            shipmentRequest.PackageDefaults.Consignee.Address1 = userAddress.Address1 || '';
            shipmentRequest.PackageDefaults.Consignee.Address2 = userAddress.Address2 || '';
            shipmentRequest.PackageDefaults.Consignee.Address3 = userAddress.Address3 || '';
            shipmentRequest.PackageDefaults.Consignee.City = userAddress.City || '';
            shipmentRequest.PackageDefaults.Consignee.StateProvince = userAddress.StateProvince || '';
            shipmentRequest.PackageDefaults.Consignee.PostalCode = userAddress.PostalCode || '';
            shipmentRequest.PackageDefaults.Consignee.Country = userAddress.Country || ''; // This country drives Canada vs. non-Canada UI logic.
            shipmentRequest.PackageDefaults.Consignee.Phone = userAddress.Phone || '';
        }

        // Step 4: Map the user's custom data into the shipment references defined by the blueprint.
        var getCustomValue = function(key) {
            // Read CustomData safely so the code works whether the profile stores the array in a CBR-friendly format or not.
            if (!userAddress || !userAddress.CustomData) {
                return '';
            }
            for (var i = 0; i < userAddress.CustomData.length; i++) {
                if ((userAddress.CustomData[i].Key || '') === key) {
                    return userAddress.CustomData[i].Value || '';
                }
            }
            return '';
        };

        // Study Reference Code comes from user Custom2 and is stored in ShipperReference.
        shipmentRequest.PackageDefaults.ShipperReference = getCustomValue('Custom2');
        // Protocol Number comes from user Custom1 and is stored in MiscReference1.
        shipmentRequest.PackageDefaults.MiscReference1 = getCustomValue('Custom1');
        // Site Number comes from user Custom3 and is stored in MiscReference2.
        shipmentRequest.PackageDefaults.MiscReference2 = getCustomValue('Custom3');

        // Step 5: Apply the blueprint defaults for specimen-return shipping.
        shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample'; // Default description from the blueprint.
        shipmentRequest.PackageDefaults.ReturnDelivery = true; // Return delivery must be on for the specimen-return workflow.
        shipmentRequest.PackageDefaults.SaturdayDelivery = true; // Saturday delivery defaults on per blueprint.
        shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid'; // Default terms to prepaid.

        // Step 6: Set the default service to UPS Express when none is already assigned.
        if (!shipmentRequest.PackageDefaults.Service) {
            shipmentRequest.PackageDefaults.Service = {}; // Create a service object so the UI has something to display.
        }
        if (!shipmentRequest.PackageDefaults.Service.Symbol) {
            shipmentRequest.PackageDefaults.Service.Symbol = 'CONNECTSHIP_UPS.UPS.EXP'; // Blueprint default service.
            shipmentRequest.PackageDefaults.Service.Name = 'UPS Express'; // Friendly label for the selected service.
        }

        // Step 7: Default package units to KG by setting the first package weight units when available.
        if (shipmentRequest.Packages && shipmentRequest.Packages.length > 0) {
            if (!shipmentRequest.Packages[0].Weight) {
                shipmentRequest.Packages[0].Weight = {}; // Build the weight object if the template has not done so.
            }
            shipmentRequest.Packages[0].Weight.Units = shipmentRequest.Packages[0].Weight.Units || 'KG'; // EU/UK profile default from the blueprint.
        }
    };

    this.Keystroke = function(shipmentRequest, vm, event) {
        /*
        * Keystroke runs whenever the user presses a key while the shipping screen is active.
        * The Marken blueprint uses it as a support hook for fast-entry behavior, especially
        * when the Temperature validation list changes and the UI should immediately react.
        *
        * Blueprint requirements fulfilled:
        * 1) React instantly when Temperature / ConsigneeReference changes.
        * 2) Support the specimen-return workflow where frozen shipments require dry-ice entry.
        * 3) Keep the client-side UI synchronized with the business rules applied in PostLoad and PreShip.
        *
        * How this hook fits into the chain:
        * - Keystroke is a lightweight UI response hook.
        * - It complements PostLoad by reacting to immediate edits rather than load-time defaults.
        * - PreShip still performs the final block/validation before shipping.
        *
        * Process flow:
        * Step 1: Detect whether the user changed the Temperature field.
        * Step 2: Update weight and dry-ice editing behavior immediately.
        * Step 3: Leave final authoritative validation to the server-side PreShip rule.
        */
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest; // Use the active shipment from the ViewModel.
        var eventObject = event || window.event; // Support browser event naming differences.
        if (!shipmentRequest || !eventObject) {
            return; // Nothing to do if the shipment or keyboard event is missing.
        }

        // Step 1: Identify whether the active field is the Temperature / ConsigneeReference control.
        var targetName = ((eventObject.target && (eventObject.target.name || eventObject.target.id || '')) + '').toLowerCase(); // Normalize for safe comparisons.
        if (targetName.indexOf('consigneereference') === -1 && targetName.indexOf('temperature') === -1) {
            return; // Only react for the temperature field to keep the hook efficient.
        }

        // Step 2: Read the current temperature selection from the shipment.
        var tempValue = '';
        if (shipmentRequest.PackageDefaults && shipmentRequest.PackageDefaults.ConsigneeReference) {
            tempValue = (shipmentRequest.PackageDefaults.ConsigneeReference || '').toString();
        }

        // Step 3: Auto-set the package weight based on the temperature option selected in the blueprint.
        var defaultWeight = null;
        if (tempValue === 'Ambient') {
            defaultWeight = 3;
        } else if (tempValue === 'Frozen') {
            defaultWeight = 6;
        } else if (tempValue === 'Refrigerated') {
            defaultWeight = 5;
        } else if (tempValue === 'Ambient/Refrigerated Combo Box') {
            defaultWeight = 6;
        }

        // Apply the weight only when the blueprint mapping produced a known value.
        if (defaultWeight !== null && shipmentRequest.Packages && shipmentRequest.Packages.length > 0) {
            if (!shipmentRequest.Packages[0].Weight) {
                shipmentRequest.Packages[0].Weight = {}; // Create the Weight object if it does not already exist.
            }
            shipmentRequest.Packages[0].Weight.Amount = defaultWeight; // Populate the package weight to match the selected temperature category.
            shipmentRequest.Packages[0].Weight.Units = shipmentRequest.Packages[0].Weight.Units || 'KG'; // Keep the default unit aligned with the blueprint.
        }

        // Step 4: If Frozen is selected, keep dry-ice entry available; otherwise ensure the field is not editable.
        var dryIceInput = document.querySelector('[name="MiscReference3"], [id*="MiscReference3"], [name*="DryIceWeight"]'); // Find the dry ice UI control in a template-tolerant way.
        if (dryIceInput) {
            if (tempValue === 'Frozen') {
                dryIceInput.removeAttribute('readonly'); // Frozen shipments must allow dry-ice entry.
                dryIceInput.removeAttribute('disabled'); // Ensure the user can enter the dry-ice value.
            } else {
                dryIceInput.setAttribute('readonly', 'readonly'); // Other temperatures must not allow dry-ice editing.
                dryIceInput.setAttribute('disabled', 'disabled'); // Keep the control disabled when frozen is not selected.
            }
        }
    };

    this.PreLoad = function(loadValue, shipmentRequest, userParams) {
    };

    this.PostLoad = function(loadValue, shipmentRequest) {
        /*
        * PostLoad runs after a load or shipment refresh has populated the screen.
        * The Marken blueprint uses this hook to align the user interface with the selected
        * Temperature option and to make sure the initial weight reflects the return-sample category.
        *
        * Blueprint requirements fulfilled:
        * 1) Set package weight based on the selected Temperature category.
        * 2) Toggle dry-ice field editability depending on whether Frozen is selected.
        * 3) Keep the client-side screen in sync after the shipment loads or refreshes.
        *
        * How this hook fits into the chain:
        * - PostLoad happens after a shipment is loaded into the UI.
        * - NewShipment sets base defaults; PostLoad applies the visual behavior once the UI is ready.
        * - Keystroke can still make live edits after PostLoad.
        *
        * Process flow:
        * Step 1: Read the Temperature field from the shipment.
        * Step 2: Apply the corresponding default package weight.
        * Step 3: Enable or disable the dry-ice field based on Frozen status.
        */
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest; // Use the loaded shipment from the view model.
        if (!shipmentRequest) {
            return; // Nothing to update if the shipment is missing.
        }

        // Step 1: Read the Temperature selection from the shipment defaults.
        var tempValue = '';
        if (shipmentRequest.PackageDefaults && shipmentRequest.PackageDefaults.ConsigneeReference) {
            tempValue = (shipmentRequest.PackageDefaults.ConsigneeReference || '').toString();
        }

        // Step 2: Map the blueprint temperature values to the default weights.
        var defaultWeight = null;
        if (tempValue === 'Ambient') {
            defaultWeight = 3;
        } else if (tempValue === 'Frozen') {
            defaultWeight = 6;
        } else if (tempValue === 'Refrigerated') {
            defaultWeight = 5;
        } else if (tempValue === 'Ambient/Refrigerated Combo Box') {
            defaultWeight = 6;
        }

        // Step 3: Apply the default weight to the first package so the user starts with the correct specimen weight.
        if (defaultWeight !== null && shipmentRequest.Packages && shipmentRequest.Packages.length > 0) {
            if (!shipmentRequest.Packages[0].Weight) {
                shipmentRequest.Packages[0].Weight = {}; // Create the weight object when needed.
            }
            shipmentRequest.Packages[0].Weight.Amount = defaultWeight; // Populate the UI weight field with the temperature-specific default.
            shipmentRequest.Packages[0].Weight.Units = shipmentRequest.Packages[0].Weight.Units || 'KG'; // Preserve the blueprint's kilogram default.
        }

        // Step 4: Enable or disable the dry-ice field according to whether Frozen is selected.
        var dryIceInput = document.querySelector('[name="MiscReference3"], [id*="MiscReference3"], [name*="DryIceWeight"]'); // Tolerant selector for template variations.
        if (dryIceInput) {
            if (tempValue === 'Frozen') {
                dryIceInput.removeAttribute('readonly'); // Frozen means dry-ice entry must be editable.
                dryIceInput.removeAttribute('disabled'); // Allow the user to type the dry-ice amount.
            } else {
                dryIceInput.setAttribute('readonly', 'readonly'); // Non-frozen shipments should not allow edits.
                dryIceInput.setAttribute('disabled', 'disabled'); // Disable the control to match the frozen-only business rule.
            }
        }
    };

    this.PreShip = function(shipmentRequest, userParams) {
        /*
        * PreShip is the client-side validation hook that runs immediately before the shipment
        * request is sent to the server. The Marken blueprint uses this hook to make sure
        * frozen specimens have a dry-ice value captured in MiscReference3 and to keep the
        * user experience consistent with the temperature-driven workflow.
        *
        * Blueprint requirements fulfilled:
        * 1) When Temperature/ConsigneeReference is Frozen, require or capture Dry Ice Weight.
        * 2) Store the entered dry-ice value in MiscReference3 so the SBR PreShip rule can convert and apply it.
        * 3) Optionally support pickup association behavior from the client side when the country is not Canada.
        *
        * How this hook fits into the chain:
        * - CBR PreShip is the last chance to stop the shipment before the request leaves the browser.
        * - SBR PreShip will still perform the authoritative server-side enforcement.
        * - This hook is intentionally user-friendly so the user gets immediate feedback before the server is called.
        *
        * Process flow:
        * Step 1: Check the current Temperature selection.
        * Step 2: If Frozen, make sure a dry-ice value exists and is stored in MiscReference3.
        * Step 3: If not Frozen, leave the dry-ice field blank or read-only.
        * Step 4: Allow the server-side PreShip hook to perform final validation and conversion.
        */
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest; // Use the current shipment object.
        if (!shipmentRequest) {
            return; // No shipment means nothing to validate.
        }

        // Step 1: Read the selected Temperature value from the shipment.
        var tempValue = '';
        if (shipmentRequest.PackageDefaults && shipmentRequest.PackageDefaults.ConsigneeReference) {
            tempValue = (shipmentRequest.PackageDefaults.ConsigneeReference || '').toString();
        }

        // Step 2: If Frozen, ensure MiscReference3 is present and populated with the dry ice value.
        if (tempValue === 'Frozen') {
            // Look for a dry-ice field in the UI so we can validate it before the shipment leaves the browser.
            var dryIceInput = document.querySelector('[name="MiscReference3"], [id*="MiscReference3"], [name*="DryIceWeight"]');
            var dryIceValue = '';
            if (dryIceInput) {
                dryIceValue = (dryIceInput.value || '').trim(); // Read what the user entered in the visible field.
            }

            // If the UI did not expose a control, fall back to any value already stored on the shipment.
            if (!dryIceValue && shipmentRequest.PackageDefaults && shipmentRequest.PackageDefaults.MiscReference3) {
                dryIceValue = (shipmentRequest.PackageDefaults.MiscReference3 || '').toString().trim();
            }

            // Block shipping if no dry-ice value exists because frozen specimens require that field.
            if (!dryIceValue) {
                client.alert.Danger('Frozen specimens require a Dry Ice Weight value before shipping.');
                throw new Error('Frozen specimens require a Dry Ice Weight value before shipping.');
            }

            // Store the dry-ice value in MiscReference3 so the server-side PreShip hook can convert kilograms to pounds.
            shipmentRequest.PackageDefaults.MiscReference3 = dryIceValue;
        }

        // Step 3: When the shipment is not Frozen, the dry-ice value should not be sent as editable user input.
        if (tempValue !== 'Frozen' && shipmentRequest.PackageDefaults) {
            // Clear out any accidental dry-ice entry so the server only sees it when the temperature truly requires it.
            shipmentRequest.PackageDefaults.MiscReference3 = shipmentRequest.PackageDefaults.MiscReference3 || '';
        }

        // Step 4: Allow the request to continue so server-side rules can enforce the authoritative specimen-return logic.
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
        /*
        * PostBuildShipment runs after ShipExec has built the shipment object from the screen.
        * This hook is the final client-side normalization point before the shipment is handed
        * off to the server. The Marken blueprint needs it to ensure the reference-field mapping
        * is consistent and that UI-entered values are copied into the expected shipment fields.
        *
        * Blueprint requirements fulfilled:
        * 1) Make sure user-entered reference values are mapped to the correct MiscReference and ShipperReference fields.
        * 2) Preserve the Temperature and Biological Sample-related values so SBR can enforce the server rules.
        * 3) Keep the assembled shipment object clean and ready for server-side PreShip processing.
        *
        * How this hook fits into the chain:
        * - PostBuildShipment runs after the screen data is collected but before the request is sent.
        * - It complements NewShipment by ensuring any late UI edits are copied into the object.
        * - SBR PreShip then performs the authoritative validation and conversion.
        *
        * Process flow:
        * Step 1: Confirm the shipment object exists.
        * Step 2: Copy any UI-held dry-ice or biological values into the fields the server expects.
        * Step 3: Leave all hard business rules to the server.
        */
        shipmentRequest = shipmentRequest || this.vm.shipmentRequest; // Use the collected shipment object.
        if (!shipmentRequest) {
            return; // Nothing to normalize.
        }

        // Step 1: Ensure the shipment defaults container exists so field assignments do not fail.
        if (!shipmentRequest.PackageDefaults) {
            shipmentRequest.PackageDefaults = {}; // Create the shipment default container if the template omitted it.
        }

        // Step 2: Copy the visible dry-ice field into MiscReference3 if the user entered it in the UI.
        var dryIceInput = document.querySelector('[name="MiscReference3"], [id*="MiscReference3"], [name*="DryIceWeight"]');
        if (dryIceInput && (dryIceInput.value || '').trim()) {
            shipmentRequest.PackageDefaults.MiscReference3 = (dryIceInput.value || '').trim(); // The server expects this exact field for dry ice processing.
        }

        // Step 3: Ensure the biological sample checkbox value is preserved so SBR can populate PackageExtras.RESTRICTED_ARTICLE_TYPE.
        var biologicalInput = document.querySelector('[name="MiscReference4"], [id*="MiscReference4"], [name*="BiologicalSample"]');
        if (biologicalInput) {
            shipmentRequest.PackageDefaults.MiscReference4 = biologicalInput.checked ? 'true' : 'false'; // Preserve the checkbox state as a boolean-style string.
        }

        // Step 4: Leave the rest of the shipment unchanged and let the server enforce the official shipping rules.
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
