function ClientBusinessRules() {

    this.PageLoaded = function(location) {
        /*
        * PageLoaded is the first client-side hook that runs whenever a ShipExec page loads.
        *
        * Blueprint requirement fulfilled:
        * - "On login, user should automatically be taken to the shipping page."
        * - "When Pickup From/Consignee address IS Canada, hide the Pickup button"
        * - Prepare the returns-oriented workflow for Marken biological specimen shipping.
        *
        * Why this hook exists:
        * - PageLoaded is the right place to initialize page-level behavior because the DOM and ViewModel are available,
        *   but the user has not yet interacted with the shipment form.
        * - This hook complements NewShipment by handling navigation and global UI behavior, while NewShipment handles
        *   shipment defaults.
        *
        * How it interacts with other hooks in the chain:
        * - PageLoaded runs before the user creates a shipment, so it can set the stage for NewShipment and PreShip.
        * - NewShipment applies shipment defaults after this page-level setup.
        * - PreShip performs the final client-side pickup association attempt after the user has edited fields.
        *
        * Process flow:
        * 1. Detect the current route.
        * 2. Redirect the user to the shipping page if they are on the login/root landing page.
        * 3. Inspect the current consignee/pickup country from the shipment request.
        * 4. Hide or show the Pickup button based on whether the country is Canada.
        * 5. Keep the UI in a state that matches the Marken returns workflow.
        */
        var route = (location || '').toString().toLowerCase();
        var vm = this.vm;
        var shipmentRequest = vm && vm.shipmentRequest ? vm.shipmentRequest : null;
        
        // Step 1: Auto-route users to shipping after login/root landing.
        // We do this on page load so the user lands directly in the returns workflow instead of a generic start page.
        if (route === '' || route === '/' || route.indexOf('login') >= 0 || route.indexOf('home') >= 0) {
        // ShipExec thin client implementations differ by deployment, so we try the safest navigation options first.
        if (typeof window !== 'undefined' && window.location && typeof window.location.replace === 'function') {
        window.location.replace('#/shipping');
        } else if (typeof window !== 'undefined' && window.location) {
        window.location.hash = '#/shipping';
        }
        }
        
        // Step 2: Read the current consignee/pickup country if a shipment already exists on the page.
        // This is only for UI control; authoritative validation still happens in SBR PreShip.
        var country = '';
        if (shipmentRequest && shipmentRequest.PackageDefaults && shipmentRequest.PackageDefaults.Consignee) {
        var consignee = shipmentRequest.PackageDefaults.Consignee;
        country = (consignee.Country || consignee.country || '').toString().trim().toUpperCase();
        }
        
        // Step 3: Find the Pickup button in the current view and toggle its visibility.
        // The exact DOM markup can vary by template, so we use several likely selectors.
        var pickupButton = null;
        if (typeof document !== 'undefined') {
        pickupButton = document.querySelector('[data-action="PickupRequest"]') ||
        document.querySelector('[data-action="Pickup"]') ||
        document.querySelector('button[title*="Pickup"]') ||
        document.querySelector('button:contains("Pickup")');
        }
        
        // Step 4: Hide the Pickup button when the pickup-from country is Canada.
        // This matches the blueprint requirement that Canada-origin return workflows should not expose Pickup.
        if (pickupButton) {
        if (country === 'CA') {
        pickupButton.style.display = 'none';
        } else {
        pickupButton.style.display = '';
        }
        }
        
        // Step 5: If this is the first time the page is loading and no shipment exists yet, make sure the UI starts clean.
        // This does not set shipment values; that is the job of NewShipment.
        if (vm && vm.packageIndex == null) {
        vm.packageIndex = 0;
        }
        
    };

    this.NewShipment = function(shipmentRequest) {
        /*
        * NewShipment runs whenever ShipExec creates a brand-new empty shipment.
        *
        * Blueprint requirement fulfilled:
        * - "Set Consignee address to User address values" so the screen behaves as "Pickup From" for returns.
        * - "Set Shipper Reference / Study Reference Code to User Custom2 value"
        * - "Set MiscReference1 / Protocol Number to User Custom1 value"
        * - Prepare the shipment so pickup association can be attempted later for non-Canada returns.
        *
        * Why this hook exists:
        * - NewShipment is the best place to establish default values from the logged-in user's profile.
        * - It keeps the UI consistent before the user begins editing anything.
        *
        * How it interacts with other hooks in the chain:
        * - PageLoaded handles navigation and page-level UI controls.
        * - NewShipment initializes the shipment defaults.
        * - PreShip performs the last client-side pickup automation attempt right before shipping.
        * - SBR PreShip is still the authoritative enforcement layer for lane, service, paperless invoice, and dry ice logic.
        *
        * Process flow:
        * 1. Ensure the shipment and package structures exist.
        * 2. Read the user's profile/custom data values.
        * 3. Copy the user address into the Consignee (Pickup From) fields.
        * 4. Map user custom data into Shipper Reference and MiscReference1.
        * 5. Default Biological Sample and other returns-oriented values if needed.
        * 6. Update the UI so the user sees a ready-to-use returns shipment.
        */
        var vm = this.vm;
        if (!vm) {
        return;
        }
        
        // Step 1: Guarantee a shipment object exists so field assignments are safe.
        var shipmentRequest = vm.shipmentRequest || {};
        vm.shipmentRequest = shipmentRequest;
        shipmentRequest.PackageDefaults = shipmentRequest.PackageDefaults || {};
        shipmentRequest.Packages = shipmentRequest.Packages || [];
        
        // Step 2: Make sure there is at least one package to edit.
        // The blueprint's returns workflow is label-centric, so we default the first package.
        if (shipmentRequest.Packages.length === 0) {
        shipmentRequest.Packages.push({});
        }
        var pkg = shipmentRequest.Packages[0];
        pkg.MiscReference4 = (pkg.MiscReference4 == null || pkg.MiscReference4 === '') ? true : pkg.MiscReference4;
        
        // Step 3: Read user profile data that acts as the return pickup address source.
        // The blueprint says the user's spreadsheet/address data should become the Pickup From address.
        var userInfo = vm.profile && vm.profile.UserInformation ? vm.profile.UserInformation : {};
        var customData = userInfo.CustomData || userInfo.customData || [];
        
        // Step 4: Copy user address fields into the shipment consignee (renamed in the template to Pickup From).
        // We intentionally copy only the common fields needed for a return label.
        shipmentRequest.PackageDefaults.Consignee = shipmentRequest.PackageDefaults.Consignee || {};
        shipmentRequest.PackageDefaults.Consignee.Company = userInfo.Company || userInfo.company || shipmentRequest.PackageDefaults.Consignee.Company || '';
        shipmentRequest.PackageDefaults.Consignee.Contact = userInfo.Contact || userInfo.contact || shipmentRequest.PackageDefaults.Consignee.Contact || '';
        shipmentRequest.PackageDefaults.Consignee.Address1 = userInfo.Address1 || userInfo.address1 || shipmentRequest.PackageDefaults.Consignee.Address1 || '';
        shipmentRequest.PackageDefaults.Consignee.Address2 = userInfo.Address2 || userInfo.address2 || shipmentRequest.PackageDefaults.Consignee.Address2 || '';
        shipmentRequest.PackageDefaults.Consignee.Address3 = userInfo.Address3 || userInfo.address3 || shipmentRequest.PackageDefaults.Consignee.Address3 || '';
        shipmentRequest.PackageDefaults.Consignee.City = userInfo.City || userInfo.city || shipmentRequest.PackageDefaults.Consignee.City || '';
        shipmentRequest.PackageDefaults.Consignee.StateProvince = userInfo.StateProvince || userInfo.stateProvince || shipmentRequest.PackageDefaults.Consignee.StateProvince || '';
        shipmentRequest.PackageDefaults.Consignee.PostalCode = userInfo.PostalCode || userInfo.postalCode || shipmentRequest.PackageDefaults.Consignee.PostalCode || '';
        shipmentRequest.PackageDefaults.Consignee.Country = userInfo.Country || userInfo.country || shipmentRequest.PackageDefaults.Consignee.Country || '';
        shipmentRequest.PackageDefaults.Consignee.Phone = userInfo.Phone || userInfo.phone || shipmentRequest.PackageDefaults.Consignee.Phone || '';
        
        // Step 5: Map the user-level custom values into the reference fields specified by the blueprint.
        // Custom1 -> Protocol Number -> MiscReference1
        // Custom2 -> Study Reference Code -> Shipper Reference
        // Custom3 -> Site Number -> MiscReference2
        var getValueByKey = (typeof client !== 'undefined' && client.getValueByKey) ? client.getValueByKey : null;
        var custom1 = getValueByKey ? getValueByKey('Custom1', customData) : null;
        var custom2 = getValueByKey ? getValueByKey('Custom2', customData) : null;
        var custom3 = getValueByKey ? getValueByKey('Custom3', customData) : null;
        
        // These assignments are the client-side defaulting required by the blueprint.
        pkg.MiscReference1 = (custom1 != null && custom1 !== '') ? custom1 : (pkg.MiscReference1 || '');
        pkg.ShipperReference = (custom2 != null && custom2 !== '') ? custom2 : (pkg.ShipperReference || '');
        pkg.MiscReference2 = (custom3 != null && custom3 !== '') ? custom3 : (pkg.MiscReference2 || '');
        
        // Step 6: Set the default temperature-related state so the user can immediately see the returns workflow.
        // The field itself is rendered by the template/profile options; here we only ensure the shipment starts in a usable state.
        if (pkg.ConsigneeReference == null || pkg.ConsigneeReference === '') {
        pkg.ConsigneeReference = 'Ambient';
        }
        
        // Step 7: Keep the defaults aligned with the blueprint's return-label expectations.
        // These values may also be set by profile defaults, but setting them here improves first-load UX.
        shipmentRequest.PackageDefaults.Description = shipmentRequest.PackageDefaults.Description || 'UN3373 Category B Human Sample';
        shipmentRequest.PackageDefaults.ReturnDelivery = true;
        shipmentRequest.PackageDefaults.SaturdayDelivery = true;
        shipmentRequest.PackageDefaults.Terms = shipmentRequest.PackageDefaults.Terms || 'Prepaid';
        shipmentRequest.PackageDefaults.Service = shipmentRequest.PackageDefaults.Service || 'UPS Express';
        
        // Step 8: Update the ViewModel so the screen refresh reflects the defaults immediately.
        vm.shipmentRequest = shipmentRequest;
        
    };

    this.Keystroke = function(shipmentRequest, vm, event) {
    };

    this.PreLoad = function(loadValue, shipmentRequest, userParams) {
    };

    this.PostLoad = function(loadValue, shipmentRequest) {
    };

    this.PreShip = function(shipmentRequest, userParams) {
        /*
        * PreShip is the final client-side checkpoint before ShipExec sends the shipment to the server.
        *
        * Blueprint requirement fulfilled:
        * - "When Pickup From/Consignee address is NOT Canada, automatically click Pickup Request and Save"
        *   so the Pickup object is associated with the return label.
        * - "When Temperature/ConsigneeReference is set to Frozen, have user enter Dry Ice Weight"
        * - "When Temperature/ConsigneeReference is any other value besides Frozen, Dry Ice Weight should not be editable"
        *
        * Why this hook exists:
        * - This is the user's last chance on the client to finish UI-driven automation before server-side validation begins.
        * - It complements SBR PreShip; the server hook is authoritative, but the client hook improves usability.
        *
        * How it interacts with other hooks in the chain:
        * - PageLoaded controls page-level UI such as hiding the Pickup button for Canada.
        * - NewShipment seeds the shipment defaults.
        * - This PreShip hook performs the best-effort pickup association click/save workflow.
        * - SBR PreShip then validates and enforces the actual business rules regardless of whether the UI automation worked.
        *
        * Process flow:
        * 1. Determine whether the current pickup-from country is Canada.
        * 2. If the country is not Canada, try to click the Pickup Request button and Save.
        * 3. If the country is Canada, do nothing because the Pickup button should be hidden/unused.
        * 4. Keep dry-ice field editability aligned with the selected temperature.
        * 5. Let the server-side PreShip perform the authoritative shipping logic.
        */
        var vm = this.vm;
        var shipmentRequest = vm && vm.shipmentRequest ? vm.shipmentRequest : null;
        
        if (!shipmentRequest || !shipmentRequest.PackageDefaults || !shipmentRequest.PackageDefaults.Consignee) {
        return;
        }
        
        // Step 1: Resolve the pickup-from country from the consignee data.
        var country = (shipmentRequest.PackageDefaults.Consignee.Country || '').toString().trim().toUpperCase();
        
        // Step 2: Best-effort automation only applies when the return is not Canada.
        // Canada shipments do not need the Pickup button, so we leave them alone.
        if (country !== 'CA') {
        if (typeof document !== 'undefined') {
        // Step 3: Try to locate a Pickup Request button from the template.
        var pickupRequestButton = document.querySelector('[data-action="PickupRequest"]') ||
        document.querySelector('button[title*="Pickup Request"]') ||
        document.querySelector('button[aria-label*="Pickup Request"]');
        
        // Step 4: Click the Pickup Request button if it exists.
        // This is the core of the client-side association strategy described in the blueprint.
        if (pickupRequestButton && typeof pickupRequestButton.click === 'function') {
        pickupRequestButton.click();
        }
        
        // Step 5: After triggering the pickup workflow, try to save the shipment so the pickup association persists.
        var saveButton = document.querySelector('[data-action="Save"]') ||
        document.querySelector('button[title*="Save"]') ||
        document.querySelector('button[aria-label*="Save"]');
        
        if (saveButton && typeof saveButton.click === 'function') {
        saveButton.click();
        }
        }
        }
        
        // Step 6: Keep the dry ice field editable only when the temperature is Frozen.
        // This matches the blueprint requirement that Dry Ice Weight is required only for frozen shipments.
        if (typeof document !== 'undefined') {
        var dryIceInput = document.querySelector('[name="MiscReference3"]') || document.querySelector('[data-field="MiscReference3"]');
        var temperature = (shipmentRequest.Packages[0] && shipmentRequest.Packages[0].ConsigneeReference ? shipmentRequest.Packages[0].ConsigneeReference : '').toString().trim();
        
        if (dryIceInput) {
        dryIceInput.readOnly = temperature !== 'Frozen';
        dryIceInput.disabled = temperature !== 'Frozen';
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


{"PageLoaded":"auto-route to /shipping on login/root load; hide/show the Pickup button based on consignee country (hide when CA)","NewShipment":"default Consignee from user address values; map Custom2 to Shipper Reference, Custom1 to MiscReference1, Custom3 to MiscReference2; default Bio Sample true and shipment defaults","PreShip":"best-effort click Pickup Request and Save when Pickup From country is not Canada; keep Dry Ice Weight editable only when temperature is Frozen"}
}
