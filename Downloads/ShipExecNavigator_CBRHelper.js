// ============================================================================
// CBRHelper.js — Single Reusable CBR Helper Object
//
// Wraps all functionality from Combined2.js into a single object returned
// by an IIFE (Immediately Invoked Function Expression).  Private helpers
// live inside the closure and are not accessible from outside.
//
// Usage:
//   var cbr = CBRHelper;                       // grab the object
//   cbr.Utilities.dateTimeStamp();             // call a method
//   cbr.Logger.Log({ Source: 'test' });        // use the logger
//   var s = new cbr.Shipment(shipmentRequest); // create a Shipment
//
// Sections (sub-objects on CBRHelper):
//   Utilities, Logger, UI, DOM, API, Context, Address, References,
//   Notifications, Validation, Services, D2M, Commodities, Orders,
//   History, Manifest, Batch, Profiles, LoadState, Billing,
//   ReturnDelivery, Printing, Files, Camera, LTL, Shipment, PageInit
// ============================================================================
//
// ============================================================================
// HOW TO USE THIS FILE — GUIDE FOR NOVICE PROGRAMMERS
// ============================================================================
//
// WHAT IS THIS FILE?
// ------------------
// CBRHelper.js is a JavaScript "helper library" written for the ShipExec
// shipping application.  It packages ~120 helper functions into a single
// global object called CBRHelper so that any page on the site can use them
// without copy-pasting code.
//
// The whole file is wrapped in one big function that runs immediately (an
// "IIFE" — pronounced "iffy").  That function returns the CBRHelper object.
// Think of it like a toolbox: the box itself is CBRHelper, the drawers are
// sub-objects like CBRHelper.Utilities or CBRHelper.Logger, and the tools
// inside each drawer are the individual functions.
//
// HOW THE CODE IS STRUCTURED
// --------------------------
// CBRHelper
//  ├── PRIVATE (hidden, inside the IIFE — cannot be called from outside)
//  │    ├── _SEL                    — dictionary of jQuery/CSS selectors
//  │    ├── _currentUserContext     — cached logged-in user data
//  │    └── _pollForElement, _buildUserMethodPayload, _decodeResponseData,
//  │        _setInputValue, _getSelectedBatchId, _ensureCommodityArray,
//  │        _buildCommodityForExport, _buildSelectForCountry,
//  │        _closeManifestForShipper, _setDescription,
//  │        _setReturnDeliveryCheckbox
//  │
//  └── PUBLIC (the returned object — callable from anywhere)
//       ├── Utilities   — string/date/array helpers
//       ├── Logger      — console + server logging
//       ├── UI          — spinners, overlays, alert banners
//       ├── DOM         — wait for elements, focus, event detection
//       ├── API         — HTTP calls to the ShipExec thin-client API
//       ├── Context     — read/write the current user and view-model
//       ├── Address     — consignee lookup, address copying
//       ├── References  — shipper/consignee reference field helpers
//       ├── Notifications — ship notification email helpers
//       ├── Validation  — field validation helpers
//       ├── Services    — carrier service selection helpers
//       ├── D2M         — Delivered-to-Mailroom feature helpers
//       ├── Commodities — add/remove/display commodity (goods) lines
//       ├── Orders      — consolidated order number dialog helpers
//       ├── History     — shipment history page helpers
//       ├── Manifest    — end-of-day manifest / close-shipper helpers
//       ├── Batch       — batch processing helpers
//       ├── Profiles    — save/load/apply shipping profiles
//       ├── LoadState   — snapshot and restore state around a Load call
//       ├── Billing     — third-party billing helpers
//       ├── ReturnDelivery — return-delivery (reverse logistics) helpers
//       ├── Printing    — PDF label printing, ZPL label helpers
//       ├── Files       — file upload / paperless document helpers
//       ├── Camera      — browser camera stream and image capture
//       ├── LTL         — Less-Than-Truckload dimension/dry-ice helpers
//       ├── Shipment    — convenience class wrapping a shipment request
//       └── PageInit    — one-time page setup (called on Angular route load)
//
// HOW TO CALL A FUNCTION
// ----------------------
// Pattern:   CBRHelper.<Section>.<methodName>(<arguments>);
//
// Examples:
//   // Get a timestamp string:
//   var stamp = CBRHelper.Utilities.dateTimeStamp();
//
//   // Log something to the console (and optionally the server):
//   CBRHelper.Logger.Log({ Source: 'myFunction()', Message: 'Hello' });
//
//   // Show the loading spinner:
//   CBRHelper.UI.showLoader();
//
//   // Make a server API call:
//   CBRHelper.API.makeUserMethodRequest('GetOrderData', { OrderId: 123 },
//     thinClient.httpClient, function(result) { console.log(result); });
//
//   // Wrap a shipment request for easy CustomData access:
//   var s = new CBRHelper.Shipment(vm.currentShipment);
//   console.log(s.UserRef1);  // reads Custom1 from origin address
//
// COMMON PATTERNS YOU'LL SEE
// --------------------------
// 1. Promises:
//      Some functions return a Promise — an object that represents a future
//      value.  Use .then(function(result) { ... }) to handle the result.
//      Example: CBRHelper.Camera.captureImage('video').then(function(b64) { ... });
//
// 2. Callbacks:
//      Some functions accept a callback parameter (a function you pass in
//      that gets called when the work is done).
//      Example: CBRHelper.Batch.getBatches(apiRequest, companyId)
//               .then(function(batches) { vm.batches = batches; });
//
// 3. 'vm' (view-model):
//      Many functions take a 'vm' argument.  This is the AngularJS controller
//      object that holds all the data bound to the HTML page.  You get it
//      from your controller: var vm = this;  or  $scope.vm = this;
//
// 4. 'httpClient' vs 'apiRequest':
//      httpClient  — an async function that returns a Promise (modern style).
//      apiRequest  — a synchronous or jQuery-deferred function (older style).
//      Both are obtained from the ShipExec thinClient object.
//
// KEY CONCEPTS FOR NOVICES
// ------------------------
// • IIFE: The entire file is a self-executing function.  It runs once when
//   the browser loads the script and the result is stored in CBRHelper.
//
// • Closure: Private variables (_SEL, _currentUserContext, etc.) are
//   "captured" inside the IIFE and cannot be seen or changed from outside.
//
// • Base64: Several functions encode/decode binary data (PDFs, images) as
//   Base64 strings — a text-safe way to transmit binary over HTTP/JSON.
//
// • jQuery ($): Used throughout for DOM manipulation.  $('selector') finds
//   HTML elements; .val(), .click(), .prop() etc. interact with them.
//
// • AngularJS (ng-model): The app uses Angular 1.x.  Some helpers call
//   .trigger('change') after setting a value so Angular's data binding
//   picks up the change.
//
// ============================================================================
// COMPLETE PUBLIC METHOD REFERENCE
// ============================================================================
//
// Each entry shows:  Section.methodName(parameters)
//                    → what it does
//
// ── UTILITIES ────────────────────────────────────────────────────────────────
//
//   Utilities.getUniqueArrayFromString(str, delimiter)
//     → Splits a delimited string and returns a de-duplicated array of
//       trimmed values.
//
//   Utilities.getUniqueCSVStringFromArray(arr)
//     → Converts an array to a comma-separated string of unique values.
//
//   Utilities.getUniqueCSVStringFromString(str, delimiter)
//     → Convenience: splits a string, de-duplicates, and rejoins as CSV.
//
//   Utilities.compareByKey(key)
//     → Returns a sort comparator function for sorting an array of objects
//       by a named property.  Use with Array.sort().
//
//   Utilities.returnPropertyValue(value)
//     → Returns the value if it is truthy, or an empty string if falsy.
//       Safe alternative to using 'value || ""' everywhere.
//
//   Utilities.getRandomAlphaNumericString()
//     → Generates a short random alphanumeric string (useful as a
//       temporary unique ID).
//
//   Utilities.dateTimeStamp()
//     → Returns the current local date/time as a human-readable string
//       (e.g. '1/15/2025, 10:30:45 AM').
//
//   Utilities.todayString()
//     → Returns today's date as a short locale string (e.g. '1/15/2025').
//
//   Utilities.currentShipdate()
//     → Returns today's date as an object with Year, Month, Day properties
//       (the format the ShipExec API expects for ship dates).
//
//   Utilities.translateUnitOfMeasurement(unitString)
//     → Maps verbose unit strings (e.g. 'Pounds') to their short API
//       equivalents (e.g. 'LB').
//
// ── LOGGER ───────────────────────────────────────────────────────────────────
//
//   Logger.LogLevel
//     → An enum-like object with constants: DEBUG, INFO, WARN, ERROR.
//       Pass one of these as the Level property of a log entry.
//
//   Logger.setServerDebugMode(enabled)
//     → Enables or disables sending log entries to the server at runtime.
//
//   Logger.Log(entry)
//     → The main logging function.  Accepts a log-entry object { Source,
//       Message, Level, Error } and writes to the console.  If server debug
//       mode is on, also POSTs to the server.
//
//   Logger.logStartMethod(methodName)
//     → Prints a standardised 'STARTING ...' banner — call at the top of
//       any function to make log output easier to follow.
//
//   Logger.logMethodInfo(message)
//     → Prints an indented informational message — call inside a function
//       to trace what it is doing.
//
//   Logger.decodeReturnString(encodedString)
//     → Decodes an HTML-entity-encoded JSON string returned by older API
//       endpoints and parses it to a JavaScript object.
//
// ── UI ───────────────────────────────────────────────────────────────────────
//
//   UI.showLoader()
//     → Makes the page's loading spinner visible while an async operation
//       is running.
//
//   UI.hideLoader()
//     → Hides the loading spinner and removes the overlay.
//
//   UI.showOverlay()
//     → Adds a semi-transparent dark overlay over the entire page to
//       block interaction during a long operation.
//
//   UI.hideOverlay()
//     → Hides the overlay without removing it from the DOM, so it can
//       be shown again quickly.
//
//   UI.showErrorAlert(message, containerId)
//     → Injects a dismissible Bootstrap error banner into the specified
//       container element.
//
//   UI.showModalAlert(title, message, $modal)
//     → Populates an existing Bootstrap modal with a title and message,
//       then opens it.
//
//   UI.setFocus(selector)
//     → Moves keyboard focus to the element matching the CSS selector.
//
// ── DOM ──────────────────────────────────────────────────────────────────────
//
//   DOM.waitForElement(selector, focus, interval, timeout, callback)
//     → Polls the DOM until an element matching the selector appears,
//       then optionally focuses it and calls the callback.
//
//   DOM.waitForSelectOptions(selector, interval, timeout)
//     → Waits until a <select> element has been populated with options,
//       then returns a Promise that resolves with the <select> element.
//
//   DOM.isEventAttached(element, eventName, handlerFn)
//     → Checks whether a specific event handler function is already bound
//       to an element — prevents duplicate event bindings.
//
// ── API ──────────────────────────────────────────────────────────────────────
//
//   API.thinClientApiRequestFromConfig(method, data)
//     → Reads the API URL and auth token from window config values and
//       makes a POST request.  Returns a Promise.
//
//   API.thinClientApiRequest(url, authToken, method, data)
//     → Makes a POST to a ShipExec thin-client API endpoint with the
//       given credentials.  Returns a Promise.
//
//   API.makeUserMethodRequest(action, data, httpClient, callback)
//     → The primary way to call ShipExec's UserMethod endpoint.  Builds
//       the request payload, posts it, decodes the response, and calls
//       the callback with the decoded result.
//
//   API.ajaxGet(url)
//     → A minimal synchronous GET request that returns the response body
//       as a string.  Use sparingly — synchronous calls block the browser.
//
// ── CONTEXT ──────────────────────────────────────────────────────────────────
//
//   Context.setCurrentUserContext(userContext)
//     → Populates the private _currentUserContext store with the current
//       user's data (name, role, shipper list, etc.).
//
//   Context.getCurrentUserContext()
//     → Returns the cached user context object set by setCurrentUserContext.
//
//   Context.getUserContext(vm)
//     → Reads user context directly from a view-model instance (useful
//       when the global cache has not been set yet).
//
//   Context.fetchUserContext(httpClient, callback)
//     → Retrieves user context from the server for the currently logged-in
//       user and calls the callback with the result.
//
//   Context.setMailroomOrTraveler(vm)
//     → Returns an object describing whether the active shipper account
//       is a Mailroom account or a Traveler account.
//
//   Context.getCustomDataValue(customData, key)
//     → Finds the value for a named key in a CustomData array
//       (the key-value pairs attached to shippers, users, etc.).
//
//   Context.getCustomField(fieldName, source, shipmentRequest)
//     → Reads a custom field from either the origin address ('User') or
//       the consignee address ('To') of a shipment request.
//
//   Context.setCurrentViewModel(vm)
//     → Stores the Angular vm on window._currentViewModel so that
//       functions without a vm parameter can still access it.
//
// ── ADDRESS ──────────────────────────────────────────────────────────────────
//
//   Address.bindConsigneeTabSearch(vm, tabSelector, inputSelector)
//     → Triggers an address-book search when the user clicks a
//       consignee tab, pre-filling the search field.
//
//   Address.bindCodeInputLookup(inputSelector, vm)
//     → Triggers an address-book lookup when the user types a code
//       into a designated input field.
//
//   Address.copyOriginToReturnAddress(shipmentRequest)
//     → Copies the shipper's origin address into the return address
//       fields of the shipment request.
//
//   Address.checkForMatchingConsignee(shipmentRequest, profileShippers,
//                                     shipperSymbol, callback)
//     → Compares the consignee address currently on the shipment against
//       a list of known shipper addresses and calls the callback with the
//       match result.
//
//   Address.syncShipNotificationEmail(shipmentRequest)
//     → Copies the ship-notification email from the shipment data into
//       the DOM input so Angular's binding stays in sync.
//
//   Address.setConsigneeByReference(vm, referenceMap, referenceKey)
//     → Looks up a consignee address from a map by a reference key and
//       applies it to the current shipment.
//
// ── REFERENCES ───────────────────────────────────────────────────────────────
//
//   References.setShipperReferenceFromCustomData(vm, customDataKey)
//     → Reads a custom data field from the current user and writes its
//       value into the ShipperReference field.
//
//   References.updateReferencesWithOrderNumber(shipmentRequest, orderNumber)
//     → Appends an order number to both the ShipperReference and
//       ConsigneeReference fields.
//
//   References.applyCompoundShipperReference(shipmentRequest, parts)
//     → Builds a combined reference string from multiple parts and
//       writes it into the ShipperReference field.
//
//   References.stampMiscReference15(shipmentRequest, value)
//     → Writes a value into MiscReference15 for all packages in the
//       shipment.
//
//   References.applyOsuReferences(vm, parts)
//     → Builds ShipperReference and ConsigneeReference fields according
//       to an OSU-specific business rule combining multiple data parts.
//
// ── NOTIFICATIONS ─────────────────────────────────────────────────────────────
//
//   Notifications.setNotificationDefaults(pkg, email)
//     → Enables all three notification types (ship, exception, delivery)
//       on a package and sets the notification email address.
//
//   Notifications.bindNotificationCheckboxes(vm, getUserName)
//     → Wires up three notification checkboxes to enable/disable each
//       notification type, and populates the email field with the user's
//       address when any checkbox is checked.
//
//   Notifications.getShipNotificationEmail()
//     → Reads the current value from the ship notification email input
//       field in the DOM.
//
//   Notifications.persistNotificationEmail(shipmentRequest)
//     → Copies the ship notification email from the DOM input into the
//       shipment request object so it is included when shipped.
//
//   Notifications.setReturnDeliveryAddressEmail(email)
//     → Writes an email address into the return-delivery email input
//       field in the DOM.
//
//   Notifications.getReturnDeliveryAddressEmail()
//     → Reads the return-delivery email address from the DOM input.
//
// ── VALIDATION ────────────────────────────────────────────────────────────────
//
//   Validation.validateAgainstFieldOptions(value, allowedValues, fieldName)
//     → Checks that a value is in an allowed list and throws a user-
//       friendly error if it is not.
//
//   Validation.validateClientMatter(shipmentRequest)
//     → Validates the ShipperReference ("Client Matter") field against
//       business rules (format, length, allowed characters).
//
//   Validation.initCustomStateProvinceSelect(stateProvinceSelector,
//                                            usStates, canadianProvinces,
//                                            changeCallback)
//     → Replaces the default state/province text input with a drop-down
//       populated from the provided US state and Canadian province lists.
//
// ── SERVICES ──────────────────────────────────────────────────────────────────
//
//   Services.autoSelectServiceByWeight(shipmentRequest,
//                                      freightThresholdLbs,
//                                      expressServiceSymbol,
//                                      freightServiceSymbol)
//     → Examines every package's weight and picks the express or freight
//       service depending on whether the total exceeds the threshold.
//
//   Services.buildFilteredServiceList(profile, serviceCodesCSV,
//                                     getServiceSymbol)
//     → Returns only the services from the profile whose symbol appears
//       in the CSV list — useful for restricting available services.
//
//   Services.selectServiceBySortIndex(rateResults)
//     → Returns the service with SortIndex === 0 from the rate results
//       (the carrier's recommended / default service).
//
// ── D2M (DELIVERED-TO-MAILROOM) ────────────────────────────────────────────────
//
//   D2M.isCurrentShipperD2MEnabled(authorizedShippers)
//     → Checks whether the currently selected shipper is in the D2M-
//       authorised shipper list.
//
//   D2M.isCurrentUserD2MEnabled(customData, d2mCustomDataKey, enabledValue)
//     → Checks whether the logged-in user's CustomData marks them as
//       D2M-enabled.
//
//   D2M.isCurrentShipmentD2M(config)
//     → The main gate: returns true only when ALL THREE conditions are
//       met (shipper, user, and shipment all D2M-enabled).
//
//   D2M.enableD2MShipping(apiRequest, userContext, vm)
//     → Calls the server-side EnableD2MShipping UserMethod and applies
//       the returned configuration to the current view-model.
//
//   D2M.hideD2MCheckbox(d2mTemplateId)
//     → Waits for the ship-date element to appear (indicating Angular
//       has finished rendering) then hides the D2M checkbox element.
//
// ── COMMODITIES ────────────────────────────────────────────────────────────────
//
//   Commodities.buildCommodityContentObject(data)
//     → Converts a raw order line item from the server into the
//       CommodityContent object format the ShipExec API expects.
//
//   Commodities.addCommodityToPackage(shipmentRequest, packageIndex,
//                                     commodity, refreshFn)
//     → Pushes a commodity into a package's CommodityContents array
//       and calls the refresh function to update the UI.
//
//   Commodities.removeCommodityFromPackage(shipmentRequest, packageIndex,
//                                          refreshFn)
//     → Removes the last commodity from a package and refreshes the UI.
//
//   Commodities.refreshCommodityDisplay()
//     → Forces the AngularJS commodity tab to re-render by simulating
//       a tab click and pagination button click (works around Angular's
//       change detection not seeing external array mutations).
//
//   Commodities.showAssignGoodsPane(context, rawGoods)
//     → Displays the "Assign Goods to Box" side panel and populates it
//       with a list of scannable order line items.
//
//   Commodities.moveGoodToBox(context, $li, destBox)
//     → Moves a commodity item from one package to another in response
//       to a user drag-and-drop or button click.
//
//   Commodities.consolidateInternationalCommodities(shipmentRequest)
//     → Merges commodity entries with the same description and country
//       of origin into single lines for international shipments.
//
//   Commodities.addToCommodityList(vm, cellsOrBundle, packageIndex,
//                                  isBundle)
//     → Adds a commodity from either a table row (array of cells) or a
//       pre-built bundle object to the package's commodity list.
//
// ── ORDERS ────────────────────────────────────────────────────────────────────
//
//   Orders.showConsolidatedOrderDialog()
//     → Opens the "Consolidate Shipments" modal so the user can add
//       order numbers to a multi-order shipment.
//
//   Orders.addOrderNumberToList()
//     → Adds a new order number typed into #textOrderNumber to the
//       pending consolidation list.
//
//   Orders.saveConsolidatedOrderNumbers(makeRequest, processCallback)
//     → Collects all 'not-processed' orders from the list, sends them
//       to the server, and calls processCallback with the response.
//
//   Orders.processOrderData(orderDataArray)
//     → Iterates the server's order response and, for each order, adds
//       its line items to the commodity list and totals the weight/value.
//
//   Orders.closeDialogWithoutSaving()
//     → Clears the consolidation list and dismisses the modal without
//       sending anything to the server.
//
//   Orders.initConsolidatedOrdersButton(logToServer)
//     → Injects the "Consolidate Orders" button into the page and wires
//       up all the order dialog event handlers.
//
// ── HISTORY ───────────────────────────────────────────────────────────────────
//
//   History.filterHistoryByUser(searchCriteria, userId, operator)
//     → Adds a WHERE clause to a search criteria object so results are
//       limited to shipments created by the specified user.
//
//   History.filterHistoryByBranch(searchCriteria, customDataKey,
//                                  customData, getValueByKey)
//     → Adds a WHERE clause filtering results by a branch/department
//       value stored in the user's CustomData.
//
//   History.enforceHistoryDateRange(vm, searchCriteria, daysBack)
//     → Sets the start date on the view-model and search criteria so
//       the history grid never shows records older than daysBack days.
//
//   History.anonymizeApportionedTotals(packages, currentProfile,
//                                      adminProfileName)
//     → Hides the ApportionedTotal (shipping cost) for non-admin users
//       by replacing it with a dash.
//
//   History.maskTrackingNumbers()
//     → Partially obscures 18-digit tracking numbers in the history
//       grid, showing only the first 3 and last 9 characters.
//
//   History.restrictPrintByAge(packages, maxDays)
//     → Marks packages as un-printable if their ship date is older than
//       maxDays, preventing re-printing of stale labels.
//
//   History.addTrackingLinks()
//     → Wraps raw tracking numbers in the history table with clickable
//       carrier tracking URLs.
//
//   History.applyBulkFilter(filterCriteria)
//     → Hides rows in the batch detail table that don't match the
//       filter criteria.  Blank criteria fields are ignored.
//
//   History.clearBulkFilter()
//     → Restores all hidden rows and clears filter input fields.
//
//   History.initHistoryPage(vm, thinClient)
//     → Sets up the history page's report selector and search trigger
//       on first load.
//
//   History.enableHistoryDuplicate(cbr)
//     → Intercepts clicks on duplicate-action links in the history
//       table and stores the shipment JSON for later use.
//
//   History.applyHistoryDuplicate(cbr, vm, shipmentRequest,
//                                  defaultDescription)
//     → Applies a previously stored historical shipment as the current
//       shipment, resetting fields that should not be duplicated.
//
// ── MANIFEST ──────────────────────────────────────────────────────────────────
//
//   Manifest.closeAllShippers(clientService, allShippers,
//                              selectedCarrierSymbol, companyId, showAlert)
//     → Closes the manifest for every shipper account, calling the
//       server once per shipper and showing a combined result alert.
//
//   Manifest.closeAllShippersAndCarriers(clientService, allShippers,
//                                         allCarriers, companyId, showAlert)
//     → Closes manifests for every combination of shipper × carrier and
//       displays a single combined success/failure alert.
//
//   Manifest.displayCloseManifestResult(response, showAlert, isAllCarriers)
//     → Shows the appropriate success or error alert for a manifest
//       close operation.
//
//   Manifest.processEndOfDay(shipmentRequest, httpClient, $modal)
//     → Submits an End-of-Day (EOD) action for the current shipment,
//       telling the carrier all pickups for the day are complete.
//
// ── BATCH ─────────────────────────────────────────────────────────────────────
//
//   Batch.getBatches(apiRequest, companyId)
//     → Fetches the list of available batches from the server
//       synchronously (result needed immediately for a drop-down).
//
//   Batch.getSelectedBatchName()
//     → Returns the batch ID of the currently selected option in the
//       batch drop-down.
//
//   Batch.bindCustomBatchProcessing(client)
//     → Wires up the custom batch processing button so all batch jobs
//       are handled before the user navigates away.
//
// ── PROFILES ──────────────────────────────────────────────────────────────────
//
//   Profiles.loadShippingProfiles(httpClient, callback)
//     → Fetches the list of saved shipping profiles from the server and
//       calls the callback with the decoded array of profile objects.
//
//   Profiles.saveOrDeleteShippingProfile(action, shipmentRequest,
//                                         profileName, selService,
//                                         httpClient, callback)
//     → Saves ('S') or deletes ('D') a shipping profile on the server
//       and calls the callback with the updated profile list.
//
//   Profiles.applyShippingProfile(vm, shippingProfiles, profileName)
//     → Loads a named profile onto the current shipment, overwriting
//       service, package defaults, and reference fields.
//
//   Profiles.createReturnLabelFromPrevious(vm)
//     → Sets up the current shipment as a return based on the most
//       recently shipped outbound shipment stored on the view-model.
//
// ── LOADSTATE ─────────────────────────────────────────────────────────────────
//
//   LoadState.capturePreLoadState(shipmentRequest)
//     → Snapshots the shipper, batch name, and batch ID from the
//       shipment before a Load operation changes them.  Returns the
//       snapshot object to pass to restorePostLoadState.
//
//   LoadState.restorePostLoadState(shipmentRequest, savedState)
//     → Writes the pre-load shipper and batch name back into the
//       shipment after a Load operation has completed.
//
//   LoadState.showPostLoadError(shipmentRequest)
//     → Checks whether the Load operation returned an error and shows
//       an alert to the user if it did.
//
//   LoadState.showPostLoadUserDataAlert(shipmentRequest, $alertModal,
//                                       $newShipModal)
//     → Displays a UserData1 message to the user after a Load, using a
//       modal dialog.  A leading '~' signals a non-blocking notice.
//
//   LoadState.restoreConsigneeEmail(shipmentRequest)
//     → Writes the consignee email from the shipment request back into
//       the DOM input after Angular's Load may have cleared it.
//
// ── BILLING ───────────────────────────────────────────────────────────────────
//
//   Billing.applyThirdPartyBillingRules(shipmentRequest, isMemberOfEu)
//     → Decides whether third-party billing should be enabled based on
//       carrier, service, and consignee country rules.
//
//   Billing.setThirdPartyBilling(shipmentRequest)
//     → Enables the third-party billing UI controls and writes the
//       hard-coded third-party billing address into the shipment.
//
//   Billing.disableThirdPartyBillingElements()
//     → Unchecks the 3PB checkbox and disables the billing fields
//       whenever the conditions for 3PB are no longer met.
//
// ── RETURNDELIVERY ────────────────────────────────────────────────────────────
//
//   ReturnDelivery.loadReturnAddressFromShipper(vm)
//     → Waits for the second 'company' input to appear and populates
//       return address fields from the current shipper's profile.
//
//   ReturnDelivery.setReturnDeliveryConfiguration(vm)
//     → Configures the form for a return-delivery workflow: checks the
//       return-delivery checkbox and fills in the return address.
//
//   ReturnDelivery.clearReturnDeliveryConfiguration()
//     → Resets all return-delivery fields to their defaults.  Called
//       when the user switches away from a return-delivery workflow.
//
//   ReturnDelivery.setReturnDeliveryInsurance(enabled, shipmentRequest)
//     → Enables the "insured" option for return-delivery shipments to
//       non-US destinations.
//
//   ReturnDelivery.onShipperChangeD2M(currentShipper, d2mShipperSymbol,
//                                     isD2MCapable, vm)
//     → Updates return-delivery settings when the user changes shipper
//       on a D2M-enabled page.
//
// ── PRINTING ──────────────────────────────────────────────────────────────────
//
//   Printing.printTravelerLabel(shipmentRequest, apiRequest)
//     → Submits the shipment to the server, receives a PDF label back,
//       and opens it in a new browser tab.
//
//   Printing.prependZplDefaults(doc, expectedSymbol)
//     → Inserts standard ZPL printer initialisation commands before the
//       raw ZPL label data (for Zebra label printers).
//
//   Printing.downloadFile(fileName, httpClient)
//     → Fetches a file from the server and triggers a browser Save
//       dialog using a hidden auto-clicked <a> element.
//
// ── FILES ─────────────────────────────────────────────────────────────────────
//
//   Files.uploadCarrierLabelFile(fileInputSelector, onReady)
//     → Reads a file chosen in an <input type="file"> and calls onReady
//       with the file's Base64 content (prefix stripped).
//
//   Files.uploadReplacementFile(file, serverKey, httpClient)
//     → Reads a File object and sends its Base64 content to the server
//       under the given key, replacing the existing server-side file.
//
//   Files.bindPaperlessFileInput(fileInputSelector, vmInstance)
//     → Wires up a file input so that when a file is selected, its
//       Base64 content is stored on vm.paperless for later submission.
//
// ── CAMERA ────────────────────────────────────────────────────────────────────
//
//   Camera.startVideoStream(videoElementId, videoModal)
//     → Requests camera access and streams it into a <video> element,
//       restoring the user's last saved zoom level.
//
//   Camera.captureImage(videoElementId)
//     → Grabs a single frame from the live video stream and returns a
//       Promise that resolves with a Base64-encoded PNG string.
//
//   Camera.stopVideoStream(videoElementId)
//     → Stops all camera tracks, releases the hardware, and disconnects
//       the <video> element from the stream.
//
// ── LTL (LESS-THAN-TRUCKLOAD) ─────────────────────────────────────────────────
//
//   LTL.matchLtlDimensions(length, width, height)
//     → Maps a package's L×W×H to a standardised dimension string from
//       the built-in dimension table (e.g. '48x40x12').
//
//   LTL.configureLtlPackage(shipmentRequest, packageIndex, nmfcDescription)
//     → Applies LTL-specific settings to one package: appends the
//       dimension string to the BOL comment, sets the NMFC description,
//       copies the BOL number, and sets the pallet container code.
//
//   LTL.configureDryIce(shipmentRequest)
//     → Applies dry-ice shipping settings to all packages that have
//       the dry-ice indicator set.  Throws if multi-package or missing
//       weight.
//
// ── SHIPMENT (CLASS) ──────────────────────────────────────────────────────────
//
//   new CBRHelper.Shipment(shipmentRequest)
//     → Creates a Shipment instance wrapping the given request object.
//       Provides computed UserRef1–10 and ToRef1–10 properties that
//       read CustomData fields without manual calls to getCustomField.
//
//   shipmentInstance.GetCustom(fieldName, source)
//     → Reads a named custom field from 'User' (origin) or 'To'
//       (consignee) CustomData.
//
//   shipmentInstance.UserRef1 … UserRef10  (computed, read-only)
//     → Shorthand for GetCustom('Custom1'–'Custom10', 'User').
//
//   shipmentInstance.ToRef1 … ToRef10  (computed, read-only)
//     → Shorthand for GetCustom('Custom1'–'Custom10', 'To').
//
// ── PAGEINIT ──────────────────────────────────────────────────────────────────
//
//   PageInit.onPageLoaded(location, vm, options)
//     → Entry point for page-specific initialisation, called from the
//       Angular controller when the route finishes loading.
//
//   PageInit.focusOnLoadInput()
//     → Waits for the load-value input to appear and focuses it, then
//       calls initializeCustomElements.
//
//   PageInit.initializeCustomElements()
//     → Runs element-level setup after the page loads (e.g. unchecks
//       "Do Not Rate Shop" so it never starts checked).
//
//   PageInit.addIorSameAsConsigneeCheckbox(iorBtnSelector, onChecked)
//     → Injects a "Same As Consignee" checkbox before the IOR
//       (Importer of Record) button and wires its change event.
//
//   PageInit.hideCommodityModalTabs($commodityModal, hideIndexes)
//     → Hides specific tabs inside the commodity modal by their
//       zero-based index.
//
//   PageInit.loadCostCenter(vm, customKey)
//     → Reads a cost-centre code from the user's CustomData and
//       pre-fills + disables the ShipperReference field with it.
//
// ============================================================================
// END OF REFERENCE SECTION — implementation begins below
// ============================================================================

// CBRHelper is the single global entry point for all shipping helper logic.
// It is built using an IIFE (Immediately Invoked Function Expression) —
// a function that executes immediately and returns an object.
//
// Why an IIFE?
//   • Every 'var' declared inside the function is PRIVATE — external code
//     cannot read or change it, preventing accidental interference.
//   • The 'return { ... }' at the bottom produces the PUBLIC object that
//     callers interact with (e.g. CBRHelper.Logger.Log(...)).
var CBRHelper = (function () {
  // 'use strict' opts in to stricter JavaScript parsing rules.
  // It catches common mistakes early — for example, assigning to a variable
  // you forgot to declare will throw an error instead of silently creating
  // an accidental global variable.
  'use strict';

  // ==========================================================================
  // PRIVATE — shared selectors, helpers, and state
  // ==========================================================================

  // _SEL is a lookup table of jQuery/CSS selectors for DOM elements used
  // across many functions in this file.
  //
  // Why store selectors here?
  //   • Single source of truth: if the HTML attribute ever changes, you fix it
  //     in ONE place instead of hunting through dozens of functions.
  //   • Readability: call sites use short names like _SEL.shipperRef instead
  //     of a long, opaque attribute string.
  //
  // These selectors use AngularJS 'ng-model' attributes because ShipExec's
  // UI is built on Angular 1.x.  jQuery finds elements by matching the
  // attribute value in square brackets, e.g. input[ng-model="..."].
  var _SEL = {
    // The primary reference text box on a package (ShipperReference field)
    shipperRef:          'input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]',
    // A secondary free-text reference field (Misc Reference 7)
    miscRef7:            'input[type=text][ng-model="vm.currentShipment.Packages[vm.packageIndex].MiscReference7"]',
    // Email address that receives the ship notification when the label is created
    shipNotifEmail:      'input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipNotificationAddressEmail"]',
    // Email address used for return-delivery confirmations
    returnDeliveryEmail: 'input[ng-model="vm.currentShipment.Packages[vm.packageIndex].ReturnDeliveryAddressEmail"]',
    // Description field when the UI renders it as a multi-line <textarea>
    descriptionTextarea: 'textarea[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]',
    // Description field when the UI renders it as a single-line <input>
    descriptionInput:    'input[ng-model="vm.currentShipment.Packages[vm.packageIndex].Description"]',
    // The "Third Party Billing" checkbox — enables billing to a different account
    thirdPartyCheckbox:  'input[type=checkbox][ng-model="vm.currentShipment.PackageDefaults.ThirdPartyBilling"]',
    // The button that opens the third-party address form (disabled unless checkbox is checked)
    thirdPartyButton:    'input[type=button][ng-disabled="!vm.currentShipment.PackageDefaults.ThirdPartyBilling"]',
    // Weight units drop-down (e.g. LB or KG)
    weightUnits:         'select[ng-model="vm.currentShipment.Packages[vm.packageIndex].Weight.Units"]',
    // Dimension units drop-down (e.g. IN or CM)
    dimensionUnits:      'select[ng-model="vm.currentShipment.Packages[vm.packageIndex].Dimensions.Units"]',
    // Commercial Invoice method drop-down (required for international shipments)
    invoiceMethod:       'select[ng-model="vm.currentShipment.Packages[vm.packageIndex].CommercialInvoiceMethod"]',
    // The consignee (recipient) address component element on the page
    consignee:           'name-address[nameaddress="vm.currentShipment.PackageDefaults.Consignee"]',
    // The email input that lives inside the consignee address component
    consigneeEmail:      'input[ng-model="nameaddress.Email"]',
    // The shipper drop-down — Angular calls vm.shipperChange() when it changes
    shipperChange:       'select[ng-change="vm.shipperChange()"]',
    // The ship-date picker input
    shipDate:            "[ng-model='vm.startshipdate.value']",
    // The "load value" text box on the Batch / Load page
    loadValue:           'input[type=text][ng-model="vm.loadValue"]',
    // The currently selected option inside the batch drop-down list
    batchSelect:         '#cboBatches select option:selected'
  };

  // _currentUserContext is the private store for the logged-in user's identity.
  // It is populated once by Context.setCurrentUserContext() and then read by
  // any function that needs to include user info in a server request
  // (e.g. CompanyId, UserId).
  var _currentUserContext = {};

  // ── Private helper functions ──────────────────────────────────────────────

  // _pollForElement — waits for a CSS selector to appear in the DOM.
  //
  // Why is this needed?
  //   ShipExec uses AngularJS, which renders HTML elements asynchronously.
  //   When you call a function, the element you need may not exist in the
  //   page yet.  This helper keeps checking on a timer until either:
  //     a) the element appears  → the Promise resolves (success), or
  //     b) the timeout expires  → the Promise rejects (failure).
  //
  // What is a Promise?
  //   A Promise represents a future value.  You chain .then() on it to
  //   run code when it succeeds, and .catch() (or try/catch with await)
  //   to handle failure.
  //
  // @param {string} selector       - jQuery CSS selector to look for
  // @param {number} timeoutSeconds - max seconds to wait before giving up (default 30)
  // @param {number} intervalMs     - milliseconds between each check (default 50)
  // @returns {Promise<jQuery>}     - resolves with the matched jQuery element
  //
  // Examples:
  //   _pollForElement('#divResults', 10, 100).then(function($el) { $el.show(); });
  //   _pollForElement('input[name="code"]', 5).catch(function(err) { console.error(err.message); });
  function _pollForElement(selector, timeoutSeconds, intervalMs) {
    // Apply defaults: the || operator returns the right side when the left is falsy
    timeoutSeconds = timeoutSeconds || 30;
    intervalMs     = intervalMs || 50;
    // Create a new Promise.  The function receives two callbacks:
    //   resolve(value) — call this to signal SUCCESS with a result value
    //   reject(reason) — call this to signal FAILURE with an error
    return new Promise(function (resolve, reject) {
      var elapsed  = 0;  // total milliseconds spent waiting so far
      // setInterval fires the callback every `intervalMs` ms until stopped
      var interval = setInterval(function () {
        var $el = $(selector);  // try to find the element right now
        if ($el.length) {
          // The element exists — stop the timer and signal success
          clearInterval(interval);
          resolve($el);
          return;
        }
        elapsed += intervalMs;  // add one tick to the running total
        if (elapsed >= timeoutSeconds * 1000) {
          // Waited too long — stop the timer and signal failure
          clearInterval(interval);
          reject(new Error('Timeout waiting for: ' + selector));
        }
      }, intervalMs);
    });
  }

  // _buildUserMethodPayload — wraps a request payload in the envelope
  // shape that ShipExec's UserMethod API endpoint expects.
  //
  // ShipExec's /api/ShippingService/UserMethod endpoint requires:
  //   { Data: '<JSON string>', UserContext: <object> }
  //
  // JSON.stringify converts a JavaScript object into a JSON text string,
  // which is what the server deserialises on the other end.
  //
  // @param {object} payload     - the actual request data to send
  // @param {object} userContext - (optional) current user identity; omit for
  //                               requests that don't need user context
  // @returns {object}           - the wrapped payload ready for $.ajax
  //
  // Examples:
  //   var p = _buildUserMethodPayload({ ServerMethod: 'GetOrders', MethodData: 'ORD-1' }, { CompanyId: 1, UserId: 5 });
  //   var p = _buildUserMethodPayload({ Action: 'EOD' }); // no userContext — omit the second argument
  function _buildUserMethodPayload(payload, userContext) {
    var obj = { Data: JSON.stringify(payload) };  // serialise payload to a JSON string
    // Only attach UserContext if a value was explicitly provided
    // (checking !== undefined rather than truthiness lets callers pass null)
    if (userContext !== undefined) obj.UserContext = userContext;
    return obj;
  }

  // _decodeResponseData — decodes a Base64-encoded JSON string from the server.
  //
  // ShipExec returns some response payloads as Base64 to safely transmit
  // binary or special-character data over HTTP.
  //
  //   atob(string) — built-in browser function that decodes Base64 → plain text
  //   JSON.parse() — converts the resulting JSON text into a JS object
  //
  // @param {string} base64Data - the Base64-encoded JSON string from the server
  // @returns {object}          - the decoded JavaScript object
  //
  // Examples:
  //   var orders = _decodeResponseData(response.Data); // response.Data is a Base64 string
  //   var profile = _decodeResponseData(ret.Data); console.log(profile.Name);
  function _decodeResponseData(base64Data) {
    return JSON.parse(atob(base64Data));
  }

  // _setInputValue — sets the value of a jQuery-selected input/select element
  // AND fires the 'change' event on it.
  //
  // Why trigger 'change'?
  //   AngularJS watches elements for DOM events to sync its internal model.
  //   Simply calling .val() changes the raw HTML value but Angular won't
  //   notice.  Triggering 'change' tells Angular "hey, this value changed",
  //   so the ng-model binding updates correctly.
  //
  // @param {string} selector - CSS selector for the target element
  // @param {*}      value    - the new value to set
  //
  // Examples:
  //   _setInputValue(_SEL.weightUnits, 'string:KG');       // switch to kilograms
  //   _setInputValue(_SEL.dimensionUnits, 'number:1');     // switch to centimetres
  function _setInputValue(selector, value) {
    $(selector).val(value).trigger('change');
  }

  // _getSelectedBatchId — returns the clean batch ID from the batch drop-down.
  //
  // AngularJS select elements prefix option values with their type, so a
  // string option looks like 'string:BATCH123'.  We strip that prefix so
  // callers receive the bare ID ('BATCH123').
  //
  // @returns {string} the selected batch ID with the 'string:' prefix removed
  //
  // Examples:
  //   var id = _getSelectedBatchId(); // returns e.g. 'MORNING-RUN' (not 'string:MORNING-RUN')
  //   if (_getSelectedBatchId() === '') { alert('Please select a batch first.'); }
  function _getSelectedBatchId() {
    return $(_SEL.batchSelect).val().replace('string:', '');
  }

  // _ensureCommodityArray — guarantees that a package's CommodityContents
  // array exists before anything tries to push items into it.
  //
  // This is a defensive-programming pattern: rather than crashing with
  // 'Cannot read property of undefined', we initialise the array if it
  // is missing and then return it for immediate use by the caller.
  //
  // @param {object} shipmentRequest - the full shipment request object
  // @param {number} packageIndex    - zero-based index of the package to check
  // @returns {Array}                - the (possibly newly created) CommodityContents array
  //
  // Examples:
  //   var arr = _ensureCommodityArray(vm.currentShipment, 0); arr.push(newItem);
  //   _ensureCommodityArray(req, 2).push({ ProductCode: 'SKU-99', Quantity: 3 });
  function _ensureCommodityArray(shipmentRequest, packageIndex) {
    if (!shipmentRequest.Packages[packageIndex].CommodityContents) {
      // Array didn't exist yet — create an empty one so callers can push into it
      shipmentRequest.Packages[packageIndex].CommodityContents = [];
    }
    return shipmentRequest.Packages[packageIndex].CommodityContents;
  }

  // _buildCommodityForExport — converts an internal commodity object into the
  // shape required by the ShipExec commercial-invoice / export API.
  //
  // International shipments require a commercial invoice.  The server expects
  // commodity line items in a specific format with export-specific fields
  // (DDTC measure, NAFTA dates, license fields, etc.) that don't exist on
  // the internal commodity object, so we map them here.
  //
  // 'PC' = 'Piece' — the default unit of measure for export quantity fields.
  //
  // @param {object} source - an internal commodity object from CommodityContents
  // @returns {object}      - a new commodity object shaped for the export API
  //
  // Examples:
  //   var exportItem = _buildCommodityForExport(pkg.CommodityContents[0]);
  //   consolidated.push(_buildCommodityForExport(commodity)); // inside consolidateInternationalCommodities
  function _buildCommodityForExport(source) {
    return {
      UnitValue:                  source.UnitValue,
      UnitWeight:                 source.UnitWeight,
      QuantityUnitMeasure:        'PC',               // pieces
      ExportQuantityUnitMeasure1: 'PC',
      ExportQuantityUnitMeasure2: 'PC',
      LicenseUnitValue:           { Currency: 'USD' }, // required field; currency defaults to USD
      DdtcUnitMeasure:            'PC',               // DDTC = Directorate of Defense Trade Controls
      OriginCountry:              source.OriginCountry,
      ProductCode:                source.ProductCode,
      HarmonizedCode:             source.HarmonizedCode, // HS code for customs classification
      Quantity:                   source.Quantity,
      Description:                source.Description,
      LicenseExpirationDate:      null,  // export license expiry — null means not applicable
      NaftaRvcAvgStartDate:       null,  // NAFTA regional value content start — null = N/A
      nAFTARVCAvgEndDate:         null   // NAFTA regional value content end   — null = N/A
    };
  }

  // _buildSelectForCountry — dynamically creates a <select> element populated
  // with country/state/province options.
  //
  // jQuery's $('<select />') creates a new element in memory (not yet in the DOM).
  // We add a blank first option so the user sees an empty default selection,
  // then loop through the provided key-value list to add real options.
  //
  // @param {object} attrs - HTML attributes to apply to the <select> (e.g. class, name)
  // @param {object} list  - key→value pairs where keys are option values and
  //                         values are the display text shown to the user
  // @returns {jQuery}     - the newly built <select> jQuery object (not yet in DOM)
  //
  // Examples:
  //   var $us = _buildSelectForCountry({ class: 'form-control', name: 'stateProvince' }, usStates);
  //   var $ca = _buildSelectForCountry({ name: 'stateProvince', required: true }, canadianProvinces);
  function _buildSelectForCountry(attrs, list) {
    var $select = $('<select />').attr(attrs);  // create <select> and apply attributes
    $select.append(new Option('', ''));          // blank first option (no default selection)
    $.each(list, function (val, text) {
      // new Option(text, value) creates an <option> element
      $select.append(new Option(text, val));
    });
    return $select;
  }

  // _closeManifestForShipper — performs a three-step end-of-day closeout for
  // a single shipper + carrier combination.
  //
  // Step 1: Verify the shipper has the target carrier configured.
  //         (There's no point closing a manifest for a carrier that isn't set up.)
  // Step 2: Fetch the open manifest items for this shipper/carrier.
  //         (Returns early with undefined if there's nothing to close.)
  // Step 3: Submit the CloseManifest request with the list of items.
  //
  // Note: all three calls use async: false (synchronous AJAX).
  // This is intentional here because the caller iterates many shippers
  // and needs each close to complete before moving to the next.
  // Generally, synchronous AJAX is discouraged in modern code because
  // it blocks the browser UI thread.
  //
  // @param {object} clientService   - thin-client service with config and auth token
  // @param {object} shipper         - shipper object with Id and Symbol properties
  // @param {string} carrierSymbol   - carrier code (e.g. 'UPS')
  // @param {string} companyId       - the company scoping these shipments
  // @returns {object|undefined}     - the CloseManifest API response, or undefined if
  //                                   there were no carriers or manifest items to close
  //
  // Examples:
  //   var result = _closeManifestForShipper(clientService, shippers[0], 'UPS', '42');
  //   var result = _closeManifestForShipper(myClient, { Id: 10, Symbol: 'ACME' }, 'FEDEX', companyId);
  function _closeManifestForShipper(clientService, shipper, carrierSymbol, companyId) {
    var authToken = clientService.authorizationToken();

    // ── Step 1: Check that this shipper has carriers configured ──────────────
    var carrierParams = { ShipperId: shipper.Id, CompanyId: companyId };
    var carrierResult = $.post({
      url: clientService.config.ApiUrl + '/api/ShippingService/GetShipperCarriers',
      data: carrierParams, async: false, headers: authToken
    }).responseJSON;

    if (!carrierResult.Carriers || carrierResult.Carriers.length === 0) return undefined;

    // ── Step 2: Get open manifest items for this shipper + carrier ───────────
    var manifestParams = {
      Carrier: carrierSymbol, Shipper: shipper.Symbol,
      // Sort newest first so the most recent shipments appear at the top
      SearchCriteria: { OrderByClauses: [{ FieldName: 'ShipDate', Direction: 'DESC' }] },
      IncludeImported: false, CompanyId: companyId
    };
    var manifestResult = $.post({
      url: clientService.config.ApiUrl + '/api/ShippingService/GetManifestItems',
      data: manifestParams, async: false, headers: authToken
    }).responseJSON;

    if (!manifestResult.ManifestItems || manifestResult.ManifestItems.length === 0) return undefined;

    // ── Step 3: Submit the close-manifest request ────────────────────────────
    // Only send the fields the API actually needs (avoid sending the full object)
    var items = manifestResult.ManifestItems.map(function (m) {
      return { Attributes: m.Attributes, ShipDate: m.ShipDate, Symbol: m.Symbol, Name: m.Name };
    });
    var closeParams = {
      Carrier: carrierSymbol, ManifestItems: items, Shipper: shipper.Symbol,
      Print: true,   // tell the server to generate the end-of-day report for printing
      UserParams: {}, CompanyId: companyId
    };
    return $.post({
      url: clientService.config.ApiUrl + '/api/ShippingService/CloseManifest',
      data: closeParams, async: false, headers: authToken
    }).responseJSON;
  }

  // _setDescription — sets the package description on both possible DOM elements.
  //
  // Depending on context or screen width, ShipExec renders the Description field
  // as either a <textarea> or a plain <input>.  Both share the same ng-model,
  // but they are different DOM elements, so we must update both to be safe.
  // Note: .val() on a textarea or input sets its displayed text.
  //
  // @param {string} value - the description text to display
  //
  // Examples:
  //   _setDescription('Returned Goods'); // standard label for return shipments
  //   _setDescription('');               // clear the description field
  function _setDescription(value) {
    $(_SEL.descriptionTextarea).val(value);
    $(_SEL.descriptionInput).val(value);
  }

  // _setReturnDeliveryCheckbox — selects or deselects the "Return Delivery" option
  // in the drop-down by targeting the option with value 'number:1'.
  //
  // In AngularJS select elements, numeric option values are prefixed 'number:'
  // (e.g. the value 1 appears as 'number:1' in the DOM).
  // .prop('selected', true/false) programmatically selects/deselects that option.
  //
  // @param {boolean} enabled - true to select (enable) Return Delivery,
  //                            false to deselect it
  //
  // Examples:
  //   _setReturnDeliveryCheckbox(true);  // turn on Return Delivery option
  //   _setReturnDeliveryCheckbox(false); // turn off Return Delivery option
  function _setReturnDeliveryCheckbox(enabled) {
    $('select option[value="number:1"]').prop('selected', enabled);
  }


  // ==========================================================================
  // PUBLIC — the returned object with all sub-objects
  // ==========================================================================

  return {

    // ========================================================================
    // Utilities — arrays, strings, comparators, dates
    //
    // General-purpose helper methods that don't depend on the DOM or the API.
    // They are stateless (no side effects) so they're easy to unit-test.
    // ========================================================================
    Utilities: {

      // getUniqueArrayFromString — splits a delimited string and returns a
      // de-duplicated array of non-empty, trimmed values.
      //
      // Example:
      //   getUniqueArrayFromString('A,B,A,C,', ',')  →  ['A', 'B', 'C']
      //
      // @param {string} inputString - the string to split
      // @param {string} delimiter   - separator character (default ',')
      // @returns {string[]}         - array of unique, non-blank values
      //
      // Examples:
      //   CBRHelper.Utilities.getUniqueArrayFromString('A,B,A,C,,D');    // → ['A','B','C','D']
      //   CBRHelper.Utilities.getUniqueArrayFromString('X|Y|X|Z', '|'); // → ['X','Y','Z']
      getUniqueArrayFromString: function (inputString, delimiter) {
        delimiter = delimiter || ',';
        // Split on the delimiter, then discard any blank/whitespace-only parts
        var parts = inputString.split(delimiter).filter(function (s) {
          return s.trim().length > 0;
        });
        // Keep only the first occurrence of each value (removes duplicates)
        return parts.filter(function (value, index, self) {
          return self.indexOf(value) === index;
        });
      },

      // getUniqueCSVStringFromArray — converts an array to a comma-separated
      // string, removing duplicates and blank entries.
      //
      // Example:
      //   getUniqueCSVStringFromArray(['A', 'B', 'A', ''])  →  'A,B,'
      //
      // Note: the trailing comma is intentional — callers strip it if needed.
      //
      // @param {string[]} inputArray - array of string values
      // @returns {string}            - unique values joined with commas
      //
      // Examples:
      //   CBRHelper.Utilities.getUniqueCSVStringFromArray(['ORD-1','ORD-2','ORD-1']); // → 'ORD-1,ORD-2,'
      //   CBRHelper.Utilities.getUniqueCSVStringFromArray(['123','456','','123']);     // → '123,456,'
      getUniqueCSVStringFromArray: function (inputArray) {
        // Remove duplicate values first
        var unique = inputArray.filter(function (value, index, self) {
          return self.indexOf(value) === index;
        });
        var result = '';
        unique.forEach(function (item) {
          if (item.trim().length > 0) {
            result += item.trim() + ',';
          }
        });
        return result;
      },

      // getUniqueCSVStringFromString — convenience method that combines
      // getUniqueArrayFromString and getUniqueCSVStringFromArray in one call.
      //
      // Example:
      //   getUniqueCSVStringFromString('A,B,A,,C')  →  'A,B,C,'
      //
      // @param {string} inputString - a comma-separated string (may have duplicates)
      // @returns {string}           - deduplicated, comma-separated string
      //
      // Examples:
      //   CBRHelper.Utilities.getUniqueCSVStringFromString('ORD-1,ORD-2,ORD-1');      // → 'ORD-1,ORD-2,'
      //   CBRHelper.Utilities.getUniqueCSVStringFromString($(_SEL.shipperRef).val() + ',' + newOrder);
      getUniqueCSVStringFromString: function (inputString) {
        var self = CBRHelper.Utilities;
        return self.getUniqueCSVStringFromArray(
          self.getUniqueArrayFromString(inputString)
        );
      },

      // compareByKey — returns a sort comparator function that sorts objects
      // by the value of a named property.
      //
      // This is a "function factory" — it returns another function.
      // Pass the result directly to Array.sort():
      //   myArray.sort(CBRHelper.Utilities.compareByKey('Name'))
      //
      // @param {string} key - the property name to sort by
      // @returns {Function} - a comparator (a, b) → -1 | 0 | 1
      //
      // Examples:
      //   vm.profile.Shippers.sort(CBRHelper.Utilities.compareByKey('Name'));      // sort shippers A→Z
      //   serviceList.sort(CBRHelper.Utilities.compareByKey('SortIndex'));          // sort by carrier rank
      compareByKey: function (key) {
        // The comparator follows the convention expected by Array.sort:
        //   return  1 if a should come AFTER  b
        //   return -1 if a should come BEFORE b
        //   return  0 if order doesn't matter
        return function (a, b) {
          if (a[key] > b[key]) return 1;
          if (a[key] < b[key]) return -1;
          return 0;
        };
      },

      // returnPropertyValue — returns the value if it is truthy, or an empty
      // string if it is null/undefined/0/false.
      //
      // Useful to avoid null-reference errors when reading optional fields,
      // especially when you need a safe string fallback.
      //
      // @param {*}      value - the value to check
      // @returns {*|string}   - the value itself, or '' if falsy
      //
      // Examples:
      //   CBRHelper.Utilities.returnPropertyValue(pkg.MiscReference1); // '' if null/undefined
      //   CBRHelper.Utilities.returnPropertyValue(pkg.Description);    // never returns undefined
      returnPropertyValue: function (value) {
        return value || '';
      },

      // getRandomAlphaNumericString — generates a short unique-enough string
      // suitable for use as a DOM element id or a temporary key.
      //
      // Math.random()         → a decimal like 0.83472...
      // .toString(36)         → converts to base-36 (digits + a-z), e.g. '0.rk2f9m'
      // .substring(2, 10)     → strips '0.' prefix and takes 8 chars
      // Prefix 'id-' ensures the result starts with a letter (valid HTML id).
      //
      // @returns {string} - a random 10-character string like 'id-rk2f9m4p'
      //
      // Examples:
      //   var id = CBRHelper.Utilities.getRandomAlphaNumericString(); // e.g. 'id-x7k9m2p4'
      //   $('<div />').attr('id', CBRHelper.Utilities.getRandomAlphaNumericString()).appendTo('body');
      getRandomAlphaNumericString: function () {
        return 'id-' + Math.random().toString(36).substring(2, 10);
      },

      // dateTimeStamp — returns the current local date/time as a human-readable
      // string including the timezone abbreviation (e.g. 'EST').
      //
      // 'en-US' locale ensures consistent formatting regardless of the browser
      // locale setting.  hour12: false gives 24-hour time (e.g. 14:30 not 2:30 PM).
      //
      // @returns {string} - formatted date/time, e.g. '1/15/2025, 09:30:00 AM EST'
      //
      // Examples:
      //   console.log('Shipment started at: ' + CBRHelper.Utilities.dateTimeStamp());
      //   entry.Timestamp = CBRHelper.Utilities.dateTimeStamp(); // stamp a log entry
      dateTimeStamp: function () {
        return new Date().toLocaleString('en-US', {
          timeZoneName: 'short',
          hour12: false
        });
      },

      // todayString — returns today's date as a short locale string
      // (e.g. '1/15/2025' in en-US).
      //
      // @returns {string} - today's date formatted for the user's locale
      //
      // Examples:
      //   console.log('Today is ' + CBRHelper.Utilities.todayString()); // e.g. '1/15/2025'
      //   vm.reportDate = CBRHelper.Utilities.todayString();
      todayString: function () {
        return new Date().toLocaleDateString();
      },

      // currentShipdate — returns today's date as an object with Year, Month,
      // and Day integer fields.
      //
      // ShipExec's API expects dates in this split-field format rather than
      // a JavaScript Date object or ISO string.
      //
      // Note: getMonth() is zero-based (January = 0), so we add 1.
      //
      // @returns {{ Year: number, Month: number, Day: number }}
      //
      // Examples:
      //   shipmentRequest.PackageDefaults.Shipdate = CBRHelper.Utilities.currentShipdate(); // { Year:2025, Month:1, Day:15 }
      //   profile.PackageDefaults.Shipdate = CBRHelper.Utilities.currentShipdate();          // reset date after applying profile
      currentShipdate: function () {
        var today = new Date();
        return {
          Year:  today.getFullYear(),
          Month: today.getMonth() + 1,  // +1 because JS months are 0-indexed
          Day:   today.getDate()
        };
      },

      // translateUnitOfMeasurement — maps verbose unit strings from an external
      // order system to the shorter codes that ShipExec's API expects.
      //
      // Example: 'YARDS' (from the order feed) → 'YD' (ShipExec code)
      //
      // If the value isn't in the map, the original value is returned unchanged,
      // so this function is safe to call on any string.
      //
      // @param {string} value - unit of measure string to translate
      // @returns {string}     - ShipExec unit code, or the original value if not mapped
      //
      // Examples:
      //   CBRHelper.Utilities.translateUnitOfMeasurement('YARDS'); // → 'YD'
      //   CBRHelper.Utilities.translateUnitOfMeasurement('EACH');  // → 'EA'
      translateUnitOfMeasurement: function (value) {
        var map = {
          'EACH': 'EA', 'YARDS': 'YD', 'YARD': 'YD',
          'METER': 'M', 'SF': 'SFT', 'PAIR': 'PR', 'SR': 'ROL'
        };
        // toUpperCase() makes the lookup case-insensitive
        // The || operator returns the original value if the key isn't found
        return map[value.toUpperCase()] || value;
      }
    },


    // ========================================================================
    // Logger — LogLevel enum, console + server logging
    //
    // Centralises all diagnostic output so you can switch between console-only
    // and console+server logging by toggling a single flag.
    // ========================================================================
    Logger: {

      // LogLevel — an enum-like object defining the recognised severity levels.
      // Using named constants (instead of raw strings like 'error') means:
      //   • Typos in log calls are easier to catch.
      //   • You can search the codebase for 'LogLevel.Error' reliably.
      LogLevel: {
        Error: 'Error',  // something broke and requires attention
        Info:  'Info',   // normal informational message
        Debug: 'Debug',  // extra detail useful during development
        Trace: 'Trace',  // very verbose, step-by-step execution tracing
        Fatal: 'Fatal'   // unrecoverable failure
      },

      // _serverLogging is a private-ish flag (prefixed _ by convention) that
      // controls whether log entries are also sent to the server.
      // Default is false — only log to the browser console.
      _serverLogging: false,

      // setServerDebugMode — enables or disables server-side logging at runtime.
      //
      // When enabled, every call to Log() will POST the entry to the
      // ShipExec UserMethod API so it can be stored server-side.
      // Useful for diagnosing issues in production without attaching a debugger.
      //
      // @param {boolean} enabled - true to turn server logging on, false to turn it off
      //
      // Examples:
      //   CBRHelper.Logger.setServerDebugMode(true);  // enable — every Log() call POSTs to the server
      //   CBRHelper.Logger.setServerDebugMode(false); // disable — console only
      setServerDebugMode: function (enabled) {
        console.log('Logging to server:', enabled);
        this._serverLogging = enabled;
      },

      // Log — the main logging function.  Accepts a log-entry object and:
      //   1. Normalises the entry (sets LogLevel, extracts Error details).
      //   2. Outputs to the browser console (console.error or console.log).
      //   3. Optionally POSTs the entry to the server if _serverLogging is on.
      //   4. Hides the loading spinner (best-effort cleanup on any log call).
      //
      // Entry object shape:
      //   { Source: string,       ← which function produced this entry
      //     Message?: string,     ← human-readable description
      //     Data?: any,           ← extra context (object, array, etc.)
      //     Error?: Error,        ← a caught JS Error object
      //     LogLevel?: string }   ← one of Logger.LogLevel (defaults to Info)
      //
      // @param {object} entry - the log entry (see shape above)
      //
      // Examples:
      //   CBRHelper.Logger.Log({ Source: 'initShipping()', Message: 'Page loaded', Data: vm.profile });
      //   CBRHelper.Logger.Log({ Source: 'processOrders()', Error: err }); // err is a caught JS Error
      Log: function (entry) {
        try {
          if (entry.Error) {
            // If an Error object was passed, force the level to Error and
            // extract only name + message (Error objects don't serialise well)
            entry.LogLevel = this.LogLevel.Error;
            entry.Error = { name: entry.Error.name, message: entry.Error.message };
          } else if (!entry.LogLevel) {
            // No level specified — default to Info
            entry.LogLevel = this.LogLevel.Info;
          }

          if (entry.LogLevel === this.LogLevel.Error) {
            // Errors get console.error (shows in red in browser DevTools)
            console.error('Exception in', entry.Source);
            console.error(entry.Error.name);
            console.error(entry.Error.message);
            if (entry.Data) console.log(entry.Data);  // print extra context if provided
          } else {
            console.log('Output from', entry.Source);
            console.log(entry.Message);
            if (entry.Data) console.log(entry.Data);
          }

          if (this._serverLogging) {
            // Build the API payload and POST it asynchronously so it doesn't
            // block the UI thread
            var ajaxData = _buildUserMethodPayload(
              { ServerMethod: 'AddClientEntry', MessageObject: entry },
              CBRHelper.Context.getCurrentUserContext()
            );
            $.ajax({
              url:         'api/ShippingService/UserMethod',
              method:      'POST',
              contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
              dataType:    'json',
              data:        ajaxData,
              async:       true  // fire-and-forget; don't block the page
            }).fail(function (jqXHR, textStatus) {
              console.log('Unable to log message to server.', jqXHR, textStatus);
            });
          }

          // Always hide the loader on any log call — even on error paths the
          // spinner should not be left spinning indefinitely
          CBRHelper.UI.hideLoader();
        } catch (error) {
          // Last-resort catch: if logging itself throws, at least print to console
          console.log(error.message);
        }
      },

      // logStartMethod — prints a standardised 'STARTING ...' banner to the
      // console when entering a method.  Optionally shows who called it.
      //
      // Useful for tracing execution flow without a debugger.
      //
      // @param {string} method        - the method that is starting
      // @param {string} [calleeMethod] - the method that invoked it (optional)
      //
      // Examples:
      //   CBRHelper.Logger.logStartMethod('initShipping()', 'onPageLoaded()'); // logs '...STARTING initShipping() called by onPageLoaded()'
      //   CBRHelper.Logger.logStartMethod('validateClientMatter()');            // logs '...STARTING validateClientMatter()'
      logStartMethod: function (method, calleeMethod) {
        if (calleeMethod) {
          console.log('...STARTING ' + method + ' called by ' + calleeMethod);
        } else {
          console.log('...STARTING ' + method);
        }
      },

      // logMethodInfo — prints an indented informational message to the console.
      // The leading spaces visually group this message under the method banner
      // printed by logStartMethod, making the log easier to read.
      //
      // @param {string} message - the message to print
      //
      // Examples:
      //   CBRHelper.Logger.logMethodInfo('Order count: ' + orders.length);
      //   CBRHelper.Logger.logMethodInfo('Service selected: ' + service.Symbol);
      logMethodInfo: function (message) {
        console.log('      ' + message);
      },

      // decodeReturnString — decodes an HTML-entity-encoded JSON string.
      //
      // Some older ShipExec endpoints return JSON that has been HTML-entity
      // encoded (e.g. '&quot;' instead of '"').  Creating a temporary <div>,
      // setting its .html() to the encoded string, and reading back .text()
      // causes the browser to unescape the entities for us — a common trick.
      //
      // @param {string} data   - an HTML-entity-encoded JSON string
      // @returns {object|void} - the parsed JS object, or undefined on error
      //
      // Examples:
      //   var result  = CBRHelper.Logger.decodeReturnString(response.Data);
      //   var profile = CBRHelper.Logger.decodeReturnString(json.Data); console.log(profile.Name);
      decodeReturnString: function (data) {
        try {
          return JSON.parse($('<div />').html(data).text());
        } catch (error) {
          console.log('DecodeReturnString Error: ' + error.name + ' ' + error.message);
        }
      }
    },


    // ========================================================================
    // UI — loader, overlay, alerts, focus
    //
    // Provides helpers for showing/hiding the loading spinner and translucent
    // overlay, displaying error alerts, and setting keyboard focus.
    // All DOM operations use jQuery and target CSS classes that already exist
    // in the ShipExec Angular template.
    // ========================================================================
    UI: {

      // showLoader — makes the page's loading spinner visible.
      //
      // The spinner is an AngularJS-managed element controlled by the CSS
      // classes 'ng-hide' and 'ng-show'.  We swap them to reveal the element.
      // An optional callback can be executed immediately after the spinner
      // appears (e.g. to kick off an async operation).
      //
      // @param {Function} [callback] - optional function to run after showing
      //
      // Examples:
      //   CBRHelper.UI.showLoader();
      //   CBRHelper.UI.showLoader(function() { CBRHelper.API.makeUserMethodRequest('GetOrders', {}, true, cb); });
      showLoader: function (callback) {
        $('div.loading').removeClass('ng-hide').addClass('ng-show');
        // 'typeof' check ensures we only call it when a function was provided
        if (typeof callback === 'function') callback();
      },

      // hideLoader — hides the loading spinner and also removes the overlay.
      //
      // Always call this when an async operation completes, even on error paths,
      // so the user isn't stuck looking at a spinning indicator.
      //
      // Examples:
      //   CBRHelper.UI.hideLoader();
      //   request.always(function() { CBRHelper.UI.hideLoader(); });
      hideLoader: function () {
        $('div.loading').removeClass('ng-show').addClass('ng-hide');
        CBRHelper.UI.hideOverlay();  // also clear the dark overlay behind the spinner
      },

      // showOverlay — adds a semi-transparent dark overlay over the entire page.
      //
      // The overlay visually blocks interaction while a modal or loader is active.
      // It is created dynamically the first time it's needed and then simply
      // shown/hidden on subsequent calls.
      //
      // CSS breakdown:
      //   position: fixed   → stays in place when the user scrolls
      //   100vw / 100vh     → covers the full viewport width and height
      //   zIndex: 1049      → sits above normal content but below Bootstrap modals (z 1050+)
      //   rgba(0,0,0,.25)   → 25% opaque black = subtle darkening effect
      //
      // Examples:
      //   CBRHelper.UI.showOverlay();
      //   CBRHelper.UI.showOverlay(); $('#confirmModal').modal('show');
      showOverlay: function () {
        if ($('div.overlay').length === 0) {
          // First call: create the overlay element and inject it into the page
          $('div.body-content').first().append(
            $('<div />')
              .addClass('overlay')
              .css({
                content: '', position: 'fixed', top: 0, left: 0,
                width: '100vw', height: '100vh',
                backgroundColor: 'rgba(0, 0, 0, .25)', zIndex: 1049
              })
          ).show();
        } else {
          // Overlay element already exists — just make it visible again
          $('div.overlay').show();
        }
      },

      // hideOverlay — hides the overlay without removing it from the DOM,
      // so the next showOverlay() call can reuse it without recreating it.
      //
      // Examples:
      //   CBRHelper.UI.hideOverlay();
      //   $('#myModal').on('hidden.bs.modal', function() { CBRHelper.UI.hideOverlay(); });
      hideOverlay: function () {
        $('div.overlay').hide();
      },

      // showErrorAlert — injects a dismissible Bootstrap error banner into the
      // page body.  Each call creates its own independent alert (they stack).
      //
      // Why build the DOM in JS rather than using a template?
      //   Some error conditions occur outside Angular's digest cycle, so we
      //   can't rely on Angular to render a template.  jQuery gives us full
      //   control.
      //
      // The random id lets us find and remove individual alerts later if needed.
      //
      // @param {string} errorMessage - the text to display in the alert banner
      //
      // Examples:
      //   CBRHelper.UI.showErrorAlert('Invalid tracking number. Please try again.');
      //   CBRHelper.UI.showErrorAlert('ERROR 500: Address mismatch for order ' + order.orderNumber);
      showErrorAlert: function (errorMessage) {
        var id = CBRHelper.Utilities.getRandomAlphaNumericString();  // unique id for this alert
        var $alert = $('<div />')
          .attr('id', id)
          .addClass('alert alert-dismissible alert-bottom alert-danger ng-scope')
          .css({ zIndex: 1550, cursor: 'pointer' })  // above the overlay and modals
          .attr('role', 'alert')
          .append(
            // Dismiss button (the 'x' in the top-right corner)
            $('<button />').attr('type', 'button').addClass('close')
              .append($('<span />').attr('aria-hidden', 'false').text('x'))
              .append($('<span />').addClass('sr-only').text('Close'))  // screen-reader text
          )
          .append(
            $('<div />').append(
              $('<div />').addClass('ng-scope alert-message')
                .text(errorMessage)
                .prepend(
                  // Warning icon to the left of the message
                  $('<span />').css('padding-right', '10px')
                    .addClass('glyphicon glyphicon-alert custom-error-icon')
                )
            )
          );
        $('body').append($alert.show());  // add to the page and make visible immediately
      },

      // showModalAlert — populates an existing Bootstrap modal with a title and
      // message and then opens it.
      //
      // This uses AngularJS's 'vm' (view-model) to push data into the template;
      // the modal HTML uses {{vm.currentShipment.AlertTitle}} etc. to display it.
      //
      // @param {string} title       - text to show in the modal header
      // @param {string} message     - text to show in the modal body
      // @param {jQuery} $alertModal - jQuery-wrapped Bootstrap modal element
      //
      // Examples:
      //   CBRHelper.UI.showModalAlert('Warning', 'Shipment weight exceeds carrier limit.', $alertModal);
      //   CBRHelper.UI.showModalAlert('ShipExec Alert', userData.substring(1), $newShipModal);
      showModalAlert: function (title, message, $alertModal) {
        vm.currentShipment.AlertTitle = title;
        vm.currentShipment.AlertMessage = message;
        $alertModal.modal('show');  // Bootstrap's modal plugin opens the dialog
      },

      // setFocus — moves keyboard focus to the element matching the selector.
      //
      // Handy after showing a dialog or completing a validation error:
      // putting focus on the relevant field saves the user a click.
      //
      // @param {string} selector - CSS selector for the element to focus
      //
      // Examples:
      //   CBRHelper.UI.setFocus('input[name="ShipperReference_txt"]');
      //   CBRHelper.UI.setFocus(_SEL.loadValue);
      setFocus: function (selector) {
        $(selector).focus();
      }
    },


    // ========================================================================
    // DOM — poll / wait for elements, event helpers
    //
    // Higher-level wrappers around _pollForElement that handle common patterns:
    // waiting for an element, waiting for a <select> to be populated, and
    // checking whether an event handler is already bound.
    // ========================================================================
    DOM: {

      // waitForElement — waits for a DOM element to appear, then optionally
      // sets a default value, runs a callback, and/or gives the element focus.
      //
      // Declared 'async' so callers can 'await' it, but it also works without await.
      // Errors are caught and routed through the logger instead of crashing.
      //
      // @param {string}   selector        - CSS selector for the element to wait for
      // @param {boolean}  focusAfter      - if true, focus the element after it appears
      // @param {*}        defaultValue    - if non-null, set the element's value to this
      // @param {number}   timeoutSeconds  - max wait time (passed to _pollForElement)
      // @param {Function} [callback]      - optional function to run once element exists
      //
      // Examples:
      //   CBRHelper.DOM.waitForElement(_SEL.shipperRef, true, 'ORD-12345', 10, null);
      //   CBRHelper.DOM.waitForElement('#divResults', false, null, 15, function() { bindEventHandlers(); });
      waitForElement: async function (selector, focusAfter, defaultValue, timeoutSeconds, callback) {
        try {
          await _pollForElement(selector, timeoutSeconds);  // wait until element exists
          if (defaultValue) $(selector).val(defaultValue);  // pre-fill value if provided
          if ($.isFunction(callback)) callback();           // run caller's setup code
          if (focusAfter) $(selector).focus();              // move keyboard focus
        } catch (error) {
          CBRHelper.Logger.Log({ Source: 'waitForElement()', Error: error });
        }
      },

      // waitForSelectOptions — waits until a <select> element has been populated
      // with at least minOptions items, then optionally pre-selects one.
      //
      // AngularJS often populates <select> options asynchronously (via $http or
      // ng-options).  This function polls until enough options exist before
      // trying to set a selection, preventing 'selected value not found' bugs.
      //
      // @param {string}   selectSelector - CSS selector for the <select>
      // @param {number}   minOptions     - minimum option count to wait for (default 1)
      // @param {number}   indexToSelect  - zero-based index to select (or null to skip)
      // @param {*}        valueToSelect  - value to select (or null to skip)
      // @param {boolean}  clearFirst     - if true, clears the current selection first
      // @param {number}   timeoutSeconds - max wait time
      // @param {Function} [callback]     - optional function to run once ready
      //
      // Examples:
      //   CBRHelper.DOM.waitForSelectOptions(_SEL.weightUnits, 1, null, 'string:LB', false, 10, null);
      //   CBRHelper.DOM.waitForSelectOptions('[ng-model="vm.service"]', 2, 0, null, true, 20, function() { console.log('service list ready'); });
      waitForSelectOptions: async function (selectSelector, minOptions, indexToSelect, valueToSelect, clearFirst, timeoutSeconds, callback) {
        if (clearFirst) $(selectSelector).val([]);  // deselect everything first if requested
        // ':gt(n)' selector matches options beyond index n; we wait for those to exist
        var optionSelector = selectSelector + ' option:gt(' + (minOptions || 1) + ')';
        try {
          await _pollForElement(optionSelector, timeoutSeconds);
          if ($.isFunction(callback)) callback();
          // Select by index using .eq(n).prop('selected', 'selected')
          if (indexToSelect != null) $(selectSelector + ' option').eq(indexToSelect).prop('selected', 'selected');
          // Select by value using jQuery .val()
          if (valueToSelect) $(selectSelector).val(valueToSelect);
        } catch (error) {
          CBRHelper.Logger.Log({ Source: 'waitForSelectOptions()', Error: error });
        }
      },

      // isEventAttached — checks whether a specific event handler function is
      // already bound to an element.  Prevents binding the same handler twice.
      //
      // jQuery stores event metadata in an internal data cache accessible via
      // $._data(element, 'events').  We look up the handlers for the given event
      // name and compare their toString() representations.
      //
      // The optional chaining operator (?.) returns undefined (not an error) if
      // the event name has no registered handlers.
      //
      // @param {jQuery}   $element  - the jQuery-wrapped element to inspect
      // @param {string}   eventName - the event name (e.g. 'click', 'change')
      // @param {Function} handler   - the function to search for
      // @returns {boolean}          - true if the handler is already attached
      //
      // Examples:
      //   if (!CBRHelper.DOM.isEventAttached($('#btnShip'), 'click', myHandler)) { $('#btnShip').on('click', myHandler); }
      //   var alreadyBound = CBRHelper.DOM.isEventAttached($('#shipForm'), 'submit', validateForm);
      isEventAttached: function ($element, eventName, handler) {
        var events = $._data($element.get(0), 'events');  // jQuery internal event store
        if (!events) return false;  // no events at all on this element
        var handlerStr = handler.toString();
        return events[eventName]?.some(function (ev) {
          return ev.handler.toString() === handlerStr;
        }) || false;
      }
    },


    // ========================================================================
    // API — thin-client requests, UserMethod, ajaxGet
    //
    // Wrappers around jQuery AJAX that handle the two ways ShipExec can be
    // called: directly from a browser (thin-client) and from within an
    // embedded ShipExec page (where auth is implicit).
    // ========================================================================
    API: {

      // thinClientApiRequestFromConfig — reads the API URL and auth token from
      // a local config.json file, then makes a POST request.
      //
      // Used in stand-alone thin-client pages that don't have a server-side
      // session.  The token is read from localStorage (set at login time).
      //
      // Note: the result of $.post() is returned from inside a $.getJSON
      // callback, so the outer function actually returns undefined.  If you
      // need the response, use thinClientApiRequest() instead.
      //
      // @param {string}  method  - API method name (appended to the base URL)
      // @param {object}  data    - the request data
      // @param {boolean} isAsync - whether to make the request asynchronously
      //
      // Examples:
      //   CBRHelper.API.thinClientApiRequestFromConfig('GetBatches', { CompanyId: 1 }, true);
      //   CBRHelper.API.thinClientApiRequestFromConfig('GetShippers', {}, false);
      thinClientApiRequestFromConfig: function (method, data, isAsync) {
        $.getJSON('config.json', function (config) {
          var url   = config.ShipExecServiceUrl;
          // If the URL is an absolute HTTP address, we need a Bearer token;
          // for relative URLs (same-origin) the session cookie is enough
          var token = url.startsWith('http')
            ? { Authorization: 'Bearer ' + JSON.parse(window.localStorage.getItem('TCToken')).access_token }
            : '';
          return $.post({ url: url + '/' + method, data: data, async: isAsync, headers: token });
        });
      },

      // thinClientApiRequest — makes a POST to a ShipExec thin-client API
      // endpoint using the auth token from the provided client object.
      //
      // Unlike the FromConfig variant, this doesn't read config.json and
      // returns the jQuery promise so callers can chain .done()/.fail().
      //
      // @param {string}  method   - API method name
      // @param {object}  data     - the request data
      // @param {boolean} isAsync  - true = non-blocking, false = synchronous
      // @param {object}  client   - thin-client object with config and getAuthorizationToken()
      // @returns {jqXHR}          - jQuery AJAX promise
      //
      // Examples:
      //   CBRHelper.API.thinClientApiRequest('GetProfile', { UserId: 5 }, true, client).done(function(r) { vm.profile = r; });
      //   var result = CBRHelper.API.thinClientApiRequest('RateShipment', shipData, false, client).responseJSON;
      thinClientApiRequest: function (method, data, isAsync, client) {
        return $.post({
          url:     client.config.ShipExecServiceUrl + '/' + method,
          data:    data,
          async:   isAsync,
          headers: client.getAuthorizationToken()
        });
      },

      // makeUserMethodRequest — the primary way to call ShipExec's UserMethod
      // endpoint from within a ShipExec page.
      //
      // UserMethod is a generic server-side extension point: you pass a
      // ServerMethod name and any data, and the server routes it to the
      // correct C# method.  This wrapper:
      //   1. Shows the loading spinner.
      //   2. Wraps the payload in the expected envelope.
      //   3. POSTs to UserMethod.
      //   4. Parses the response on success.
      //   5. Logs errors on failure.
      //   6. Hides the spinner and calls the optional callback when done.
      //
      // @param {string}   requestMethod - the server-side method name to call
      // @param {object}   requestData   - data to pass to the server method
      // @param {boolean}  isAsync       - true (default) for async, false for sync
      // @param {Function} [callback]    - called with the parsed response when complete
      // @returns {object|undefined}     - the parsed response (only useful when isAsync=false)
      //
      // Examples:
      //   CBRHelper.API.makeUserMethodRequest('GetOrderInformation', 'ORD-001,ORD-002', true, function(data) { processOrders(data); });
      //   CBRHelper.API.makeUserMethodRequest('ValidateAddress', addressData, false, null);
      makeUserMethodRequest: function (requestMethod, requestData, isAsync, callback) {
        CBRHelper.UI.showLoader();
        var retJson;  // will hold the parsed response data
        var dataObject = { ServerMethod: requestMethod, MethodData: requestData };
        var ajaxObject = _buildUserMethodPayload(dataObject, CBRHelper.Context.getCurrentUserContext());

        var request = $.ajax({
          url:         'api/ShippingService/UserMethod',
          method:      'POST',
          contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
          dataType:    'json',
          async:       isAsync !== false,  // default to async unless explicitly set to false
          data:        ajaxObject
        });

        // .done() fires when the server returns a 2xx response
        request.done(function (json) {
          retJson = parseReturnData(json.Data);
        });
        // .fail() fires on HTTP errors (4xx, 5xx) or network failures
        request.fail(function (jqXHR) {
          console.error('MakeUserMethodRequest failed:', jqXHR.responseText);
        });
        // .always() fires regardless of success or failure — good for cleanup
        request.always(function () {
          CBRHelper.UI.hideLoader();
          if (typeof callback === 'function') callback(retJson);
        });
        return retJson;  // only useful in synchronous mode; undefined in async mode
      },

      // ajaxGet — a minimal synchronous GET request that returns the response
      // body directly.
      //
      // Use for simple data fetches where you need the result immediately
      // and the blocked UI thread is acceptable (e.g. page initialisation).
      //
      // @param {string} url    - the URL to GET
      // @returns {*}           - the raw response (parsed by jQuery based on Content-Type)
      //
      // Examples:
      //   var config = CBRHelper.API.ajaxGet('config.json');
      //   var carriers = CBRHelper.API.ajaxGet('api/ShippingService/GetCarriers');
      ajaxGet: function (url) {
        var result;
        $.ajax({
          type: 'GET', url: url, async: false,
          success: function (response) { result = response; }
        });
        return result;
      }
    },


    // ========================================================================
    // Context — user session, mailroom/traveler, custom data, view model
    //
    // Manages the current user's identity (CompanyId, UserId) and provides
    // helpers for reading custom data fields attached to addresses.
    // The 'current user context' is stored in the private _currentUserContext
    // variable and read back by the API helpers to authorise requests.
    // ========================================================================
    Context: {

      // setCurrentUserContext — populates the private _currentUserContext store
      // from the Angular view-model.
      //
      // There are three code paths:
      //   1. viewModel has UserInformation directly → use it.
      //   2. We don't have a UserId yet AND includeUser is true → fetch from the
      //      server's usercontext endpoint.
      //   3. We have a UserId but no CompanyId → read CompanyId from the profile.
      //
      // @param {object}  viewModel   - the Angular vm (view-model) object
      // @param {boolean} includeUser - whether to fetch the UserId from the server
      //                                if it isn't already known
      //
      // Examples:
      //   CBRHelper.Context.setCurrentUserContext(vm, false); // use what's already on the vm
      //   CBRHelper.Context.setCurrentUserContext(vm, true);  // also fetch UserId from server if missing
      setCurrentUserContext: function (viewModel, includeUser) {
        var ctx = {};
        try {
          if (viewModel.UserInformation) {
            // Path 1: UserInformation is directly on the view-model (most common)
            ctx.CompanyId = viewModel.UserInformation.CompanyId;
            ctx.UserId    = viewModel.UserInformation.UserId;
            _currentUserContext = ctx;
            return;
          }
          if (!_currentUserContext.UserId && includeUser) {
            // Path 2: No UserId in cache yet — fetch from the server synchronously
            $.ajax({ url: 'api/usercontext/GET', method: 'GET', async: false })
              .done(function (data) { ctx = data; })
              .fail(function (jqXHR) { console.error('SetCurrentUserContext failed:', jqXHR.responseText); });
          } else if (!_currentUserContext.CompanyId) {
            // Path 3: Have a UserId but the CompanyId is missing — get it from the profile
            ctx.CompanyId = viewModel.profile.CompanyId;
            ctx.UserId    = _currentUserContext.UserId;
          }
        } catch (error) {
          console.error('SetCurrentUserContext error:', error.message);
        }
        _currentUserContext = ctx;
      },

      // getCurrentUserContext — returns the cached user context object.
      //
      // Called by API helpers (_buildUserMethodPayload etc.) when they need to
      // attach user identity to an outgoing request.
      //
      // @returns {{ CompanyId: string, UserId: string }}
      //
      // Examples:
      //   var ctx = CBRHelper.Context.getCurrentUserContext(); // { CompanyId: '42', UserId: '5' }
      //   var payload = _buildUserMethodPayload(data, CBRHelper.Context.getCurrentUserContext());
      getCurrentUserContext: function () {
        return _currentUserContext;
      },

      // getUserContext — reads user context directly from a view-model instance.
      //
      // Different Angular controllers store the user context under different
      // property names.  This helper tries both variants.
      //
      // @param {object} vmInstance - the Angular controller's vm object
      // @returns {object}          - the user context (or undefined if neither exists)
      //
      // Examples:
      //   var ctx = CBRHelper.Context.getUserContext(vm);
      //   var companyId = CBRHelper.Context.getUserContext(vm).CompanyId;
      getUserContext: function (vmInstance) {
        return vmInstance.userContext || vmInstance.UserInformation;
      },

      // fetchUserContext — retrieves user context from the server for a
      // thin-client page and stores it directly on the client object.
      //
      // Used when there is no Angular view-model to read from (e.g. standalone
      // pages that communicate with ShipExec via the thin-client API).
      //
      // @param {object} client - thin-client object with config and auth helpers
      //
      // Examples:
      //   CBRHelper.Context.fetchUserContext(thinClient); // populates thinClient.userContext
      //   CBRHelper.Context.fetchUserContext(client); var userId = client.userContext.UserId;
      fetchUserContext: function (client) {
        // Reuse the ShipExecServiceUrl but swap the service segment to usercontext/GET
        var url   = client.config.ShipExecServiceUrl.replace('ShippingService', 'usercontext/GET');
        var token = client.getAuthorizationToken();
        client.userContext = $.get({ url: url, headers: token, async: false }).responseJSON;
      },

      // setMailroomOrTraveler — returns an object describing whether the active
      // profile is a Mailroom or Traveler profile.
      //
      // These two profile types have different UI rules (e.g. Traveler profiles
      // hide load-radio buttons).  Centralising the logic here keeps it out of
      // individual page controllers.
      //
      // @param {string} profileName - the profile's display name
      // @returns {{ IsMailroom: boolean, IsTraveler: boolean, LoadRadioButtonsIsHidden: boolean }}
      //
      // Examples:
      //   CBRHelper.Context.setMailroomOrTraveler('Mailroom Profile'); // → { IsMailroom: true,  IsTraveler: false, … }
      //   CBRHelper.Context.setMailroomOrTraveler('Traveler Profile'); // → { IsMailroom: false, IsTraveler: true,  … }
      setMailroomOrTraveler: function (profileName) {
        return {
          IsMailroom:               profileName !== 'Traveler Profile',
          IsTraveler:               profileName === 'Traveler Profile',
          LoadRadioButtonsIsHidden: true  // always hidden on first load
        };
      },

      // getCustomDataValue — finds the value for a named key in a CustomData
      // array (the ShipExec format for extension fields on addresses).
      //
      // CustomData is an array of { Key: string, Value: string } pairs.
      // The comparison is case-insensitive so callers don't have to match
      // exact capitalisation.
      //
      // @param {string}   key        - the custom field name to look for
      // @param {object[]} customData - array of Key/Value pairs (may be null)
      // @returns {string}            - the field's value, or '' if not found
      //
      // Examples:
      //   CBRHelper.Context.getCustomDataValue('Department', address.CustomData); // e.g. 'Finance'
      //   CBRHelper.Context.getCustomDataValue('CostCenter', pkg.CustomData);     // e.g. '4400'
      getCustomDataValue: function (key, customData) {
        if (!customData) return '';  // guard against null/undefined customData
        for (var i = 0; i < customData.length; i++) {
          if (customData[i].Key.toLowerCase() === key.toLowerCase()) {
            return customData[i].Value;
          }
        }
        return '';  // key not found
      },

      // getCustomField — reads a custom field from either the origin address
      // ('User') or the consignee address ('To') of the current shipment.
      //
      // ShipExec stores per-address extension data in CustomData arrays.
      // This method selects the right array based on the source parameter
      // and delegates the lookup to getCustomDataValue.
      //
      // @param {string} fieldName       - the custom field name
      // @param {string} source          - 'User' (origin) or 'To' (consignee)
      // @param {object} shipmentRequest - the shipment request object
      // @returns {string}               - the field value, or ''
      //
      // Examples:
      //   CBRHelper.Context.getCustomField('Department', 'User', vm.currentShipment); // from origin address
      //   CBRHelper.Context.getCustomField('PONumber',   'To',   shipmentRequest);    // from consignee address
      getCustomField: function (fieldName, source, shipmentRequest) {
        var customData = null;
        // Optional chaining (?.) returns undefined without throwing if a
        // nested property doesn't exist
        if (source === 'User') customData = shipmentRequest?.PackageDefaults?.OriginAddress?.CustomData;
        if (source === 'To')   customData = shipmentRequest?.PackageDefaults?.Consignee?.CustomData;
        return CBRHelper.Context.getCustomDataValue(fieldName, customData);
      },

      // setCurrentViewModel — stores the Angular vm on window._currentViewModel
      // so that non-Angular code can access it, and optionally updates the
      // user context.
      //
      // window._currentViewModel is the conventional way in this codebase to
      // pass the Angular view-model to console-based debugging scripts.
      //
      // @param {object}   viewModel  - the Angular vm to store
      // @param {boolean}  setUserCtx - if true, also call setCurrentUserContext
      // @param {boolean}  includeUser- passed through to setCurrentUserContext
      // @param {Function} [callback] - optional function to call when done
      //
      // Examples:
      //   CBRHelper.Context.setCurrentViewModel(vm, true, true, function() { initShippingPage(); });
      //   CBRHelper.Context.setCurrentViewModel(vm, false, false, null); // just store the vm reference
      setCurrentViewModel: function (viewModel, setUserCtx, includeUser, callback) {
        if (setUserCtx) {
          CBRHelper.Context.setCurrentUserContext(viewModel, includeUser);
        }
        window._currentViewModel = viewModel;  // make vm accessible globally for debugging
        if (typeof callback === 'function') callback();
      }
    },


    // ========================================================================
    // Address — consignee search triggers, address matching, copy
    //
    // Helpers for working with the ShipExec consignee (recipient) address
    // component: wiring up keyboard shortcuts, copying the origin address,
    // and detecting when an incoming order address differs from what's on screen.
    // ========================================================================
    Address: {

      // bindConsigneeTabSearch — triggers an address-book search when the user
      // presses Tab inside the consignee 'code' input.
      //
      // Default Tab behaviour moves focus to the next field.  e.preventDefault()
      // stops that, and we instead programmatically click the Search button so
      // the user can quickly look up an address without reaching for the mouse.
      //
      // $(document).ready() ensures the DOM is fully loaded before we try to
      // attach the event listener.
      //
      // Examples:
      //   CBRHelper.Address.bindConsigneeTabSearch(); // call once during page init
      //   // After calling, pressing Tab in the consignee code box auto-clicks Search
      bindConsigneeTabSearch: function () {
        $(document).ready(function () {
          // Delegate the keydown event to the consignee section's code input
          $('[id="Consignee Name Address"]').on('keydown', 'input[name="code"]', function (e) {
            var keyCode = e.keyCode || e.which;  // e.which is older; keyCode is modern
            if (keyCode === 9) {  // 9 = Tab key
              e.preventDefault();  // prevent default Tab navigation
              $('[caption="Consignee Name Address"]')
                .find('button[ng-click="search(nameaddress)"]').click();
            }
          });
        });
      },

      // bindCodeInputLookup — triggers an address-book lookup when the user
      // presses Enter or leaves (focusout) the code input field.
      //
      // .delegate() is the older jQuery API for delegated event binding; it
      // attaches to 'body' but only fires for elements matching the selector.
      // This lets it work even if the code input is added to the DOM later.
      //
      // @param {string} codeInputSelector - CSS selector for the lookup code input
      //
      // Examples:
      //   CBRHelper.Address.bindCodeInputLookup('input[name="consigneeCode"]');
      //   CBRHelper.Address.bindCodeInputLookup('#txtConsigneeId'); // fires search on Enter or focusout
      bindCodeInputLookup: function (codeInputSelector) {
        $('body').delegate(codeInputSelector, 'keyup focusout', function (e) {
          var keycode = e.keyCode || e.which;
          // Fire on Enter key (13) OR when focus leaves the field (focusout event)
          if (keycode === 13 || e.type === 'focusout') {
            var conSection   = '[nameaddress="vm.currentShipment.PackageDefaults.Consignee"] ';
            var btnSearchAdd = conSection + 'button[ng-click="search(nameaddress)"]';
            $(btnSearchAdd).trigger('click');  // programmatically click the search button
          }
        });
      },

      // copyOriginToReturnAddress — copies the shipper's origin address into
      // the shipment's return address field.
      //
      // When creating a return label, the return address should be the same as
      // the shipper's address.  This helper does that copy automatically.
      //
      // Optional chaining (?.) prevents a crash if vm.profile is not yet loaded.
      //
      // @param {object} vm - the Angular view-model
      //
      // Examples:
      //   CBRHelper.Address.copyOriginToReturnAddress(vm); // pre-fill return address from shipper profile
      //   CBRHelper.Address.copyOriginToReturnAddress(window._currentViewModel);
      copyOriginToReturnAddress: function (vm) {
        var originAddress = vm?.profile?.UserInformation?.Address;
        if (!originAddress || $.isEmptyObject(originAddress)) {
          console.log('Error: Origin Address not found.');
          return;
        }
        vm.currentShipment.PackageDefaults.ReturnAddress = originAddress;
        console.log('User Origin Address copied to Return Address.');
      },

      // checkForMatchingConsignee — compares the consignee address currently
      // displayed on screen with an address from an incoming customer order.
      //
      // Used when consolidating orders: all orders in a batch must ship to the
      // same address.  If there's a mismatch, we log it and return false so the
      // caller can alert the user.
      //
      // Why JSON.stringify for comparison?  It's simpler than comparing each
      // field individually and handles the structure in one expression.
      // .replace(/ /g, '') removes spaces before comparing to tolerate minor
      // formatting differences.
      //
      // @param {object} customerOrder - an order object from the server with
      //                                 shipAddress1, shipCity, shipState, shipZipCode
      // @returns {boolean}            - true if addresses match
      //
      // Examples:
      //   if (CBRHelper.Address.checkForMatchingConsignee(order)) { CBRHelper.References.updateReferencesWithOrderNumber(order.orderNumber); }
      //   var isMatch = CBRHelper.Address.checkForMatchingConsignee({ shipAddress1: '123 Main St', shipCity: 'Columbus', shipState: 'OH', shipZipCode: '43210-1234', orderNumber: 'ORD-99' });
      checkForMatchingConsignee: function (customerOrder) {
        try {
          var $con = $(_SEL.consignee);  // find the consignee component on the page
          // Build a normalised snapshot of the current on-screen address
          var current = JSON.stringify({
            address: $con.find('input[name="address1"]').val().trim().toUpperCase(),
            city:    $con.find('input[name="city"]').val().trim().toUpperCase(),
            state:   $con.find('input[name="stateProvince"]').val().trim().toUpperCase(),
            zip:     $con.find('input[name="postalCode"]').val().trim().substring(0, 5)  // first 5 digits only
          }).replace(/ /g, '');
          // Build the same snapshot from the incoming order
          var incoming = JSON.stringify({
            address: customerOrder.shipAddress1.trim().toUpperCase(),
            city:    customerOrder.shipCity.trim().toUpperCase(),
            state:   customerOrder.shipState.trim().toUpperCase(),
            zip:     customerOrder.shipZipCode.trim().substring(0, 5)
          }).replace(/ /g, '');
          if (current !== incoming) {
            CBRHelper.Logger.Log({
              Source: 'checkForMatchingConsignee()',
              Message: 'Address mismatch for order ' + customerOrder.orderNumber,
              Data: { current: current, incoming: incoming }
            });
          }
          return current === incoming;
        } catch (error) {
          CBRHelper.Logger.Log({ Source: 'checkForMatchingConsignee()', Error: error });
          return false;
        }
      },

      // syncShipNotificationEmail — copies the ship-notification email from the
      // DOM input into the shipment request object.
      //
      // Angular's two-way binding sometimes doesn't pick up programmatic changes
      // to inputs; reading the value directly from the DOM guarantees we have
      // the latest value before sending the shipment.
      //
      // @param {object} shipmentRequest - the shipment to update (mutated in place)
      //
      // Examples:
      //   CBRHelper.Address.syncShipNotificationEmail(vm.currentShipment); // call before submitting
      //   CBRHelper.Address.syncShipNotificationEmail(shipmentRequest); // ensures DOM value is captured
      syncShipNotificationEmail: function (shipmentRequest) {
        shipmentRequest.Packages[0].ShipNotificationAddressEmail = $(_SEL.shipNotifEmail).val();
      },

      // setConsigneeByReference — looks up a consignee address from a map by
      // a reference key and writes the fields into the shipment's consignee.
      //
      // @param {object} shipmentRequest - the shipment to update
      // @param {string} referenceKey    - the key to look up in addressMap
      // @param {object} addressMap      - map of referenceKey → address objects
      // @param {object} defaultAddress  - fallback if referenceKey isn't in the map
      //
      // Examples:
      //   CBRHelper.Address.setConsigneeByReference(shipmentRequest, order.shipTo, addressBook, defaultAddr);
      //   CBRHelper.Address.setConsigneeByReference(req, 'WAREHOUSE-A', warehouseMap, fallbackWarehouse);
      setConsigneeByReference: function (shipmentRequest, referenceKey, addressMap, defaultAddress) {
        // Use the mapped address if found; otherwise fall back to the default
        var address   = addressMap[referenceKey] || defaultAddress;
        var consignee = shipmentRequest.PackageDefaults.Consignee;
        consignee.Company       = address.Company;
        consignee.Contact       = address.Contact;
        consignee.Address1      = address.Address1;
        consignee.City          = address.City;
        consignee.StateProvince = address.StateProvince;
        consignee.PostalCode    = address.PostalCode;
        if (address.Phone) consignee.Phone = address.Phone;  // phone is optional
      }
    },


    // ========================================================================
    // References — ShipperReference, CustomData propagation
    //
    // Helpers for setting and updating the ShipperReference field and other
    // reference fields on packages.  ShipperReference is typically used as
    // the primary tracking/billing reference (e.g. order number, cost centre).
    // ========================================================================
    References: {

      // setShipperReferenceFromCustomData — reads a custom data field from the
      // origin address and, if it has a value, writes it to ShipperReference
      // for the specified package.
      //
      // Useful when a cost-centre or department code is stored on the user's
      // address record and should automatically populate the reference field.
      //
      // @param {object} shipmentRequest - the full shipment request
      // @param {number} packageIndex    - zero-based package index
      // @param {string} customDataKey   - the CustomData key to read
      //
      // Examples:
      //   CBRHelper.References.setShipperReferenceFromCustomData(vm.currentShipment, 0, 'CostCenter');
      //   CBRHelper.References.setShipperReferenceFromCustomData(req, vm.packageIndex, 'Department');
      setShipperReferenceFromCustomData: function (shipmentRequest, packageIndex, customDataKey) {
        var customData = shipmentRequest.PackageDefaults.OriginAddress.CustomData;
        var value = CBRHelper.Context.getCustomDataValue(customDataKey, customData);
        if (value !== '') {
          // Only override ShipperReference if a value was actually found
          shipmentRequest.Packages[packageIndex].ShipperReference = value;
        }
      },

      // updateReferencesWithOrderNumber — appends an order number to both the
      // ShipperReference and MiscReference7 fields (deduplicating as it goes).
      //
      // Multiple orders can be consolidated onto one shipment, so references
      // accumulate as a comma-separated list.  getUniqueCSVStringFromString
      // ensures no order number appears twice.
      //
      // @param {string} orderNumber - the order number to add
      //
      // Examples:
      //   CBRHelper.References.updateReferencesWithOrderNumber('ORD-12345');
      //   CBRHelper.References.updateReferencesWithOrderNumber(order.orderNumber); // called for each consolidated order
      updateReferencesWithOrderNumber: function (orderNumber) {
        var $shipperRef = $(_SEL.shipperRef);
        var $miscRef7   = $(_SEL.miscRef7);
        var currentVal  = $shipperRef.val() + ',' + orderNumber;  // append new order
        var uniqueVal   = CBRHelper.Utilities.getUniqueCSVStringFromString(currentVal);
        $shipperRef.val(uniqueVal);
        $miscRef7.val(uniqueVal);   // keep both reference fields in sync
      },

      // applyCompoundShipperReference — builds a combined reference string
      // from a custom value and an optional additional reference, then:
      //   1. Writes it to ShipperReference on every package.
      //   2. Validates customValue against a list of allowed values.
      //
      // @param {object}   shipmentRequest - the shipment to update
      // @param {string}   customValue     - the primary reference value
      // @param {string}   [additionalRef] - optional suffix to append
      // @param {object[]} validationList  - list of allowed FieldOptionValidation objects
      //
      // Examples:
      //   CBRHelper.References.applyCompoundShipperReference(req, 'PROJ-42', 'Phase1', vm.fieldOptions.ShipperReference.FieldOptionValidations);
      //   CBRHelper.References.applyCompoundShipperReference(req, costCenter, '', validationList);
      applyCompoundShipperReference: function (shipmentRequest, customValue, additionalRef, validationList) {
        // .trim() removes leading/trailing spaces that result from joining
        var referenceValue = (customValue + ' ' + (additionalRef || '')).trim();
        for (var i = 0; i < shipmentRequest.Packages.length; i++) {
          shipmentRequest.Packages[i].ShipperReference = referenceValue;
        }
        CBRHelper.Validation.validateAgainstFieldOptions(customValue, validationList);
      },

      // stampMiscReference15 — writes a value into MiscReference15 for a
      // specific package.  Used to record audit or tracking stamps.
      //
      // @param {object} shipmentRequest - the shipment to update
      // @param {number} packageIndex    - zero-based package index
      // @param {string} stampValue      - the value to stamp
      //
      // Examples:
      //   CBRHelper.References.stampMiscReference15(vm.currentShipment, 0, CBRHelper.Utilities.dateTimeStamp());
      //   CBRHelper.References.stampMiscReference15(req, vm.packageIndex, 'AUDITED');
      stampMiscReference15: function (shipmentRequest, packageIndex, stampValue) {
        shipmentRequest.Packages[packageIndex].MiscReference15 = stampValue;
      },

      // applyOsuReferences — builds ShipperReference and ConsigneeReference
      // from MiscReference1-6, stamps the UserId in MiscReference20, and
      // clears the ship-notification email for all packages.
      //
      // OSU (Ohio State University) uses a tilde-delimited compound reference:
      //   ShipperReference   = Ref1~Ref2~Ref3~Ref4
      //   ConsigneeReference = Ref5~Ref6
      //
      // Array.map creates a new array, .slice(0, 4) takes the first 4 items,
      // and .join('~') concatenates them with ~ separators.
      //
      // @param {object} shipmentRequest - the shipment to update
      // @param {string} userId          - the current user's ID to stamp
      //
      // Examples:
      //   CBRHelper.References.applyOsuReferences(vm.currentShipment, vm.UserInformation.UserId);
      //   CBRHelper.References.applyOsuReferences(req, CBRHelper.Context.getCurrentUserContext().UserId);
      applyOsuReferences: function (shipmentRequest, userId) {
        var pkg0 = shipmentRequest.Packages[0];
        // Collect MiscReference1 through MiscReference6, defaulting to '' if absent
        var refs = [1, 2, 3, 4, 5, 6].map(function (n) {
          return CBRHelper.Utilities.returnPropertyValue(pkg0['MiscReference' + n]);
        });
        var shipperRef   = refs.slice(0, 4).join('~');  // first 4 refs → shipper reference
        var consigneeRef = refs.slice(4, 6).join('~');  // last 2 refs  → consignee reference
        shipmentRequest.Packages.forEach(function (pkg) {
          pkg.ShipperReference   = shipperRef;
          pkg.ConsigneeReference = consigneeRef;
          pkg.MiscReference20    = userId;  // audit trail: who created this shipment
          pkg.ShipNotificationAddressEmail = '';  // clear notification for this profile type
        });
      }
    },


    // ========================================================================
    // Notifications — email notification defaults, checkbox bindings
    //
    // Manages the three email notification types ShipExec supports:
    //   • Ship notification     — sent when the label is created
    //   • Delivery notification — sent when the package is delivered
    //   • Exception notification — sent if there is a delivery problem
    // ========================================================================
    Notifications: {

      // setNotificationDefaults — enables all three notification types on a
      // package object and sets them all to the same email address.
      //
      // Call this during page initialisation when you want all notifications
      // pre-wired to the user's email.
      //
      // @param {object} pkg   - a package object from shipmentRequest.Packages[]
      // @param {string} email - the email address to receive all notifications
      //
      // Examples:
      //   CBRHelper.Notifications.setNotificationDefaults(vm.currentShipment.Packages[0], 'jdoe@company.com');
      //   CBRHelper.Notifications.setNotificationDefaults(pkg, CBRHelper.Context.getCurrentUserContext().Email);
      setNotificationDefaults: function (pkg, email) {
        pkg.ShipNotificationEmail                     = true;   // enable ship notification
        pkg.ShipNotificationAddressEmail              = email;
        pkg.DeliveryNotificationEmail                 = true;   // enable delivery notification
        pkg.DeliveryNotificationAddressEmail          = email;
        pkg.DeliveryExceptionNotificationEmail        = true;   // enable exception notification
        pkg.DeliveryExceptionNotificationAddressEmail = email;
      },

      // bindNotificationCheckboxes — wires up three notification checkboxes
      // so that when each is clicked, the corresponding email field is auto-filled
      // with the current user's name/email.
      //
      // .delegate() attaches a live event listener to <body> for each checkbox.
      // 'Live' means it will work even for checkboxes added to the DOM later,
      // because the event bubbles up to <body>.
      //
      // The mappings object is: checkboxSelector → emailInputSelector
      //
      // @param {object}   vm          - the Angular view-model (not directly used here)
      // @param {Function} getUserName - function that returns the user's email/name string
      //
      // Examples:
      //   CBRHelper.Notifications.bindNotificationCheckboxes(vm, function() { return vm.UserInformation.Email; });
      //   CBRHelper.Notifications.bindNotificationCheckboxes(vm, function() { return $(_SEL.shipNotifEmail).val(); });
      bindNotificationCheckboxes: function (vm, getUserName) {
        var mappings = {
          '[property="ShipNotificationEmail"]':              '[name="shipNotificationAddressEmail"]',
          '[property="DeliveryExceptionNotificationEmail"]': '[name="DeliveryExceptionNotificationAddressEmail"]',
          '[property="DeliveryNotificationEmail"]':          '[name="DeliveryNotificationEmail"]'
        };
        Object.keys(mappings).forEach(function (checkboxSelector) {
          $('body').delegate(checkboxSelector, 'click', function () {
            // When checkbox is clicked, fill the corresponding email field
            $(mappings[checkboxSelector]).val(getUserName()).change();
          });
        });
      },

      // getShipNotificationEmail — reads the current value from the ship
      // notification email input in the DOM.
      //
      // @returns {string} - the email address currently in the field
      //
      // Examples:
      //   var email = CBRHelper.Notifications.getShipNotificationEmail(); // e.g. 'jdoe@company.com'
      //   console.log('Will notify: ' + CBRHelper.Notifications.getShipNotificationEmail());
      getShipNotificationEmail: function () {
        return $(_SEL.shipNotifEmail).val();
      },

      // persistNotificationEmail — copies the ship notification email from the
      // DOM into the shipment request object for the specified package.
      //
      // Must be called before submitting the shipment to ensure the latest
      // DOM value is captured (Angular binding can lag behind manual DOM writes).
      //
      // @param {object} shipmentRequest - the shipment to update
      // @param {number} packageIndex    - zero-based package index
      //
      // Examples:
      //   CBRHelper.Notifications.persistNotificationEmail(vm.currentShipment, 0);
      //   CBRHelper.Notifications.persistNotificationEmail(shipmentRequest, vm.packageIndex);
      persistNotificationEmail: function (shipmentRequest, packageIndex) {
        shipmentRequest.Packages[packageIndex].ShipNotificationAddressEmail =
          CBRHelper.Notifications.getShipNotificationEmail();
      },

      // setReturnDeliveryAddressEmail — writes an email address into the
      // return-delivery address email field in the DOM.
      //
      // @param {string} email - the email address to set
      //
      // Examples:
      //   CBRHelper.Notifications.setReturnDeliveryAddressEmail('returns@company.com');
      //   CBRHelper.Notifications.setReturnDeliveryAddressEmail(''); // clear the field
      setReturnDeliveryAddressEmail: function (email) {
        $(_SEL.returnDeliveryEmail).val(email);
      },

      // getReturnDeliveryAddressEmail — reads the return-delivery email address
      // from the DOM input field.
      //
      // @returns {string} - the email address currently in the field
      //
      // Examples:
      //   var retEmail = CBRHelper.Notifications.getReturnDeliveryAddressEmail();
      //   shipmentRequest.Packages[0].ReturnDeliveryAddressEmail = CBRHelper.Notifications.getReturnDeliveryAddressEmail();
      getReturnDeliveryAddressEmail: function () {
        return $(_SEL.returnDeliveryEmail).val();
      }
    },


    // ========================================================================
    // Validation — field validation, client-matter checks, state/province
    //
    // Guards that run before a shipment is submitted, ensuring required fields
    // are populated with values that the server will accept.
    // ========================================================================
    Validation: {

      // validateAgainstFieldOptions — checks that a value is in an allowed list
      // (FieldOptionValidations) and throws an error object if it isn't.
      //
      // ShipExec field options are server-configured lookup lists.  When a field
      // has validations, only values in that list are accepted by the server.
      // Throwing here (rather than returning false) lets the caller use a
      // try/catch to handle the failure in one place.
      //
      // @param {string}   value          - the value to validate
      // @param {object[]} validationList - array of { Value: string } objects
      // @throws {object} { message, errorCode } if the value is not in the list
      //
      // Examples:
      //   CBRHelper.Validation.validateAgainstFieldOptions(cm, vm.fieldOptions.ShipperReference.FieldOptionValidations);
      //   try { CBRHelper.Validation.validateAgainstFieldOptions(dept, deptList); } catch(e) { alert(e.message); }
      validateAgainstFieldOptions: function (value, validationList) {
        for (var i = 0; i < validationList.length; i++) {
          if (validationList[i].Value === value) return;  // found — validation passes
        }
        // Not found — throw a structured error (not a real JS Error, just a plain object)
        throw { message: 'Unable to validate shipment', errorCode: '001' };
      },

      // validateClientMatter — validates the ShipperReference ("Client Matter")
      // field against the configured FieldOptionValidations before saving.
      //
      // If the field is empty or invalid, an alert is shown and focus is moved
      // to the field so the user can correct it immediately.
      //
      // vm.saveLicensePlate() is only called when the value is valid.
      //
      // @param {object} vm - the Angular view-model
      //
      // Examples:
      //   CBRHelper.Validation.validateClientMatter(vm); // call before vm.saveLicensePlate()
      //   $('button#btnShip').on('click', function() { CBRHelper.Validation.validateClientMatter(vm); });
      validateClientMatter: function (vm) {
        var cm = vm.currentShipment.Packages[0].ShipperReference;
        if (cm === null) {
          alert('Client Matter field is required. Please enter a valid client matter number!');
          CBRHelper.UI.setFocus('input[name="ShipperReference_txt"]');
          return;
        }
        var validationList = vm.fieldOptions.ShipperReference.FieldOptionValidations;
        try {
          CBRHelper.Validation.validateAgainstFieldOptions(cm, validationList);
          vm.saveLicensePlate();  // proceed to save only when valid
        } catch (e) {
          alert('Client Matter field is invalid. Please enter a valid client matter number!');
          CBRHelper.UI.setFocus('input[name="ShipperReference_txt"]');
        }
      },

      // initCustomStateProvinceSelect — replaces the default state/province text
      // input with a custom <select> drop-down pre-populated with US states and
      // Canadian provinces.
      //
      // Why replace the default input?
      //   ShipExec's default state/province field is a free-text input that lets
      //   users type anything.  Some customers need a validated drop-down instead.
      //
      // The function waits for the original element, reads its HTML attributes
      // (to preserve styling and behaviour), then builds two <select> elements
      // (one for US, one for Canada) and hands them to the caller's changeCallback
      // to swap in at the right time.
      //
      // @param {string}   stateProvinceSelector - CSS selector for the existing input
      // @param {object}   usStates              - map of state codes → state names
      // @param {object}   canadianProvinces      - map of province codes → province names
      // @param {Function} changeCallback         - called with ($original, $usSelect, $caSelect)
      //                                           so the caller can swap the elements
      //
      // Examples:
      //   CBRHelper.Validation.initCustomStateProvinceSelect('input[name="stateProvince"]', usStates, caProvinces, swapFn);
      //   CBRHelper.Validation.initCustomStateProvinceSelect('#stateInput', US_STATES, CA_PROVINCES, function($orig, $us, $ca) { $orig.replaceWith($us); });
      initCustomStateProvinceSelect: function (stateProvinceSelector, usStates, canadianProvinces, changeCallback) {
        _pollForElement(stateProvinceSelector, 30, 10).then(function ($original) {
          if ($original.attr('name') === 'StateProvince') {
            // FieldOption validation is incompatible with the custom select;
            // bail out early to avoid breaking the validation logic
            console.warn('Custom State/Province does not work with FieldOption validation');
            return;
          }
          // Copy the original element's attributes so the new <select> matches
          // the existing CSS class, name, required flag, etc.
          var attrs = {
            'class': $original.attr('class'), name: $original.attr('name'),
            type: $original.attr('type'),
            required: $original.attr('required') !== undefined,
            readonly: $original.attr('readonly') !== undefined
          };
          var $usSelect = _buildSelectForCountry(attrs, usStates);
          var $caSelect = _buildSelectForCountry(attrs, canadianProvinces);
          // Hand both selects to the caller to place in the DOM at the right time
          changeCallback($original, $usSelect, $caSelect);
        });
      }
    },


    // ========================================================================
    // Services — weight-based selection, filtered lists, sort-index
    //
    // Helpers for choosing the right shipping service based on package weight,
    // building a filtered list of available services, and picking the best
    // rated option from a rate-shop result.
    // ========================================================================
    Services: {

      // autoSelectServiceByWeight — examines every package's weight and picks
      // either an express or freight service for the whole shipment.
      //
      // Rule:
      //   • All packages below freightThresholdLbs → use expressServiceSymbol.
      //   • All packages at or above threshold   → use freightServiceSymbol.
      //   • Mixed weights (some above, some below) → alert and abort.
      //
      // Freight packages also get packaging type CUSTOMER_PALLET and the
      // ImportDelivery flag set to true.
      //
      // @param {object} shipmentRequest         - the shipment to update
      // @param {number} freightThresholdLbs      - weight in lbs that triggers freight service
      // @param {string} expressServiceSymbol     - ShipExec symbol for the express service
      // @param {string} freightServiceSymbol     - ShipExec symbol for the freight service
      //
      // Examples:
      //   CBRHelper.Services.autoSelectServiceByWeight(vm.currentShipment, 150, 'CONNECTSHIP_UPS.UPS.2DA', 'CONNECTSHIP_UPS.UPS.FRT');
      //   CBRHelper.Services.autoSelectServiceByWeight(req, 70, 'UPS_GND', 'UPS_LTL'); // packages >= 70 lbs go freight
      autoSelectServiceByWeight: function (shipmentRequest, freightThresholdLbs, expressServiceSymbol, freightServiceSymbol) {
        var hasFreight = false;  // true if at least one package meets the freight threshold
        var hasMixed   = false;  // true if packages are split across both service levels
        shipmentRequest.Packages.forEach(function (pkg) {
          var weight = parseInt(pkg.Weight.Amount, 10);  // parseInt with radix 10 for safety
          if (weight >= freightThresholdLbs) { hasFreight = true; }
          else if (hasFreight) { hasMixed = true; }  // found a lighter pkg after a heavy one
        });
        if (hasMixed) { alert('Cannot create one shipment with 2 different service levels.'); return; }
        shipmentRequest.PackageDefaults.Service = {
          Symbol: hasFreight ? freightServiceSymbol : expressServiceSymbol
        };
        if (hasFreight) {
          // Freight packages need the pallet packaging type and the ImportDelivery flag
          shipmentRequest.Packages.forEach(function (pkg) {
            pkg.Packaging = 'CUSTOMER_PALLET';
            pkg.ImportDelivery = true;
          });
        }
      },

      // buildFilteredServiceList — returns only the services from the profile
      // that match a comma-separated allow-list of service codes.
      //
      // Callers can restrict the available services shown to the user without
      // modifying the server profile — useful for special-purpose shipping pages.
      //
      // @param {object}   profile         - the ShipExec profile object with a Services array
      // @param {string}   serviceCodesCSV - comma-separated list of allowed service codes
      // @param {Function} getServiceSymbol - function(code) → ShipExec service symbol string
      // @returns {object[]}               - filtered array of service objects
      //
      // Examples:
      //   var list = CBRHelper.Services.buildFilteredServiceList(vm.profile, 'GND,2DA,NDA', getServiceSymbol);
      //   vm.filteredServices = CBRHelper.Services.buildFilteredServiceList(profile, allowedCodes, lookupFn);
      buildFilteredServiceList: function (profile, serviceCodesCSV, getServiceSymbol) {
        var filtered = [];
        serviceCodesCSV.split(',').forEach(function (code) {
          var symbol = getServiceSymbol(code.trim().toLowerCase());
          if (!symbol) return;  // skip codes that don't map to a known symbol
          // Find the matching service in the profile and add it to filtered
          for (var i = 0; i < profile.Services.length; i++) {
            if (profile.Services[i].Symbol === symbol) { filtered.push(profile.Services[i]); break; }
          }
        });
        return filtered;
      },

      // selectServiceBySortIndex — returns the service with SortIndex === 0
      // from a rate-shop result array.
      //
      // After rating a shipment, ShipExec returns multiple service options sorted
      // by cost/preference.  SortIndex 0 is the top-ranked (cheapest or preferred)
      // option.  This helper extracts it for automatic selection.
      //
      // @param {object[]} rateResults - array of rate results from the server
      // @returns {object|undefined}   - the service object at SortIndex 0, or undefined
      //
      // Examples:
      //   vm.currentShipment.PackageDefaults.Service = CBRHelper.Services.selectServiceBySortIndex(rateResults);
      //   var bestService = CBRHelper.Services.selectServiceBySortIndex(vm.rateShopResults);
      selectServiceBySortIndex: function (rateResults) {
        for (var i = 0; i < rateResults.length; i++) {
          if (rateResults[i].SortIndex === 0) return rateResults[i].PackageDefaults.Service;
        }
        // Returns undefined implicitly if no result has SortIndex 0
      }
    },


    // ========================================================================
    // D2M — Direct-to-Mobile checks and enablement
    //
    // D2M (Direct-to-Mobile) is a ShipExec feature that routes return labels
    // directly to a recipient's mobile device instead of printing a paper label.
    // Three conditions must all be true for D2M to be active:
    //   1. The logged-in user has D2M enabled in their CustomData.
    //   2. The D2M checkbox on the page is checked.
    //   3. The currently selected shipper account supports D2M.
    // ========================================================================
    D2M: {

      // isCurrentShipperD2MEnabled — checks whether the currently selected
      // shipper account is in the D2M-authorised shipper list.
      //
      // The selected shipper's symbol is read from the shipper drop-down.
      // AngularJS prefixes select values with their type ('string:'), so we
      // strip the first 7 characters ('string:') and uppercase for comparison.
      //
      // Array.some() returns true as soon as it finds a matching element,
      // making it more efficient than iterating the whole array.
      //
      // @param {object[]} authorizedShippers - array of { ShipperSymbol: string } objects
      // @returns {boolean}
      //
      // Examples:
      //   var ok = CBRHelper.D2M.isCurrentShipperD2MEnabled(config.authorizedShippers);
      //   if (CBRHelper.D2M.isCurrentShipperD2MEnabled(authorizedList)) { showD2MCheckbox(); }
      isCurrentShipperD2MEnabled: function (authorizedShippers) {
        var currentSymbol = $(_SEL.shipperChange).val().substring(7).toUpperCase();
        return authorizedShippers.some(function (s) { return s.ShipperSymbol === currentSymbol; });
      },

      // isCurrentUserD2MEnabled — checks whether the logged-in user's CustomData
      // contains the D2M enable flag set to the expected value.
      //
      // @param {object[]} customData      - user address CustomData array
      // @param {string}   d2mCustomDataKey - the key to look for (e.g. 'D2MEnabled')
      // @param {string}   enabledValue    - the value that means "enabled" (e.g. 'Y')
      // @returns {boolean}
      //
      // Examples:
      //   CBRHelper.D2M.isCurrentUserD2MEnabled(address.CustomData, 'D2MEnabled', 'Y');
      //   if (CBRHelper.D2M.isCurrentUserD2MEnabled(customData, 'D2M', 'TRUE')) { enableD2MControls(); }
      isCurrentUserD2MEnabled: function (customData, d2mCustomDataKey, enabledValue) {
        if (!customData) return false;  // no custom data → D2M is not enabled
        for (var i = 0; i < customData.length; i++) {
          if (customData[i].Key === d2mCustomDataKey) {
            // Case-insensitive comparison in case the server returns 'y' or 'Y'
            return customData[i].Value.toUpperCase() === enabledValue.toUpperCase();
          }
        }
        return false;  // key not found → D2M not enabled
      },

      // isCurrentShipmentD2M — the main gate: returns true only when ALL THREE
      // D2M conditions are satisfied simultaneously.
      //
      // @param {object} config - configuration object with:
      //   { customData, d2mKey, enabledValue, checkboxId, authorizedShippers }
      // @returns {boolean}
      //
      // Examples:
      //   var isD2M = CBRHelper.D2M.isCurrentShipmentD2M({ customData: addr.CustomData, d2mKey: 'D2MEnabled', enabledValue: 'Y', checkboxId: 'chkD2M', authorizedShippers: shippers });
      //   if (CBRHelper.D2M.isCurrentShipmentD2M(d2mConfig)) { CBRHelper.D2M.enableD2MShipping(apiRequest, ctx, vm); }
      isCurrentShipmentD2M: function (config) {
        var d2m = CBRHelper.D2M;
        var userOk    = d2m.isCurrentUserD2MEnabled(config.customData, config.d2mKey, config.enabledValue);
        var checked   = $('#' + config.checkboxId).is(':checked');  // is the D2M checkbox ticked?
        var shipperOk = d2m.isCurrentShipperD2MEnabled(config.authorizedShippers);
        return userOk && checked && shipperOk;  // all three must be true
      },

      // enableD2MShipping — calls the server-side EnableD2MShipping UserMethod
      // which activates the D2M feature for the current session and returns an
      // updated profile.  The updated profile is decoded and stored on the vm.
      //
      // @param {Function} apiRequest  - function(method, data, async) → response
      // @param {object}   userContext - current user context for the request
      // @param {object}   vm          - Angular view-model (profile is updated on it)
      //
      // Examples:
      //   CBRHelper.D2M.enableD2MShipping(CBRHelper.API.thinClientApiRequest, CBRHelper.Context.getCurrentUserContext(), vm);
      //   CBRHelper.D2M.enableD2MShipping(apiRequest, userCtx, vm); // call when D2M checkbox is checked
      enableD2MShipping: function (apiRequest, userContext, vm) {
        var params   = { ActionMessage: 'EnableD2MShipping' };
        var data     = _buildUserMethodPayload(params, userContext);
        var response = apiRequest('UserMethod', data, false);  // synchronous call
        if (response && response.responseJSON.ErrorCode !== 0) {
          alert('Error while executing UserMethod: ' + response.responseJSON.ErrorCode);
        } else {
          // Decode the returned profile and update the view-model
          vm.profile = CBRHelper.Logger.decodeReturnString(response.responseJSON.Data);
        }
      },

      // hideD2MCheckbox — waits for the ship-date element to appear (indicating
      // the shipping page is fully rendered) then hides the D2M checkbox.
      //
      // Used on pages where the D2M option should not be visible at all,
      // regardless of user/shipper eligibility.
      //
      // @param {string} d2mTemplateId - the HTML id of the D2M checkbox container
      //
      // Examples:
      //   CBRHelper.D2M.hideD2MCheckbox('divD2MCheckbox');
      //   CBRHelper.D2M.hideD2MCheckbox('templateD2M'); // hides after ship date picker is ready
      hideD2MCheckbox: function (d2mTemplateId) {
        _pollForElement(_SEL.shipDate, 30, 50).then(function () {
          $('#' + d2mTemplateId).hide();
        });
      }
    },


    // ========================================================================
    // Commodities — build, add, remove, move, consolidate
    //
    // Manages the CommodityContents array on each package.  Commodity data is
    // required for international shipments (it populates the commercial invoice)
    // and for duty/tax calculation.
    // ========================================================================
    Commodities: {

      // buildCommodityContentObject — converts a raw order line item from the
      // order system into a ShipExec CommodityContents entry.
      //
      // The + prefix on numeric fields coerces strings to numbers:
      //   +'3.50'  →  3.50  (number)
      //
      // PVTotalWeight and PVTotalValue are pre-calculated totals used for
      // quickly summing package weight/value without re-iterating commodities.
      //
      // @param {object} data - raw order data with quantity, skuId, unitPrice,
      //                        weightLbs, UniqueId, PkgIndex fields
      // @returns {object}    - a ShipExec CommodityContents entry
      //
      // Examples:
      //   var c = CBRHelper.Commodities.buildCommodityContentObject({ quantity: 2, skuId: 'SKU-99', unitPrice: '15.00', weightLbs: '0.5', UniqueId: 'e1a2b3', PkgIndex: 1 });
      //   context.packages[0].CommodityContents.push(CBRHelper.Commodities.buildCommodityContentObject(item));
      buildCommodityContentObject: function (data) {
        return {
          Quantity: data.quantity,
          ProductCode: data.skuId,
          QuantityUnitMeasure: 'EA',  // EA = Each
          UnitValue:  { Currency: 'USD', Amount: +data.unitPrice },   // + coerces to number
          UnitWeight: { Units: 'LB',  Amount: +data.weightLbs },
          UniqueId: data.UniqueId,    // unique key used to track this item in the UI
          PkgIndex: data.PkgIndex || 1,  // which package this belongs to (1-based)
          PVTotalWeight: +data.quantity * +data.weightLbs,   // pre-calculated total weight
          PVTotalValue:  +data.quantity * +data.unitPrice    // pre-calculated total value
        };
      },

      // addCommodityToPackage — pushes a commodity into a package's
      // CommodityContents array and then calls the UI refresh function.
      //
      // _ensureCommodityArray() creates the array if it doesn't exist yet,
      // preventing a 'cannot push to undefined' error.
      //
      // @param {object}   shipmentRequest - the full shipment request
      // @param {number}   packageIndex    - zero-based index of the target package
      // @param {object}   commodity       - the commodity entry to add
      // @param {Function} refreshFn       - called with packageIndex after adding;
      //                                    typically re-renders the commodity table
      //
      // Examples:
      //   CBRHelper.Commodities.addCommodityToPackage(vm.currentShipment, 0, newCommodity, function(i) { CBRHelper.Commodities.refreshCommodityDisplay(); });
      //   CBRHelper.Commodities.addCommodityToPackage(req, vm.packageIndex, scannedItem, refreshFn);
      addCommodityToPackage: function (shipmentRequest, packageIndex, commodity, refreshFn) {
        try {
          _ensureCommodityArray(shipmentRequest, packageIndex).push(commodity);
          refreshFn(packageIndex);  // tell the UI to update the displayed list
        } catch (error) {
          CBRHelper.Logger.Log({ Source: 'addCommodityToPackage()', Error: error });
        }
      },

      // removeCommodityFromPackage — removes the last commodity from a package
      // and refreshes the display.
      //
      // .pop() removes and returns the last element of an array (LIFO order).
      // This is used when undoing the last scan/addition.
      //
      // @param {object}   shipmentRequest - the full shipment request
      // @param {number}   packageIndex    - zero-based index of the target package
      // @param {Function} refreshFn       - called after removal to update the UI
      //
      // Examples:
      //   CBRHelper.Commodities.removeCommodityFromPackage(vm.currentShipment, 0, function() { CBRHelper.Commodities.refreshCommodityDisplay(); });
      //   CBRHelper.Commodities.removeCommodityFromPackage(req, vm.packageIndex, refreshFn); // undo last scan
      removeCommodityFromPackage: function (shipmentRequest, packageIndex, refreshFn) {
        try {
          _ensureCommodityArray(shipmentRequest, packageIndex).pop();
          refreshFn(packageIndex);
        } catch (error) {
          CBRHelper.Logger.Log({ Source: 'removeCommodityFromPackage()', Error: error });
        }
      },

      // refreshCommodityDisplay — forces the AngularJS commodity tab to re-render
      // by simulating a tab click and a pagination button click.
      //
      // Why is this needed?
      //   After pushing items into the CommodityContents array from outside Angular,
      //   the ng-table directive doesn't know the data changed.  Clicking the active
      //   tab and then the pagination button triggers Angular's change detection.
      //
      // Examples:
      //   CBRHelper.Commodities.refreshCommodityDisplay(); // call after programmatically adding a commodity
      //   CBRHelper.Commodities.addCommodityToPackage(req, 0, item, CBRHelper.Commodities.refreshCommodityDisplay);
      refreshCommodityDisplay: function () {
        // Click the active tab to trigger a refresh of the tab content
        $('div.ui-tab-container > div.ng-isolate-scope > ul > li.active > a').click();
        // Click the first non-active pagination button to force the table to re-render
        $('#goods').find('div.ng-table-counts.btn-group').find('button:not(.active):first').click();
      },

      // showAssignGoodsPane — displays the "Assign Goods to Box" side panel and
      // populates it with a list of scannable order line items.
      //
      // Each item gets:
      //   • A random UniqueId for DOM-to-data mapping.
      //   • A cloned <li> element from a hidden template (#liClone).
      //   • Its commodity data pushed into package[0]'s CommodityContents.
      //   • Its weight and value added to the package totals.
      //
      // The panel slides in from the right using jQuery .animate().
      //
      // @param {object}   context  - object with viewModel and packages properties
      // @param {object[]} rawGoods - array of order line items from the server
      //
      // Examples:
      //   CBRHelper.Commodities.showAssignGoodsPane({ viewModel: vm }, orderLineItems);
      //   CBRHelper.Commodities.showAssignGoodsPane(context, goods); // goods is the server response array
      showAssignGoodsPane: function (context, rawGoods) {
        var self = CBRHelper.Commodities;
        $('#listItemContainer li:gt(0)').remove();  // clear all items except the header
        // Resize the tab container to make room for the side panel (toggle between 8/6 columns)
        $('div.ui-tab-container').toggleClass('col-md-8 col-md-6')
          .siblings('div').toggleClass('col-md-4 col-md-6');
        context.packages = context.viewModel.currentShipment.Packages;
        context.goods    = rawGoods.sort(function (a, b) { return a.skuId - b.skuId; });  // sort by SKU
        $(context.goods).each(function (index, item) {
          // Generate a unique id for this list item so we can find it later
          var uniqueKey = 'e' + (Math.random() + 1).toString(36).substring(2);
          item.UniqueId = uniqueKey;
          // Clone the hidden template <li> and attach the item's data to it
          var $item = $('#liClone').clone(true).data(item).attr('id', uniqueKey);
          // Populate the display fields inside the cloned <li>
          $item.find('.goods-sku').html('SKU: <strong>' + item.skuId + '</strong>');
          $item.find('.goods-quantity').html('Quantity: <strong>' + item.quantity + '</strong>');
          $item.find('.goods-declared-value').html('Unit Cost: <strong>$' + item.unitPrice + '</strong>');
          $('#divAssignCommodities ul.list-group').append($item);
          var commodity = self.buildCommodityContentObject(item);
          context.packages[0].CommodityContents.push(commodity);
          var pkg = context.packages[0];
          // Accumulate weight and value totals on the package
          pkg.Weight.Amount              = (pkg.Weight.Amount || 0) + commodity.PVTotalWeight;
          pkg.DeclaredValueAmount.Amount = (pkg.DeclaredValueAmount.Amount || 0) +
                                           Math.round(commodity.PVTotalValue * 100) / 100;  // round to cents
          $item.show();
        });
        // Slide the panel in from the right
        $('#divAssignCommodities').show().animate({ left: 0 }, 400);
        $('.scan-input:visible').first().focus();  // auto-focus the barcode scanner input
        $('#divAssignCommodities').find('[data-toggle="tooltip"]').tooltip();  // init tooltips
      },

      // moveGoodToBox — moves a commodity item from one package to another
      // within the same shipment and recalculates each package's totals.
      //
      // Steps:
      //   1. Flatten all packages' CommodityContents into one list.
      //   2. Find the item by UniqueId.
      //   3. Remove it from the source package and add it to the destination.
      //   4. Update the PkgIndex on the item and animate the UI label.
      //   5. Recalculate weight and declared value for every package.
      //
      // @param {object} context - object with packages array
      // @param {jQuery} $li     - the list item element being moved
      // @param {number} destBox - 1-based destination package index
      //
      // Examples:
      //   CBRHelper.Commodities.moveGoodToBox(context, $(this), 2); // move dragged item to box 2
      //   CBRHelper.Commodities.moveGoodToBox(context, $clickedItem, parseInt($('#boxSelect').val()));
      moveGoodToBox: function (context, $li, destBox) {
        var data     = $li.data();  // read the data attached to this <li> by showAssignGoodsPane
        // .flat() collapses the array-of-arrays into a single flat array
        var allGoods = context.packages.map(function (p) { return p.CommodityContents; }).flat();
        var item     = allGoods.find(function (cc) { return cc.UniqueId === data.UniqueId; });
        if (+item.PkgIndex === +destBox) { console.log('Item already in box.'); return; }  // no-op
        var srcIndex = item.PkgIndex - 1;  // convert 1-based PkgIndex to 0-based array index
        // Remove from source package
        context.packages[srcIndex].CommodityContents =
          context.packages[srcIndex].CommodityContents.filter(function (p) { return p.UniqueId !== data.UniqueId; });
        item.PkgIndex = destBox;  // update the item's package assignment
        (context.packages[destBox - 1].CommodityContents || []).push(item);  // add to destination
        // Animate the package-index label on the list item
        $li.find('.pkg-index-text').fadeOut().promise().done(function () { $(this).text(destBox).fadeIn(); });
        // Re-flatten and recalculate all package totals from scratch
        allGoods = context.packages.map(function (p) { return p.CommodityContents; }).flat();
        allGoods.sort(function (a, b) { return a.PkgIndex - b.PkgIndex; });
        $(allGoods).each(function (idx, cc) {
          var pkg = context.packages[cc.PkgIndex - 1];
          // Reset totals at the first item of each package group
          if (idx === 0 || cc.PkgIndex !== allGoods[idx - 1].PkgIndex) {
            pkg.Weight.Amount = 0; pkg.DeclaredValueAmount.Amount = 0;
          }
          pkg.Weight.Amount += +cc.PVTotalWeight;
          pkg.DeclaredValueAmount.Amount += +cc.PVTotalValue;
        });
      },

      // consolidateInternationalCommodities — merges commodity entries with the
      // same Description across all packages into a single entry on package[0].
      //
      // Why? International commercial invoices typically require one line per
      // product description, not one line per package.  This function:
      //   1. Skips domestic (US) shipments entirely.
      //   2. Walks every package's CommodityContents.
      //   3. If a Description already exists in the consolidated list, adds
      //      the quantity to the existing entry.
      //   4. Otherwise, creates a new export-format entry.
      //   5. Clears all packages' CommodityContents and puts the consolidated
      //      list only on package[0].
      //
      // A Map is used for O(1) description lookups instead of iterating the
      // consolidated array every time.
      //
      // @param {object} shipmentRequest - the shipment to update (mutated in place)
      //
      // Examples:
      //   CBRHelper.Commodities.consolidateInternationalCommodities(vm.currentShipment); // call before rating/shipping
      //   CBRHelper.Commodities.consolidateInternationalCommodities(req); // no-op for domestic (US) shipments
      consolidateInternationalCommodities: function (shipmentRequest) {
        if (shipmentRequest.PackageDefaults.Consignee.Country === 'US') return;  // domestic — skip
        var descriptionIndex = new Map();  // description → index in consolidated array
        var consolidated     = [];
        shipmentRequest.Packages.forEach(function (pkg) {
          if (!pkg.CommodityContents) return;
          pkg.CommodityContents.forEach(function (commodity) {
            if (descriptionIndex.has(commodity.Description)) {
              // Merge: add quantity to the existing entry
              var existing = consolidated[descriptionIndex.get(commodity.Description)];
              existing.Quantity = String(parseInt(existing.Quantity) + parseInt(commodity.Quantity));
            } else {
              // New description: add an export-format entry
              descriptionIndex.set(commodity.Description, consolidated.length);
              consolidated.push(_buildCommodityForExport(commodity));
            }
          });
          pkg.CommodityContents = [];  // clear individual package commodities
        });
        shipmentRequest.Packages[0].CommodityContents = consolidated;  // put merged list on pkg 0
      },

      // addToCommodityList — adds a commodity from either a table row (array of
      // <td> cells) or a bundle object to the shipment's CommodityContents.
      //
      // The isBundle flag distinguishes the two input shapes:
      //   • false / undefined: cellsOrBundle is an array of DOM <td> elements
      //     from a product grid; values are read from specific column indexes.
      //   • true: cellsOrBundle is a bundle/kit object from the order system
      //     with named properties.
      //
      // Note: the duplicate 'var price' declaration is intentional ES5 — both
      // branches declare it in the same function scope (hoisting).
      //
      // @param {object}        vm             - Angular view-model
      // @param {Array|object}  cellsOrBundle  - TD cell array or bundle object
      // @param {number}        packageIndex   - zero-based package index
      // @param {boolean}       isBundle       - true if cellsOrBundle is a bundle object
      //
      // Examples:
      //   CBRHelper.Commodities.addToCommodityList(vm, $('tr.product-row td'), 0, false); // table row cells
      //   CBRHelper.Commodities.addToCommodityList(vm, kitBundleObject, vm.packageIndex, true);
      addToCommodityList: function (vm, cellsOrBundle, packageIndex, isBundle) {
        if (!cellsOrBundle || cellsOrBundle === '') { console.log('Error: Commodity data unavailable'); return; }
        var commodity;
        if (isBundle) {
          // Bundle path: read from a structured bundle/kit object
          // Use ExportUnitPrice if available, otherwise fall back to SaleUnitPrice
          var price = (cellsOrBundle.InternationalInfo && cellsOrBundle.InternationalInfo.ExportUnitPrice)
            ? cellsOrBundle.InternationalInfo.ExportUnitPrice : cellsOrBundle.SaleUnitPrice;
          commodity = {
            UnitValue: { Currency: cellsOrBundle.InternationalInfo.ExportCurrencyCode, Amount: price },
            UnitWeight: { Units: 'LB', Amount: cellsOrBundle.UnitWeight },
            QuantityUnitMeasure: 'PC',
            OriginCountry: cellsOrBundle.InternationalInfo.CountryOfOrigin,
            ProductCode: cellsOrBundle.KitComponentPart,
            HarmonizedCode: cellsOrBundle.InternationalInfo.ExportTariffCode,
            Quantity: cellsOrBundle.UnitsPerAssembly,
            Description: cellsOrBundle.KitComponentPartDesc,
            packageIndex: packageIndex
          };
        } else {
          // Table-cell path: read values from specific column indexes
          // (column positions are defined by the product grid HTML structure)
          var cells = cellsOrBundle;
          // Column 17 = export price; column 4 = fallback sale price
          var price = (cells[17].textContent || cells[17].textContent !== '')
            ? cells[17].textContent : cells[4].textContent;
          commodity = {
            UnitValue: { Currency: cells[15].textContent, Amount: price },
            UnitWeight: { Units: 'LB', Amount: cells[7].textContent },
            QuantityUnitMeasure: 'PC',
            OriginCountry: cells[14].textContent,
            ProductCode: cells[1].textContent,      // SKU/part number column
            HarmonizedCode: cells[16].textContent,  // HS tariff code column
            Quantity: cells[10].textContent,
            Description: cells[5].textContent,
            packageIndex: packageIndex
          };
        }
        _ensureCommodityArray(vm.currentShipment, packageIndex).push(commodity);
      }
    },


    // ========================================================================
    // Orders — consolidated orders dialog, save, process
    //
    // Allows multiple order numbers to be combined onto one shipment label.
    // The workflow: open dialog → scan/type order numbers → save → server
    // validates each order's address → references are updated.
    // ========================================================================
    Orders: {

      // showConsolidatedOrderDialog — opens the "Consolidate Shipments" modal.
      //
      // Before opening, it pre-populates the list with order numbers that are
      // already in the ShipperReference field (from a previous call or a
      // loaded shipment), marking them as already-processed (blue colour).
      //
      // .fadeIn('fast') shows the modal with a quick animation.
      // .promise().done() runs the callback after the animation completes.
      //
      // Examples:
      //   CBRHelper.Orders.showConsolidatedOrderDialog(); // open the consolidation modal
      //   $('#btnConsolidate').on('click', function() { CBRHelper.Orders.showConsolidatedOrderDialog(); });
      showConsolidatedOrderDialog: function () {
        CBRHelper.UI.showOverlay();
        $('#selectConsolidatedOrders').empty();  // clear any leftover options
        var shipperRef     = $(_SEL.shipperRef).val();
        var existingOrders = CBRHelper.Utilities.getUniqueArrayFromString(shipperRef);
        existingOrders.forEach(function (order) {
          $('#selectConsolidatedOrders').append(
            // 'processed' class = already on the shipment (shown in blue)
            $('<option />').val(order).text(order)
              .removeClass('not-processed').addClass('processed').css('color', '#265ca1')
          );
        });
        $('#divModalConsolidateShipments').fadeIn('fast').promise().done(function () {
          $('#textOrderNumber').val('').focus();  // clear input and focus it for immediate typing
        });
      },

      // addOrderNumberToList — adds a new order number typed into #textOrderNumber
      // to the consolidation list, if it isn't already there.
      //
      // Returns false (a jQuery convention to signal 'done') in two cases:
      //   • The input is empty.
      //   • The order number is already in the list.
      //
      // New orders get the 'not-processed' CSS class so they can be sent to
      // the server for validation on save.
      //
      // @returns {boolean} false if the add was skipped
      //
      // Examples:
      //   $('#btnAddOrder').on('click', function() { CBRHelper.Orders.addOrderNumberToList(); });
      //   $('#textOrderNumber').on('keydown', function(e) { if (e.which === 13) CBRHelper.Orders.addOrderNumberToList(); });
      addOrderNumberToList: function () {
        var value = $('#textOrderNumber').val();
        if (!value || value.length === 0) return false;  // nothing typed
        var exists = false;
        // Check whether this order number is already in the list
        $('#selectConsolidatedOrders option').each(function () {
          if (this.text === value) { exists = true; return false; }  // return false stops .each()
        });
        if (exists) { $('#textOrderNumber').val('').focus(); return false; }
        $('#selectConsolidatedOrders').append(
          $('<option />').val(value).text(value).addClass('not-processed').trigger('change')
        ).promise().done(function () { $('#textOrderNumber').val('').focus(); });  // clear and re-focus
      },

      // saveConsolidatedOrderNumbers — collects all 'not-processed' orders from
      // the list, closes the modal, and fires a server request to validate them.
      //
      // Only new (not-processed) orders are sent to the server; already-processed
      // orders are already on the shipment and don't need re-validation.
      //
      // @param {Function} makeRequest     - function(method, data, async, callback)
      //                                     used to call the server
      // @param {Function} processCallback - callback passed to makeRequest;
      //                                     called with order data when the server responds
      //
      // Examples:
      //   CBRHelper.Orders.saveConsolidatedOrderNumbers(CBRHelper.API.makeUserMethodRequest, processOrderData);
      //   $('#btnSaveOrders').on('click', function() { CBRHelper.Orders.saveConsolidatedOrderNumbers(makeReq, handleOrders); });
      saveConsolidatedOrderNumbers: function (makeRequest, processCallback) {
        try {
          var ordersToQuery = '';
          // Collect the text of each 'not-processed' option into a CSV string
          $('#selectConsolidatedOrders > option.not-processed').each(function () {
            ordersToQuery += $(this).text() + ',';
          });
          $('#divModalConsolidateShipments').hide();  // close the dialog immediately
          if (ordersToQuery.length > 0) {
            makeRequest('GetOrderInformation', ordersToQuery, true, processCallback);
          } else {
            CBRHelper.UI.showErrorAlert('ERROR: No unique orders to pull from the server.');
          }
        } catch (error) {
          CBRHelper.Logger.Log({ Source: 'saveConsolidatedOrderNumbers()', Error: error });
        }
      },

      // processOrderData — iterates the server's order response and, for each order:
      //   • Checks the consignee address matches what's on screen.
      //   • If it matches, appends the order number to the shipment's reference fields.
      //   • If it doesn't match, shows an error alert (the order can't be consolidated).
      //
      // @param {object[]} orderDataArray - array of order objects from the server
      //
      // Examples:
      //   CBRHelper.Orders.processOrderData(serverResponse.Orders);
      //   CBRHelper.Orders.processOrderData(data); // data is the callback argument from makeUserMethodRequest
      processOrderData: function (orderDataArray) {
        try {
          orderDataArray.forEach(function (order) {
            if (CBRHelper.Address.checkForMatchingConsignee(order)) {
              CBRHelper.References.updateReferencesWithOrderNumber(order.orderNumber);
            } else {
              CBRHelper.UI.showErrorAlert('ERROR 500: Address mismatch for order ' + order.orderNumber);
            }
          });
        } catch (error) {
          CBRHelper.Logger.Log({ Source: 'processOrderData()', Error: error });
        }
      },

      // closeDialogWithoutSaving — clears the consolidation list and dismisses
      // the modal without sending anything to the server.
      //
      // Also hides the loader spinner, which may have been shown anticipating
      // a server call that isn't going to happen.
      //
      // Examples:
      //   CBRHelper.Orders.closeDialogWithoutSaving(); // bind to Cancel button
      //   $('#btnCancelConsolidate').on('click', function() { CBRHelper.Orders.closeDialogWithoutSaving(); });
      closeDialogWithoutSaving: function () {
        $('#selectConsolidatedOrders').empty();
        $('#divModalConsolidateShipments').hide();
        CBRHelper.UI.hideLoader();
      },

      // initConsolidatedOrdersButton — injects the "Consolidate Orders" button
      // into the top toolbar and enables server-side debug logging if requested.
      //
      // .fadeIn() makes the button appear with a smooth animation after it's
      // appended to the toolbar, so it doesn't just pop in abruptly.
      //
      // @param {boolean} logToServer - if true, enables server-side logging
      //
      // Examples:
      //   CBRHelper.Orders.initConsolidatedOrdersButton(false); // production — console only
      //   CBRHelper.Orders.initConsolidatedOrdersButton(true);  // dev/debug — also logs to server
      initConsolidatedOrdersButton: function (logToServer) {
        $('div.top-tool-bar > div.pull-left').append(
          $('<button />').addClass('btn btn-xs btn-primary')
            .attr('id', 'buttonConsolidateOrders').text('Consolidate Orders').fadeIn()
        );
        CBRHelper.Logger.setServerDebugMode(logToServer);
      }
    },


    // ========================================================================
    // History — search filters, anonymisation, tracking links
    //
    // Helpers for the Shipment History page: narrowing search results,
    // masking sensitive data, adding clickable tracking links, and
    // duplicating historical shipments.
    // ========================================================================
    History: {

      // filterHistoryByUser — adds a WHERE clause to a search criteria object
      // so results are filtered to a specific user.
      //
      // ShipExec's search API uses WhereClauses arrays to build server-side
      // SQL-like filters.  Operator 0 typically means 'equals'.
      //
      // @param {object} searchCriteria - the search criteria object (mutated)
      // @param {string} userId         - the user ID to filter by
      // @param {number} [operator]     - comparison operator; defaults to 0 (equals)
      //
      // Examples:
      //   CBRHelper.History.filterHistoryByUser(vm.searchCriteria, vm.UserInformation.UserId, 0);
      //   CBRHelper.History.filterHistoryByUser(criteria, '5'); // filter to user ID 5 only
      filterHistoryByUser: function (searchCriteria, userId, operator) {
        searchCriteria.WhereClauses.push({ FieldName: 'UserId', FieldValue: userId, Operator: operator || 0 });
      },

      // filterHistoryByBranch — adds a WHERE clause filtering results by a
      // branch number stored in a user's CustomData field.
      //
      // Operator 5 typically means 'starts with' or 'contains' depending on
      // the ShipExec server configuration.
      //
      // @param {object}   searchCriteria  - the search criteria object (mutated)
      // @param {string}   customDataKey   - the CustomData key holding the branch number
      // @param {object[]} customData      - the user's CustomData array
      // @param {Function} getValueByKey   - function(key, data) → string value
      //
      // Examples:
      //   CBRHelper.History.filterHistoryByBranch(criteria, 'BranchNumber', address.CustomData, CBRHelper.Context.getCustomDataValue);
      //   CBRHelper.History.filterHistoryByBranch(vm.searchCriteria, 'BRANCH', customData, getValueByKey);
      filterHistoryByBranch: function (searchCriteria, customDataKey, customData, getValueByKey) {
        var branchNo = getValueByKey(customDataKey, customData);
        if (branchNo) {
          // Only add the clause if a branch number was found in CustomData
          searchCriteria.WhereClauses.push({ FieldName: 'MiscReference20', FieldValue: branchNo, Operator: 5 });
        }
      },

      // enforceHistoryDateRange — sets the start date on the view-model and
      // updates the corresponding WHERE clause in the search criteria to match.
      //
      // Prevents users from searching too far back (which could return huge
      // result sets and time out the server).
      //
      // .setDate() modifies the Date object in place; subtracting daysBack
      // rolls the date back by that many calendar days.
      //
      // @param {object} vm             - Angular view-model (vm.dtstart is updated)
      // @param {object} searchCriteria - the criteria object whose Shipdate clause is updated
      // @param {number} daysBack       - how many days back the earliest allowed date is
      //
      // Examples:
      //   CBRHelper.History.enforceHistoryDateRange(vm, vm.searchCriteria, 30); // limit to last 30 days
      //   CBRHelper.History.enforceHistoryDateRange(vm, criteria, 90);
      enforceHistoryDateRange: function (vm, searchCriteria, daysBack) {
        var startDate = new Date();
        startDate.setDate(startDate.getDate() - daysBack);  // go back N days from today
        vm.dtstart = startDate;  // update the date-picker on the view-model
        searchCriteria.WhereClauses.forEach(function (w) {
          // Find the 'greater than or equal' Shipdate clause (Operator 3) and update it
          if (w.FieldName === 'Shipdate' && w.Operator === 3) {
            w.FieldValue = startDate.toISOString().slice(0, 10);  // 'YYYY-MM-DD' format
          }
        });
      },

      // anonymizeApportionedTotals — hides the ApportionedTotal (shipping cost)
      // from non-admin users by nulling out the amount and currency.
      //
      // Some customers don't want end-users to see actual shipping costs.
      // Admin users (matching adminProfileName) see the real values.
      //
      // @param {object[]} packages        - array of package objects from history results
      // @param {string}   currentProfile  - the active profile name
      // @param {string}   adminProfileName - the profile name that bypasses anonymisation
      //
      // Examples:
      //   CBRHelper.History.anonymizeApportionedTotals(packages, vm.profile.Name, 'Admin');
      //   CBRHelper.History.anonymizeApportionedTotals(response.Packages, currentProfile, 'SuperUser');
      anonymizeApportionedTotals: function (packages, currentProfile, adminProfileName) {
        if (currentProfile === adminProfileName) return;  // admins see real costs
        packages.forEach(function (pkg) { pkg.ApportionedTotal = { Amount: null, Currency: null }; });
      },

      // maskTrackingNumbers — partially obscures 18-digit tracking numbers
      // displayed in the history table, replacing characters 4-9 with 'X'.
      //
      // A MutationObserver watches for new DOM nodes being added to <body>.
      // This is needed because Angular re-renders the table rows dynamically
      // whenever the data changes; a one-time selector wouldn't catch re-renders.
      //
      // Pattern: '1Z2345678901234567' → '1Z2XXXXXX901234567'
      // (First 3 chars kept, 6 masked, then the rest revealed.)
      //
      // Examples:
      //   CBRHelper.History.maskTrackingNumbers(); // call once at page load
      //   // After calling, all 18-char tracking numbers in the DOM are automatically masked
      maskTrackingNumbers: function () {
        var observer = new MutationObserver(function () {
          // Check both div and td elements that use ng-binding (Angular data binding)
          ['div.ng-binding', 'td.ng-binding'].forEach(function (sel) {
            document.querySelectorAll(sel).forEach(function (el) {
              var text = el.textContent.trim();
              if (text.length === 18) {  // only process 18-character strings (tracking numbers)
                el.textContent = text.substring(0, 3) + 'XXXXXX' + text.substring(9);
              }
            });
          });
        });
        // childList: watch for added/removed child elements
        // subtree: watch the entire descendant tree, not just direct children
        observer.observe(document.body, { childList: true, subtree: true });
      },

      // restrictPrintByAge — marks packages as un-printable if their ship date
      // is more than maxDays in the past.
      //
      // Carriers typically don't accept labels older than a few days, so we
      // set CantPrint: true to disable the print button for those packages.
      //
      // Math.floor(Math.abs(diff) / ms-per-day) converts a millisecond
      // difference into whole calendar days.
      //
      // @param {object[]} packages - history package objects (mutated in place)
      // @param {number}   maxDays  - maximum allowed age in calendar days
      //
      // Examples:
      //   CBRHelper.History.restrictPrintByAge(vm.currentPackages, 5);  // disable print if older than 5 days
      //   CBRHelper.History.restrictPrintByAge(searchResults, 3);
      restrictPrintByAge: function (packages, maxDays) {
        var now = new Date();
        packages.forEach(function (pkg) {
          // Reconstruct the ship date from Year/Month/Day fields
          // Month is 1-based from the server, but JS Date() expects 0-based months
          var shipDate = new Date(pkg.Shipdate.Year, pkg.Shipdate.Month - 1, pkg.Shipdate.Day);
          var diffDays = Math.floor(Math.abs(shipDate - now) / (1000 * 60 * 60 * 24));
          if (diffDays > maxDays) pkg.CantPrint = true;
        });
      },

      // addTrackingLinks — wraps raw tracking numbers in the history table with
      // clickable <a> tags that open the carrier's tracking page.
      //
      // UPS tracking numbers start with '1Z'; all others are routed to
      // CH Robinson's tracking portal.
      //
      // .html(function) is a jQuery pattern where passing a function to .html()
      // lets you transform each element's HTML based on its current content.
      //
      // Examples:
      //   CBRHelper.History.addTrackingLinks(); // call after history search results load
      //   // After calling, each tracking number in the table becomes a clickable link
      addTrackingLinks: function () {
        // Wait until at least one data row exists in the detailed history table
        _pollForElement('table[ng-table="vm.tableParamsForDetailed"] tr:gt(0)', 30, 100).then(function () {
          $('table[ng-table="vm.tableParamsForDetailed"] tr:gt(1)')
            .find('td:eq(2) > div:not(.ng-hide)')  // column 2 = tracking number column
            .html(function () {
              var trackingNo = $(this).text().trim();
              var baseUrl = trackingNo.startsWith('1Z')
                ? 'https://wwwapps.ups.com/WebTracking/track?track=yes&trackNums='
                : 'https://online.chrobinson.com/tracking/#/?trackingNumber=';
              return '<a href="' + baseUrl + trackingNo + '" target="_new">' + trackingNo + '</a>';
            });
        });
      },

      // applyBulkFilter — hides rows in the batch detail table that don't match
      // all of the provided filter criteria.
      //
      // filterCriteria is an object where each key matches a column 'title'
      // attribute and each value is the text to filter by.  Empty values are
      // ignored (so partial filters work).
      //
      // .fadeOut('fast') hides non-matching rows with a smooth animation.
      //
      // @param {object} filterCriteria - map of columnTitle → filterValue
      //
      // Examples:
      //   CBRHelper.History.applyBulkFilter({ 'Shipper': 'ACME', 'Service': 'UPS_GND' });
      //   CBRHelper.History.applyBulkFilter({ 'Status': 'DELIVERED' }); // hide non-delivered rows
      applyBulkFilter: function (filterCriteria) {
        $('#tableBatchDetailItems tr:gt(0)').each(function () {
          var $tr = $(this);
          for (var key in filterCriteria) {
            var filterValue = filterCriteria[key].trim().toUpperCase();
            if (filterValue === '') continue;  // skip empty filters
            var cellText = $tr.find('td[title="' + key + '"]').text().trim().toUpperCase();
            if (filterValue !== cellText) $tr.fadeOut('fast');  // hide non-matching rows
          }
        });
      },

      // clearBulkFilter — restores all hidden rows and clears filter input fields.
      //
      // .fadeIn('fast') reveals the previously hidden rows with an animation.
      //
      // Examples:
      //   CBRHelper.History.clearBulkFilter(); // bind to a Reset button
      //   $('#btnResetFilter').on('click', function() { CBRHelper.History.clearBulkFilter(); });
      clearBulkFilter: function () {
        $('#tableBatchDetailItems tr:hidden').fadeIn('fast');  // show all hidden rows
        $('div[title="Filter"] input[type="text"]').each(function () { $(this).val('').change(); });
      },

      // initHistoryPage — sets up the history page's report selector and search
      // button listeners, restricts available reports for non-admin users, and
      // wires up the Summary Report's server-side aggregation call.
      //
      // Inner functions (onReportChange, getSites) use closure to access
      // the outer scope's vm and thinClient without needing parameters.
      //
      // @param {object} vm         - the Angular view-model
      // @param {object} thinClient - thin-client service object
      //
      // Examples:
      //   CBRHelper.History.initHistoryPage(vm, thinClient); // call in $onInit or page load
      //   CBRHelper.History.initHistoryPage(vm, { adminProfileName: 'Admin', httpClient: httpFn });
      initHistoryPage: function (vm, thinClient) {
        vm.history = { searchButton: 'button:contains("Search")', reportSelector: '#reports' };
        $(document).on('click', vm.history.searchButton, onReportChange);
        $(document).on('change', vm.history.reportSelector, onReportChange);
        if (vm.profile.Name !== thinClient.adminProfileName) {
          // Non-admin users: hide the export button and restrict which reports show
          vm.hideButtonExport = true;
          var allowedReports = ['Default', 'Detailed Report', 'Void Report', 'Summary Report'];
          vm.reports = vm.reports.filter(function (report) {
            // Also strip search icons from the report templates to prevent re-searching
            report.TemplateHtml = report.TemplateHtml.replace(/<span.*glyphicon-search.*<\/span>/g, '');
            return allowedReports.includes(report.Name);
          });
        }
        // onReportChange fires when the user clicks Search or changes the report type
        function onReportChange() {
          if (vm.report?.Name !== 'Summary Report') { vm.hideButtonExport = false; return; }
          vm.hideButtonExport = true;
          // Ensure the table container has a scrollbar when the report is tall
          vm.history.carrierAcceptTableContainer = {
            height: $('.panel-body').height() + 'px', overflowY: 'auto'
          };
          var payload = {
            Action: 'summaryReport',
            Data: {
              startDate: vm.dtstart.toLocaleDateString('en-CA'),  // 'en-CA' gives YYYY-MM-DD
              endDate:   vm.dtend.toLocaleDateString('en-CA'),
              sites:     getSites()
            }
          };
          thinClient.httpClient('UserMethod', _buildUserMethodPayload(payload)).then(function (ret) {
            vm.history.carrierAcceptReport = _decodeResponseData(ret.Data);
            if (thinClient.loadData) thinClient.loadData.show();
          });
        }
        // getSites — returns the list of sites to include in the summary report.
        // If the user is scoped to a specific site (SiteId), only include that one;
        // otherwise include all sites.
        function getSites() {
          var sites = vm.UserInformation.SiteId
            ? vm.sites.filter(function (s) { return s.Id === vm.UserInformation.SiteId; })
            : vm.sites;
          return sites.map(function (s) { return { name: s.Name }; });
        }
      },

      // enableHistoryDuplicate — intercepts clicks on duplicate-action links in
      // the history table and stores the serialised shipment for use when the
      // shipping page next loads.
      //
      // The [action-duplicate] attribute on a link contains the full shipment
      // request as a JSON string.  Navigating to '#!/shipping' loads the
      // Angular shipping view, where applyHistoryDuplicate() picks it up.
      //
      // @param {object} cbr - the CBR state object (typically 'window.cbr')
      //
      // Examples:
      //   CBRHelper.History.enableHistoryDuplicate(window.cbr); // call once at history page load
      //   CBRHelper.History.enableHistoryDuplicate(cbr); // clicking a duplicate link stores it on cbr.historyShipment
      enableHistoryDuplicate: function (cbr) {
        $('body').delegate('table a[action-duplicate]', 'click', function () {
          cbr.historyShipment = $(this).attr('action-duplicate');  // store JSON string
          document.location = '#!/shipping';  // navigate to shipping page
        });
      },

      // applyHistoryDuplicate — applies a previously stored historical shipment
      // onto the current shipping form, pre-filling all fields.
      //
      // When duplicating a return shipment, specific return-delivery fields are
      // overridden to fresh values (method, email, description) so the user
      // doesn't accidentally re-use old return settings.
      //
      // cbr.historyShipment is set to null after use to prevent it from being
      // re-applied on the next page load.
      //
      // @param {object} cbr              - the CBR state object
      // @param {object} vm               - the Angular view-model
      // @param {object} shipmentRequest  - the current shipment to overwrite
      // @param {string} defaultDescription - description to use for the return
      //
      // Examples:
      //   CBRHelper.History.applyHistoryDuplicate(cbr, vm, vm.currentShipment, 'Returned Goods');
      //   CBRHelper.History.applyHistoryDuplicate(window.cbr, vm, shipmentRequest, 'Return Shipment');
      applyHistoryDuplicate: function (cbr, vm, shipmentRequest, defaultDescription) {
        if (cbr.historyShipment === null) return;  // nothing to apply
        var historyRequest = JSON.parse(cbr.historyShipment);  // deserialise from JSON string
        vm.totalPackages = historyRequest.Packages.length;     // sync package count in the UI
        shipmentRequest.PackageDefaults = historyRequest.PackageDefaults;
        shipmentRequest.Packages        = historyRequest.Packages;
        shipmentRequest.PackageDefaults.Description = defaultDescription;
        shipmentRequest.Packages.forEach(function (pkg) {
          pkg.ReturnDelivery             = true;   // mark as a return shipment
          pkg.ReturnDeliveryMethod       = 4;      // method 4 = Electronic Return Label
          pkg.ReturnDeliveryAddressEmail = historyRequest.PackageDefaults.Consignee.Email;
          pkg.Description                = defaultDescription;
        });
        cbr.historyShipment = null;             // clear so it isn't applied again
        cbr.bulk.create.itemReferences = [];    // reset bulk-create state
      }
    },


    // ========================================================================
    // Manifest — close manifest, end-of-day
    //
    // A "manifest close" is the end-of-day process where all open shipments
    // are submitted to the carrier for pickup.  These helpers coordinate
    // closing manifests across multiple shippers and/or carriers.
    // ========================================================================
    Manifest: {

      // closeAllShippers — closes the manifest for every shipper account,
      // using a single carrier symbol.
      //
      // Iterates allShippers and calls the private _closeManifestForShipper
      // for each.  We track only the last non-undefined response because the
      // display function needs a single result to show the success/fail status.
      //
      // @param {object}   clientService        - thin-client service object
      // @param {object[]} allShippers          - array of shipper objects
      // @param {string}   selectedCarrierSymbol - the carrier to close for
      // @param {string}   companyId            - company scoping
      // @param {Function} showAlert            - callback(modalId) to display result modal
      //
      // Examples:
      //   CBRHelper.Manifest.closeAllShippers(clientService, vm.profile.Shippers, 'UPS', companyId, showAlert);
      //   CBRHelper.Manifest.closeAllShippers(client, shippers, 'FEDEX', '42', function(id) { $(id).modal('show'); });
      closeAllShippers: function (clientService, allShippers, selectedCarrierSymbol, companyId, showAlert) {
        var lastResponse;
        allShippers.forEach(function (shipper) {
          var result = _closeManifestForShipper(clientService, shipper, selectedCarrierSymbol, companyId);
          if (result) lastResponse = result;  // keep the last meaningful response
        });
        CBRHelper.Manifest.displayCloseManifestResult(lastResponse, showAlert, false);
        $('#loaderspinnerImg').hide();  // hide the spinner when all done
      },

      // closeAllShippersAndCarriers — closes manifests for every combination of
      // shipper × carrier by looping through all carriers and calling
      // closeAllShippers for each.
      //
      // After all carriers are processed, displayCloseManifestResult is called
      // with undefined response and isAllCarriers=true to show the combined status.
      //
      // @param {object}   clientService - thin-client service object
      // @param {object[]} allShippers   - array of shipper objects
      // @param {object[]} allCarriers   - array of carrier objects with Symbol property
      // @param {string}   companyId     - company scoping
      // @param {Function} showAlert     - callback(modalId) to display result modal
      //
      // Examples:
      //   CBRHelper.Manifest.closeAllShippersAndCarriers(clientService, shippers, carriers, companyId, showAlert);
      //   CBRHelper.Manifest.closeAllShippersAndCarriers(client, vm.profile.Shippers, vm.profile.Carriers, '42', alertFn);
      closeAllShippersAndCarriers: function (clientService, allShippers, allCarriers, companyId, showAlert) {
        allCarriers.forEach(function (carrier) {
          // Empty function passed as showAlert to suppress intermediate alerts;
          // only the final combined alert is shown below
          CBRHelper.Manifest.closeAllShippers(clientService, allShippers, carrier.Symbol, companyId, function () {});
        });
        CBRHelper.Manifest.displayCloseManifestResult(undefined, showAlert, true);
        $('#loaderspinnerImg').hide();
      },

      // displayCloseManifestResult — shows the appropriate success or error
      // div/section on the page based on the close-manifest API response.
      //
      // isAllCarriers selects between two sets of element IDs (one set for
      // single-carrier closes, another for all-carriers closes).
      //
      // ErrorCode -1 = API-level failure; ErrorCode !== 0 on the first
      // CloseManifestResult = carrier-level failure.
      //
      // @param {object}   response      - the CloseManifest API response (may be undefined)
      // @param {Function} showAlert     - callback(modalId) to open a Bootstrap modal
      // @param {boolean}  isAllCarriers - true if this was an all-carriers close
      //
      // Examples:
      //   CBRHelper.Manifest.displayCloseManifestResult(response, showAlert, false); // single carrier
      //   CBRHelper.Manifest.displayCloseManifestResult(undefined, showAlert, true); // all carriers
      displayCloseManifestResult: function (response, showAlert, isAllCarriers) {
        var errorSel   = isAllCarriers ? '#divAllshippersAndCarriersErrorMsg'                  : '#divAllshippersErrorMsg';
        var failSel    = isAllCarriers ? '#divAllshipperAndCarrierssunsuccessfulcloseoutMsg'   : '#divAllshippersunsuccessfulcloseoutMsg';
        var successSel = isAllCarriers ? '#divsuccessfulAllShippersAndAllCarriers'             : '#divsuccessfulAllShippers';
        // Hide all result divs first so only the relevant one is visible
        $('#divAllshippersErrorMsg, #divAllshippersunsuccessfulcloseoutMsg, #divAllshippersAndCarriersErrorMsg, #divAllshipperAndCarrierssunsuccessfulcloseoutMsg, #divsuccessfulAllShippers, #divsuccessfulAllShippersAndAllCarriers').hide();
        if (!response || response.ErrorCode === -1) { $(errorSel).show(); showAlert('ErrorModalCloseShippers'); }
        else if (response.CloseManifestResults[0].ErrorCode !== 0) { $(failSel).show(); showAlert('ErrorModalCloseShippers'); }
        else { $(successSel).show(); showAlert('successfulModalCloseShippers'); }
      },

      // processEndOfDay — submits an End-of-Day (EOD) action for the current
      // shipment and hides the modal when the server responds.
      //
      // EOD tells the carrier that all pickups for the day are complete.
      //
      // @param {object}   shipmentRequest - the shipment request (Action='EOD' is added)
      // @param {Function} httpClient      - async HTTP function(method, data)
      // @param {jQuery}   $modal          - the Bootstrap modal to hide on success
      //
      // Examples:
      //   CBRHelper.Manifest.processEndOfDay(vm.currentShipment, thinClient.httpClient, $('#eodModal'));
      //   CBRHelper.Manifest.processEndOfDay(shipmentRequest, httpClient, $modal);
      processEndOfDay: function (shipmentRequest, httpClient, $modal) {
        shipmentRequest.Action = 'EOD';  // mark the request as an end-of-day submission
        httpClient('UserMethod', _buildUserMethodPayload(shipmentRequest)).then(function () {
          $modal.modal('hide');  // close the dialog after the server confirms
        });
      }
    },


    // ========================================================================
    // Batch — lookup, selection, custom processing
    //
    // Provides access to ShipExec batches (named groups of shipments) and a
    // hook for triggering custom batch-processing server-side logic.
    // ========================================================================
    Batch: {

      // getBatches — fetches the list of available batches from the server
      // synchronously and returns the Batches array.
      //
      // The synchronous call (isAsync = false) is intentional here because
      // the result is needed immediately to populate a drop-down.
      //
      // @param {Function} apiRequest - function(method, data, async) → jqXHR
      // @param {string}   companyId  - company scoping the batch list
      // @returns {object[]}          - array of batch objects
      //
      // Examples:
      //   var batches = CBRHelper.Batch.getBatches(apiRequest, companyId);
      //   vm.batches = CBRHelper.Batch.getBatches(CBRHelper.API.thinClientApiRequest, '42');
      getBatches: function (apiRequest, companyId) {
        var batches;
        apiRequest('GetBatches', { SearchCriteria: null, CompanyId: companyId }, false)
          .done(function (response) { batches = response.Batches; });
        return batches;
      },

      // getSelectedBatchName — returns the batch ID of the currently selected
      // option in the batch drop-down.  Delegates to _getSelectedBatchId.
      //
      // @returns {string} - the selected batch ID (without Angular type prefix)
      //
      // Examples:
      //   var batchId = CBRHelper.Batch.getSelectedBatchName(); // e.g. 'MORNING-RUN'
      //   console.log('Processing batch: ' + CBRHelper.Batch.getSelectedBatchName());
      getSelectedBatchName: function () {
        return _getSelectedBatchId();
      },

      // bindCustomBatchProcessing — wires up the custom batch processing button
      // so that clicking it sends a 'CustomBatchProcessing' UserMethod request.
      //
      // This is a hook for site-specific batch post-processing logic that lives
      // on the server.  The button click fires a synchronous call so any errors
      // are handled before the user navigates away.
      //
      // @param {object} client - thin-client object with config and auth helpers
      //
      // Examples:
      //   CBRHelper.Batch.bindCustomBatchProcessing(thinClient); // call once at page load
      //   CBRHelper.Batch.bindCustomBatchProcessing(client); // clicking #btnCustomBatchProcessing fires the server call
      bindCustomBatchProcessing: function (client) {
        $('#btnCustomBatchProcessing').on('click', function () {
          var data = _buildUserMethodPayload({ ActionMessage: 'CustomBatchProcessing' }, client.userContext);
          $.post({
            url: client.config.ShipExecServiceUrl + '/UserMethod',
            data: data, async: false, headers: client.getAuthorizationToken()
          });
        });
      }
    },


    // ========================================================================
    // Profiles — CRUD, apply, return-label creation
    //
    // Shipping profiles are saved shipment templates.  Users can save a set of
    // defaults (service, packaging, reference fields) as a named profile and
    // re-apply it later to avoid re-entering common values.
    // ========================================================================
    Profiles: {

      // loadShippingProfiles — fetches the list of saved shipping profiles from
      // the server and passes them to a callback.
      //
      // Action 'L' = List.  The server returns encoded data; _decodeResponseData
      // converts the Base64 JSON string to a JS array of profile objects.
      //
      // @param {Function} httpClient - async HTTP function(method, data) → Promise
      // @param {Function} callback   - called with the decoded profile array
      //
      // Examples:
      //   CBRHelper.Profiles.loadShippingProfiles(thinClient.httpClient, function(profiles) { vm.shippingProfiles = profiles; });
      //   CBRHelper.Profiles.loadShippingProfiles(httpClient, function(p) { populateProfileDropdown(p); });
      loadShippingProfiles: function (httpClient, callback) {
        var data = _buildUserMethodPayload({ Action: 'L', ShippingProfileName: '' });
        httpClient('UserMethod', data).then(function (ret) { callback(_decodeResponseData(ret.Data)); });
      },

      // saveOrDeleteShippingProfile — saves ('S') or deletes ('D') a shipping
      // profile on the server.
      //
      // The current shipment request is used as the profile template.  For save
      // operations, SelService records which service was selected.
      //
      // @param {string}   action          - 'S' to save, 'D' to delete
      // @param {object}   shipmentRequest - the shipment to use as the template
      // @param {string}   profileName     - the name for the profile
      // @param {string}   selService      - the selected service symbol (save only)
      // @param {Function} httpClient      - async HTTP function
      // @param {Function} callback        - called with the decoded server response
      //
      // Examples:
      //   CBRHelper.Profiles.saveOrDeleteShippingProfile('S', vm.currentShipment, 'My Profile', service.Symbol, httpClient, cb);
      //   CBRHelper.Profiles.saveOrDeleteShippingProfile('D', vm.currentShipment, 'Old Profile', null, httpClient, cb);
      saveOrDeleteShippingProfile: function (action, shipmentRequest, profileName, selService, httpClient, callback) {
        shipmentRequest.Action = action;
        shipmentRequest.ShippingProfileName = profileName;
        if (action === 'S') shipmentRequest.SelService = selService;  // only needed for save
        var data = _buildUserMethodPayload(shipmentRequest);
        httpClient('UserMethod', data).then(function (ret) { callback(_decodeResponseData(ret.Data)); });
      },

      // applyShippingProfile — loads a named profile onto the current shipment
      // and updates the Angular view-model's service selection.
      //
      // structuredClone() creates a deep copy of the profile so that changes
      // to the current shipment don't mutate the stored profile template.
      // (Without cloning, any edit by the user would modify the profile in memory.)
      //
      // ProactiveRecovery instructions are a bitmask:  4096 + 2048 + 32 = three
      // recovery options selected simultaneously.
      //
      // @param {object}   vm               - the Angular view-model
      // @param {object[]} shippingProfiles  - array of loaded profile objects
      // @param {string}   profileName       - the profile to apply
      //
      // Examples:
      //   CBRHelper.Profiles.applyShippingProfile(vm, vm.shippingProfiles, 'Domestic Ground');
      //   CBRHelper.Profiles.applyShippingProfile(vm, profiles, selectedProfileName);
      applyShippingProfile: function (vm, shippingProfiles, profileName) {
        var template = shippingProfiles.find(function (p) { return p.ShippingProfileName === profileName; });
        if (!template) return;  // profile not found — do nothing
        var profile = structuredClone(template);  // deep copy so edits don't affect the template
        var service = { Symbol: profile.SelService };
        if (profile.Packages[0].ProactiveRecovery === true) {
          // Expand the ProactiveRecovery flag into the full instructions array
          profile.Packages[0].SelectedProactiveRecoveryInstructions = [4096, 2048, 32];
        }
        // Always reset the ship date to today (profiles shouldn't carry old dates)
        profile.PackageDefaults.Shipdate = CBRHelper.Utilities.currentShipdate();
        vm.currentShipment     = profile;
        vm.selectedServices    = vm.selectedServices || [];
        vm.selectedServices[0] = service;  // update the service selection in the UI
      },

      // createReturnLabelFromPrevious — sets up the current shipment as a return
      // label by cloning the last completed shipment and resetting specific fields.
      //
      // structuredClone() is used again to avoid mutating lastShipmentRequest.
      // The service is forced to UPS 2-Day Air ('2DA') for return labels.
      //
      // Fields reset to safe defaults for a return:
      //   ReturnDelivery = true      → marks this as a return shipment
      //   ReturnDeliveryMethod = 0   → method 0 = print label in browser
      //   ProactiveRecovery = false  → not applicable for returns
      //   Proof / DirectDelivery = false → standard delivery options off
      //
      // @param {object} vm - Angular view-model with lastShipmentRequest set
      //
      // Examples:
      //   CBRHelper.Profiles.createReturnLabelFromPrevious(vm); // call after a successful ship
      //   $('#btnCreateReturn').on('click', function() { CBRHelper.Profiles.createReturnLabelFromPrevious(vm); });
      createReturnLabelFromPrevious: function (vm) {
        vm.currentShipment = structuredClone(vm.lastShipmentRequest);
        var pkg = vm.currentShipment.Packages[0];
        pkg.ReturnDelivery = true; pkg.ReturnDeliveryMethod = 0;  // print in browser
        pkg.ProactiveRecovery = false; pkg.DirectDelivery = false;
        pkg.Proof = false; pkg.ProofRequireSignature = false;
        vm.currentShipment.PackageDefaults.Service = { Symbol: 'CONNECTSHIP_UPS.UPS.2DA' };
        if (!vm.selectedServices) vm.selectedServices = [];
        vm.selectedServices.push(vm.currentShipment.PackageDefaults.Service);
      }
    },


    // ========================================================================
    // LoadState — capture / restore across PreLoad / PostLoad
    //
    // ShipExec's "Load" operation replaces the entire shipment request with
    // a server-generated template.  These helpers save selected fields before
    // the load and restore them afterwards, preserving user-entered values
    // that the load would otherwise overwrite.
    // ========================================================================
    LoadState: {

      // capturePreLoadState — snapshots the shipper, batch name, and batch ID
      // from the current shipment before a Load operation overwrites them.
      //
      // Returns a plain object — a "saved state" bag — that restorePostLoadState
      // can use to put the values back.
      //
      // @param {object} shipmentRequest - the shipment about to be loaded
      // @returns {{ shipper: object, batchName: string, batchId: string }}
      //
      // Examples:
      //   var saved = CBRHelper.LoadState.capturePreLoadState(vm.currentShipment);
      //   var preState = CBRHelper.LoadState.capturePreLoadState(shipmentRequest); // before calling vm.load()
      capturePreLoadState: function (shipmentRequest) {
        return {
          shipper:   shipmentRequest.PackageDefaults.Shipper,
          batchName: shipmentRequest.Packages[0].MiscReference5,  // batch name stored in MiscRef5
          batchId:   _getSelectedBatchId()                         // read from the DOM drop-down
        };
      },

      // restorePostLoadState — writes the pre-load shipper and batch name back
      // into the shipment request after a Load operation has completed.
      //
      // @param {object} shipmentRequest - the shipment that was just loaded
      // @param {object} savedState      - the object returned by capturePreLoadState
      //
      // Examples:
      //   CBRHelper.LoadState.restorePostLoadState(vm.currentShipment, savedState);
      //   CBRHelper.LoadState.restorePostLoadState(shipmentRequest, preState); // call in PostLoad callback
      restorePostLoadState: function (shipmentRequest, savedState) {
        shipmentRequest.PackageDefaults.Shipper    = savedState.shipper;
        shipmentRequest.Packages[0].MiscReference5 = savedState.batchName;
      },

      // showPostLoadError — checks whether the Load operation returned an error
      // code and, if so, shows the message to the user as an alert.
      //
      // ErrorCode 1 indicates a server-side error with a human-readable message.
      // We reset ErrorCode to 0 after showing it to prevent the alert from
      // appearing again if the page is refreshed.
      //
      // @param {object} shipmentRequest - the just-loaded shipment
      //
      // Examples:
      //   CBRHelper.LoadState.showPostLoadError(vm.currentShipment); // call in PostLoad
      //   CBRHelper.LoadState.showPostLoadError(shipmentRequest); // shows alert if ErrorCode === 1
      showPostLoadError: function (shipmentRequest) {
        if (shipmentRequest.PackageDefaults.ErrorCode === 1) {
          alert(shipmentRequest.PackageDefaults.ErrorMessage);
          shipmentRequest.PackageDefaults.ErrorCode = 0;  // clear after showing
        }
      },

      // showPostLoadUserDataAlert — displays a UserData1 message to the user
      // in the appropriate modal.
      //
      // UserData1 is a free-text field the server can use to send messages
      // to the user at load time.  A leading '~' character signals that a
      // "new shipment" modal should be used instead of the default alert modal.
      //
      // @param {object} shipmentRequest - the just-loaded shipment
      // @param {jQuery} $alertModal     - the standard alert Bootstrap modal
      // @param {jQuery} $newShipModal   - the "new shipment" Bootstrap modal
      //
      // Examples:
      //   CBRHelper.LoadState.showPostLoadUserDataAlert(vm.currentShipment, $alertModal, $newShipModal);
      //   CBRHelper.LoadState.showPostLoadUserDataAlert(req, $('#alertModal'), $('#newShipModal'));
      showPostLoadUserDataAlert: function (shipmentRequest, $alertModal, $newShipModal) {
        var userData = shipmentRequest.Packages[0].UserData1;
        if (!userData || userData.length === 0) return;  // no message to show
        if (userData.charAt(0) === '~') {
          // '~' prefix → use the new-shipment modal and strip the prefix from the message
          shipmentRequest.AlertTitle   = 'ShipExec Alert';
          shipmentRequest.AlertMessage = userData.substring(1);  // remove the leading '~'
          $newShipModal.modal('show');
        } else {
          // No prefix → use the standard alert modal
          shipmentRequest.AlertTitle   = 'ShipExec Alert';
          shipmentRequest.AlertMessage = userData;
          $alertModal.modal('show');
        }
      },

      // restoreConsigneeEmail — writes the consignee email from the shipment
      // object back into the DOM input after a Load operation.
      //
      // Angular's Load operation may not restore the email field in the DOM
      // (it's nested inside the name-address component), so we do it manually.
      //
      // @param {object} shipmentRequest - the just-loaded shipment
      //
      // Examples:
      //   CBRHelper.LoadState.restoreConsigneeEmail(vm.currentShipment);
      //   CBRHelper.LoadState.restoreConsigneeEmail(shipmentRequest); // call after PostLoad completes
      restoreConsigneeEmail: function (shipmentRequest) {
        $(_SEL.consigneeEmail).val(shipmentRequest.PackageDefaults.Consignee.Email);
      }
    },


    // ========================================================================
    // Billing — third-party billing rules, EU logic
    //
    // Third-party billing means the shipping cost is charged to an account
    // number that belongs to neither the shipper nor the consignee.
    // These helpers determine when that should be enabled and apply the
    // correct billing address automatically.
    // ========================================================================
    Billing: {

      // applyThirdPartyBillingRules — decides whether third-party billing should
      // be enabled for the current shipment based on country and EU membership.
      //
      // Logic:
      //   • US or CA shippers, or missing destination → disable 3PB.
      //   • Ship from ≠ ship to, OR both countries are in the EU → enable 3PB.
      //   • Non-US origin → also switch weight/dimension units to metric.
      //
      // @param {object}   shipmentRequest - the current shipment
      // @param {Function} isMemberOfEu    - function(countryCode) → boolean
      //
      // Examples:
      //   CBRHelper.Billing.applyThirdPartyBillingRules(vm.currentShipment, function(c) { return EU_COUNTRIES.includes(c); });
      //   CBRHelper.Billing.applyThirdPartyBillingRules(req, isEuMember); // call in shipperChange handler
      applyThirdPartyBillingRules: function (shipmentRequest, isMemberOfEu) {
        var shipFrom = shipmentRequest.PackageDefaults.OriginAddress.Country;
        var shipTo   = shipmentRequest.PackageDefaults.Consignee.Country;
        if (!shipTo || shipFrom === 'US' || shipFrom === 'CA') {
          // Domestic US/CA or unknown destination — 3PB not applicable
          CBRHelper.Billing.disableThirdPartyBillingElements(); return;
        }
        // Enable 3PB if shipping internationally or between two EU countries
        var shouldEnable = (shipFrom !== shipTo) || (isMemberOfEu(shipFrom) && isMemberOfEu(shipTo));
        if (shouldEnable) CBRHelper.Billing.setThirdPartyBilling(shipmentRequest);
        if (shipFrom !== 'US') {
          // International origins use metric units
          _setInputValue(_SEL.weightUnits, 'string:KG');   // kilograms
          _setInputValue(_SEL.dimensionUnits, 'number:1'); // centimetres
        }
      },

      // setThirdPartyBilling — enables the third-party billing UI controls and
      // writes the hard-coded third-party billing address into the shipment.
      //
      // .prop() and .attr() are both set for maximum jQuery/AngularJS compatibility
      // (older Angular versions sometimes require the attribute as well as the property).
      //
      // @param {object} shipmentRequest - the shipment to update
      //
      // Examples:
      //   CBRHelper.Billing.setThirdPartyBilling(vm.currentShipment); // enable 3PB and set address
      //   CBRHelper.Billing.setThirdPartyBilling(req); // writes PRA Global billing address
      setThirdPartyBilling: function (shipmentRequest) {
        $(_SEL.thirdPartyButton).prop('disabled', false).attr('disabled', false);
        $(_SEL.thirdPartyCheckbox).prop('checked', true).attr('checked', true).change();
        // Hard-coded billing address for PRA Global — all international charges go here
        shipmentRequest.PackageDefaults.ThirdPartyBillingAddress = {
          Company: 'PRA Global', Address1: '995 Research Park Blvd', Address2: 'Suite 300',
          City: 'Charlottesville', StateProvince: 'VA', PostalCode: '22911',
          Account: '883F79', Country: 'US'
        };
      },

      // disableThirdPartyBillingElements — unchecks the 3PB checkbox and
      // disables the 3PB button when 3PB is not applicable.
      //
      // This is the mirror of setThirdPartyBilling and should be called
      // whenever the conditions for 3PB are no longer met.
      //
      // Examples:
      //   CBRHelper.Billing.disableThirdPartyBillingElements(); // call when shipper changes to US
      //   CBRHelper.Billing.disableThirdPartyBillingElements(); // call when destination clears
      disableThirdPartyBillingElements: function () {
        $(_SEL.thirdPartyCheckbox).prop('checked', false).attr('checked', false);
        $(_SEL.thirdPartyButton).prop('disabled', true).attr('disabled', true);
      }
    },


    // ========================================================================
    // ReturnDelivery — address, config toggle, insurance
    //
    // Manages the UI for return shipments: populating the return address from
    // the shipper's profile, toggling the return-delivery option, and setting
    // insurance based on the destination country.
    // ========================================================================
    ReturnDelivery: {

      // loadReturnAddressFromShipper — waits for the second 'company' input to
      // appear (index 1 = return address section) and fills in the shipper's
      // address fields.
      //
      // ':eq(1)' targets the second matching element (0-indexed), not the first.
      // The fields and props arrays are zipped together by index in the forEach loop.
      //
      // @param {object} vm - the Angular view-model with profile.Shippers[0]
      //
      // Examples:
      //   CBRHelper.ReturnDelivery.loadReturnAddressFromShipper(vm);
      //   CBRHelper.ReturnDelivery.loadReturnAddressFromShipper(window._currentViewModel);
      loadReturnAddressFromShipper: function (vm) {
        _pollForElement('[name="company"]:eq(1)', 30, 150).then(function () {
          var shipper = vm.profile.Shippers[0];
          if (!shipper || !shipper.Company) return;  // no shipper configured
          var fields = ['company', 'address1', 'city', 'stateProvince', 'postalCode', 'phone', 'contact'];
          var props  = ['Company', 'Address1', 'City', 'StateProvince', 'PostalCode', 'Phone', 'Contact'];
          // Set each field's DOM value to the matching shipper property
          fields.forEach(function (field, i) {
            _setInputValue('[name="' + field + '"]:eq(1)', shipper[props[i]]);
          });
          // Country needs the 'string:' prefix because it's a ShipExec <select> value
          _setInputValue('[name="Country"]:eq(1)', 'string:' + shipper.Country);
        });
      },

      // setReturnDeliveryConfiguration — configures the form for a return
      // shipment: sets the description, selects the return-delivery option,
      // and fills in the return address.
      //
      // @param {object} vm - the Angular view-model
      //
      // Examples:
      //   CBRHelper.ReturnDelivery.setReturnDeliveryConfiguration(vm); // call when return toggle is enabled
      //   CBRHelper.ReturnDelivery.setReturnDeliveryConfiguration(window._currentViewModel);
      setReturnDeliveryConfiguration: function (vm) {
        _setDescription('Returned Goods');   // standard description for returns
        _setReturnDeliveryCheckbox(true);    // select the return-delivery option
        CBRHelper.ReturnDelivery.loadReturnAddressFromShipper(vm);
      },

      // clearReturnDeliveryConfiguration — resets all return-delivery fields
      // to their defaults (empty description, cleared email, deselected option).
      //
      // Called when the user switches away from a return-delivery workflow.
      //
      // Examples:
      //   CBRHelper.ReturnDelivery.clearReturnDeliveryConfiguration(); // call when return toggle is disabled
      //   CBRHelper.ReturnDelivery.clearReturnDeliveryConfiguration(); // resets description, email, and checkbox
      clearReturnDeliveryConfiguration: function () {
        _setDescription('');
        CBRHelper.Notifications.setReturnDeliveryAddressEmail('');
        _setReturnDeliveryCheckbox(false);
        $(_SEL.invoiceMethod).val('');  // clear commercial invoice method
      },

      // setReturnDeliveryInsurance — enables the "insured" option for
      // international return shipments.
      //
      // International returns typically require insurance (carrier policy).
      // US/USA destinations are excluded because domestic returns don't need it.
      //
      // 'number:1' is the Angular-prefixed value for the "insured" select option.
      //
      // @param {boolean} enabled         - if false (or shipmentRequest is null), does nothing
      // @param {object}  shipmentRequest - the shipment to check the destination country on
      //
      // Examples:
      //   CBRHelper.ReturnDelivery.setReturnDeliveryInsurance(true, vm.currentShipment); // auto-insure international returns
      //   CBRHelper.ReturnDelivery.setReturnDeliveryInsurance(false, null); // no-op
      setReturnDeliveryInsurance: function (enabled, shipmentRequest) {
        if (!shipmentRequest || !enabled) return;
        var country = shipmentRequest.PackageDefaults.Consignee.Country.toUpperCase();
        // Select 'insured' only for non-US destinations
        $('select option[value="number:1"]').prop('selected', country !== 'US' && country !== 'USA');
      },

      // onShipperChangeD2M — updates return-delivery settings when the user
      // changes the shipper account on a D2M-capable page.
      //
      // If the new shipper is the D2M shipper and the user is D2M-capable:
      //   → configure for return delivery.
      // Otherwise:
      //   → clear the return delivery fields.
      //
      // @param {string}  currentShipper   - the newly selected shipper symbol
      // @param {string}  d2mShipperSymbol - the shipper symbol that supports D2M
      // @param {boolean} isD2MCapable     - whether the user has D2M enabled
      // @param {object}  vm               - the Angular view-model
      //
      // Examples:
      //   CBRHelper.ReturnDelivery.onShipperChangeD2M($(_SEL.shipperChange).val(), 'D2M_SHIPPER', isD2MCapable, vm);
      //   CBRHelper.ReturnDelivery.onShipperChangeD2M(newShipperSymbol, config.d2mShipperSymbol, userIsD2M, vm);
      onShipperChangeD2M: function (currentShipper, d2mShipperSymbol, isD2MCapable, vm) {
        if (isD2MCapable && currentShipper === d2mShipperSymbol) {
          _setDescription('Returned Goods');
          CBRHelper.ReturnDelivery.loadReturnAddressFromShipper(vm);
        } else {
          // Not D2M — clear return delivery settings
          _setDescription('');
          CBRHelper.Notifications.setReturnDeliveryAddressEmail('');
          $(_SEL.invoiceMethod).val('');
        }
      }
    },


    // ========================================================================
    // Printing — traveler labels, ZPL, document download
    //
    // Helpers for generating and downloading shipping documents:
    // PDF labels opened in a new tab, raw ZPL with default printer settings
    // prepended, and file downloads via dynamic <a> tag.
    // ========================================================================
    Printing: {

      // printTravelerLabel — submits the shipment request to the server, receives
      // a PDF label back, and opens it in a new browser tab.
      //
      // 'data:application/pdf;base64,...' is a Data URL — a way to embed a
      // file's binary content directly in a URL so the browser can open it
      // without a separate HTTP request.
      //
      // window.open() opens the PDF in a new tab ('_blank').
      //
      // @param {object}   shipmentRequest - the shipment data for label generation
      // @param {Function} apiRequest      - function(method, data, async) → jqXHR
      //
      // Examples:
      //   CBRHelper.Printing.printTravelerLabel(vm.currentShipment, thinClient.apiRequest);
      //   CBRHelper.Printing.printTravelerLabel(shipReq, apiRequest); // opens PDF in a new tab
      printTravelerLabel: function (shipmentRequest, apiRequest) {
        var data     = _buildUserMethodPayload(shipmentRequest);
        var result   = apiRequest('UserMethod', data, false).responseJSON;  // synchronous
        var response = _decodeResponseData(result.Data);
        var pdf      = response.DocumentResponses[0].PdfData[0];  // first document, first page
        window.open('data:application/pdf;base64,' + pdf, '_blank');  // open PDF in new tab
        alert('Delegate Label Created');
      },

      // prependZplDefaults — inserts standard ZPL printer initialisation commands
      // before the raw ZPL label data if the document matches the expected symbol.
      //
      // ZPL (Zebra Programming Language) is the command language for Zebra
      // label printers.  The prepended commands set:
      //   ^XA        → start label
      //   ^LH0,0     → label home position (top-left)
      //   ^XSY,Y     → enable sensor for continuous paper
      //   ^MD30      → media darkness 30 (print density)
      //   ^XZ        → end label
      //
      // atou / utoa are custom Base64 helper functions (not shown here) that
      // decode and re-encode the raw ZPL data.
      //
      // @param {object} doc            - a document response object with DocumentSymbol and RawData
      // @param {string} expectedSymbol - only modify documents matching this symbol
      //
      // Examples:
      //   CBRHelper.Printing.prependZplDefaults(response.Documents[0], 'ZEBRA_LABEL');
      //   documents.forEach(function(d) { CBRHelper.Printing.prependZplDefaults(d, 'ZPL_4X6'); });
      prependZplDefaults: function (doc, expectedSymbol) {
        if (doc.DocumentSymbol !== expectedSymbol || !doc.RawData) return;  // wrong type or no data
        var printerDefaults = '^XA^LH0,0^XSY,Y^MD30^XZ\n';
        var originalRaw     = atou(doc.RawData);         // decode Base64 → raw ZPL string
        doc.RawData[0]      = utoa(printerDefaults + originalRaw);  // prepend + re-encode
      },

      // downloadFile — fetches a file from the server and triggers a browser
      // download using a dynamically created, auto-clicked <a> element.
      //
      // Why create a hidden <a> tag?
      //   The browser's built-in 'download' attribute on an <a> tag triggers a
      //   file save dialog when clicked.  We create one, click it, and remove
      //   it — the user never sees it but gets the download prompt.
      //
      // @param {string}   fileName   - the name of the file on the server
      // @param {Function} httpClient - async HTTP function(method, data) → Promise
      //
      // Examples:
      //   CBRHelper.Printing.downloadFile('template.pdf', thinClient.httpClient);
      //   CBRHelper.Printing.downloadFile(selectedFileName, httpClient); // triggers browser Save dialog
      downloadFile: function (fileName, httpClient) {
        var data = _buildUserMethodPayload({ Action: 'downloadFile', Data: fileName });
        httpClient('UserMethod', data).then(function (ret) {
          if (ret.ErrorCode !== 0) return;  // server-side error — abort
          var fileData     = _decodeResponseData(ret.Data);
          // Build a Data URL from the Base64-encoded file content
          var linkData     = 'data:' + fileData.fileType + ';base64,' + fileData.encodedFile;
          var downloadLink = document.createElement('a');
          downloadLink.style.display = 'none';          // invisible element
          downloadLink.download = fileData.fileName;    // sets the suggested filename
          downloadLink.href    = linkData;
          document.body.appendChild(downloadLink);
          downloadLink.click();                         // trigger the download
          document.body.removeChild(downloadLink);      // clean up the DOM
        });
      }
    },


    // ========================================================================
    // Files — upload, download, paperless
    //
    // Helpers for reading local files from the user's machine via the browser's
    // FileReader API and sending their contents to the server.
    // ========================================================================
    Files: {

      // uploadCarrierLabelFile — reads a file from a file input and returns its
      // Base64 content (minus the Data URL prefix) via a callback.
      //
      // If no file has been selected yet, it opens the file-picker dialog.
      // FileReader.readAsDataURL() converts the file to a Base64 string.
      // The 'onload' callback strips the MIME type prefix (e.g. 'data:image/png;base64,')
      // so the caller receives only the raw Base64 data.
      //
      // $input.val(null) resets the input so the same file can be selected again
      // on subsequent clicks.
      //
      // @param {string}   fileInputSelector - CSS selector for the <input type="file">
      // @param {Function} onReady           - called with the Base64 string when ready
      //
      // Examples:
      //   CBRHelper.Files.uploadCarrierLabelFile('#labelFileInput', function(b64) { vm.labelData = b64; });
      //   CBRHelper.Files.uploadCarrierLabelFile('.carrier-upload', function(data) { sendToServer(data); });
      uploadCarrierLabelFile: function (fileInputSelector, onReady) {
        var $input    = $(fileInputSelector);
        var imageFile = $input.prop('files')[0];  // the first selected file (or undefined)
        if (!imageFile) { $input.click(); return; }  // no file yet — open the picker
        var reader    = new FileReader();
        reader.readAsDataURL(imageFile);  // async: starts reading; triggers onload when done
        reader.onload = function () {
          // Strip the 'data:mime/type;base64,' prefix, keeping only the Base64 payload
          onReady(this.result.replace(/^data:.+;base64,/, ''));
          $input.val(null);  // reset so the user can upload the same filename again
        };
      },

      // uploadReplacementFile — reads a File object and sends its Base64 content
      // to the server under a given key.
      //
      // Used to replace a server-side file (e.g. a configuration template) with
      // one the user selects locally.  The server stores it by Key.
      //
      // reader.readAsDataURL(file) — triggers onload with a Data URL string.
      // .split(',')[1] — takes everything after the comma (the Base64 part only).
      //
      // @param {File}     file       - the File object from an <input type="file">
      // @param {string}   serverKey  - the server-side storage key
      // @param {Function} httpClient - async HTTP function(method, data)
      //
      // Examples:
      //   CBRHelper.Files.uploadReplacementFile(e.target.files[0], 'config_template', thinClient.httpClient);
      //   CBRHelper.Files.uploadReplacementFile(pickedFile, 'customs_doc', httpClient);
      uploadReplacementFile: function (file, serverKey, httpClient) {
        var reader = new FileReader();
        reader.onload = function () {
          var request = {
            Key: serverKey,
            // JSON.stringify bundles file metadata and content into one string
            Value: JSON.stringify({ fileType: file.type, fileName: file.name, encodedFile: this.result.split(',')[1] })
          };
          httpClient('UserMethod', _buildUserMethodPayload({ Action: 'updateFile', Data: request }));
        };
        reader.readAsDataURL(file);
      },

      // bindPaperlessFileInput — wires up a file input so that when a file is
      // selected, its Base64 content is stored on vmInstance.paperless for later
      // submission as a paperless document attachment.
      //
      // Event delegation (binding to 'body') handles inputs that may be added
      // to the DOM dynamically after this call.
      //
      // e.data is the vmInstance passed through the jQuery event system;
      // this is the recommended way to pass outer-scope data into delegated handlers.
      //
      // @param {string} fileInputSelector - CSS selector for the paperless file input
      // @param {object} vmInstance        - the Angular view-model instance
      //
      // Examples:
      //   CBRHelper.Files.bindPaperlessFileInput('#paperlessInput', vm);
      //   CBRHelper.Files.bindPaperlessFileInput('.paperless-upload', $scope.vm); // wire on page load
      bindPaperlessFileInput: function (fileInputSelector, vmInstance) {
        $('body').on('change', fileInputSelector, vmInstance, function (e) {
          var file = e.target.files[0];
          if (!file) { e.data.paperless = null; return; }  // no file selected — clear
          var reader = new FileReader();
          reader.readAsDataURL(file);
          reader.onloadend = function () {
            // Store { fileName, fileData } on the vm so it's included in the next ship request
            e.data.paperless = { fileName: file.name, fileData: this.result.split(',')[1] };
          };
        });
      }
    },


    // ========================================================================
    // Camera — video stream, capture, stop
    //
    // Accesses the device camera via the browser's MediaDevices API to capture
    // images (e.g. of barcodes or documents) directly from a web page.
    // Only works in secure contexts (HTTPS or localhost).
    // ========================================================================
    Camera: {

      // startVideoStream — requests camera access and streams it into a <video>
      // element, restoring the user's last saved zoom level.
      //
      // navigator.mediaDevices.getUserMedia() prompts the user for camera
      // permission and returns a Promise that resolves with a MediaStream.
      //
      // The video constraints ask for:
      //   • No audio (we only need video)
      //   • Rear-facing camera (environment) if available
      //   • High resolution (2560×1440 ideal) for clear barcode scanning
      //   • Zoom support
      //
      // The zoom level is persisted in localStorage so repeat visits start at
      // the same zoom the user last used.
      //
      // @param {string} videoElementId - the id of the <video> element
      // @param {object} videoModal     - object to store zoom settings on
      //
      // Examples:
      //   CBRHelper.Camera.startVideoStream('cameraPreview', vm.cameraModal);
      //   CBRHelper.Camera.startVideoStream('barcodeVideo', { zoom: null, track: null });
      startVideoStream: function (videoElementId, videoModal) {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
          // Camera API not supported in this browser (e.g. older IE)
          console.error('Your browser does not support the getUserMedia API.'); return;
        }
        var constraints = {
          audio: false,
          video: { zoom: true, facingMode: 'environment', width: { ideal: 2560 }, height: { ideal: 1440 } }
        };
        navigator.mediaDevices.getUserMedia(constraints).then(function (stream) {
          var video = document.getElementById(videoElementId);
          video.srcObject = stream;  // attach the live stream to the <video> element
          var track        = stream.getVideoTracks()[0];      // the first (only) video track
          var capabilities = track.getCapabilities();         // what the camera hardware supports
          // Initialise zoom metadata if not already set
          videoModal.zoom = videoModal.zoom || {
            min: capabilities.zoom.min, max: capabilities.zoom.max, current: track.getSettings().zoom
          };
          // Restore the last zoom level the user used, or the camera's default
          var savedZoom = localStorage.getItem('zoomLevel') || track.getSettings().zoom;
          localStorage.setItem('zoomLevel', savedZoom);   // persist for next session
          track.applyConstraints({ advanced: [{ zoom: savedZoom }] });  // apply to live track
          videoModal.track = track;  // store track reference so zoom slider can adjust it later
        }).catch(function (error) { console.error('Error accessing the camera:', error); });
      },

      // captureImage — grabs a single frame from the live video stream and
      // returns it as a Base64-encoded PNG string.
      //
      // ImageCapture.grabFrame() returns a Promise that resolves with an
      // ImageBitmap (raw pixel data).  We draw it onto an off-screen <canvas>
      // and then use canvas.toDataURL() to get the Base64 PNG.
      //
      // The regex strips the 'data:image/png;base64,' prefix so the caller
      // receives only the Base64 payload.
      //
      // @param {string}  videoElementId - the id of the <video> element
      // @returns {Promise<string>}       - resolves with Base64-encoded PNG
      //
      // Examples:
      //   CBRHelper.Camera.captureImage('cameraPreview').then(function(b64) { vm.scannedImage = b64; });
      //   CBRHelper.Camera.captureImage('barcodeVideo').then(function(img) { uploadImage(img); });
      captureImage: function (videoElementId) {
        var video = document.getElementById(videoElementId);
        var track = video.srcObject.getVideoTracks()[0];  // the active video track
        return new ImageCapture(track).grabFrame().then(function (imageBitmap) {
          // Create an off-screen canvas sized to match the captured frame
          var canvas = document.createElement('canvas');
          var ctx    = canvas.getContext('2d');
          canvas.width = imageBitmap.width; canvas.height = imageBitmap.height;
          ctx.drawImage(imageBitmap, 0, 0);  // paint the frame onto the canvas
          // quality 1.0 = maximum PNG quality; strip the prefix before returning
          return canvas.toDataURL('image/png', 1.0).replace(/^data:.+;base64,/, '');
        });
      },

      // stopVideoStream — stops all tracks in the video stream, releasing the
      // camera hardware and turning off the camera indicator light.
      //
      // Setting srcObject to null disconnects the <video> element from the stream.
      // Always call this when done with the camera to free up the device.
      //
      // @param {string} videoElementId - the id of the <video> element
      //
      // Examples:
      //   CBRHelper.Camera.stopVideoStream('cameraPreview'); // call when closing the camera modal
      //   CBRHelper.Camera.stopVideoStream('barcodeVideo');
      stopVideoStream: function (videoElementId) {
        var video = document.getElementById(videoElementId);
        if (video.srcObject) {
          // Stop each track individually; this releases the hardware resource
          video.srcObject.getTracks().forEach(function (track) { track.stop(); });
          video.srcObject = null;  // detach the stream from the video element
        }
      }
    },


    // ========================================================================
    // LTL — NMFC dimension matching, dry ice, package configuration
    //
    // LTL (Less-Than-Truckload) shipments require specific dimension/description
    // formats for carrier BOL (Bill of Lading) generation and NMFC classification.
    // ========================================================================
    LTL: {

      // LTL_DIMENSION_TABLE — an array of [length, width] pairs representing
      // the standard pallet and container dimensions used by this customer.
      //
      // matchLtlDimensions uses this table to map arbitrary package dimensions
      // to the nearest standard dimension string for the BOL comment field.
      // Each row is [the larger dim, the smaller dim] for the standard size.
      LTL_DIMENSION_TABLE: [
        [34, 32], [48, 40], [52, 36],   // standard pallets (first 3 rows checked differently)
        [12, 52], [14, 44], [18, 32],   // smaller container sizes
        [18, 52], [28, 44], [37, 37]    // additional standard sizes
      ],

      // matchLtlDimensions — maps a package's L×W×H to a standardised dimension
      // string based on the table above.
      //
      // Why sort the dimensions first?
      //   The table stores pairs in a canonical order, but the package dimensions
      //   can arrive in any order.  Sorting descending lets us compare the largest
      //   and second-largest values against each table entry regardless of how
      //   the user entered them.
      //
      // The first 3 rows use one matching strategy (check dims[0] and dims[1]);
      // the rest use a different strategy that checks all three positions.
      //
      // Returns 'NxNxN' with the original values if no match is found.
      //
      // @param {number} length - package length (any unit)
      // @param {number} width  - package width
      // @param {number} height - package height
      // @returns {string}      - e.g. '48x40x12' or '0x0x0' for empty dimensions
      //
      // Examples:
      //   var dimStr = CBRHelper.LTL.matchLtlDimensions(48, 40, 12); // returns '48x40x12'
      //   var dimStr = CBRHelper.LTL.matchLtlDimensions(0, 0, 0);   // returns '0x0x0'
      matchLtlDimensions: function (length, width, height) {
        var dims = [length || 0, width || 0, height || 0];
        dims.sort(function (a, b) { return b - a; });  // sort descending: [largest, mid, smallest]
        if (dims[0] === 0 && dims[1] === 0 && dims[2] === 0) return '0x0x0';  // all zero
        var table = CBRHelper.LTL.LTL_DIMENSION_TABLE;
        for (var i = 0; i < table.length; i++) {
          var x = table[i][0], y = table[i][1];
          if (i < 3) {
            // First 3 rows: check if the two largest dims match this table entry
            if (dims[0] === x && dims[1] === y) return x + 'x' + y + 'x' + dims[2];
            if (dims[1] === x && dims[2] === y) return x + 'x' + y + 'x' + dims[0];
          } else {
            // Remaining rows: check all three positional combinations
            if (dims[0] === y && dims[1] === x) return x + 'x' + y + 'x' + dims[2];
            if (dims[0] === y && dims[2] === x) return x + 'x' + y + 'x' + dims[1];
            if (dims[1] === y && dims[2] === x) return x + 'x' + y + 'x' + dims[0];
          }
        }
        return dims.join('x');  // no standard match — return the raw sorted dimensions
      },

      // configureLtlPackage — applies LTL-specific settings to one package:
      //   • Appends the matched dimension string to the BOL comment.
      //   • Sets the description to the NMFC description + package number.
      //   • Copies MiscReference20 to WaybillBolNumber if not already set.
      //   • Sets the ParentContainerCode on the next package for pallet grouping.
      //
      // Note: packageIndex is 1-based here (first package = 1), but the Packages
      // array is 0-based, so we use packageIndex - 1 to access the array.
      //
      // Optional chaining (?.) prevents a crash when accessing the next package
      // that may not exist yet.
      //
      // @param {object} shipmentRequest  - the full shipment request
      // @param {number} packageIndex     - 1-based package number
      // @param {string} nmfcDescription  - the NMFC freight class description
      //
      // Examples:
      //   CBRHelper.LTL.configureLtlPackage(vm.currentShipment, 1, 'Industrial Machinery');
      //   for (var i = 1; i <= pkgCount; i++) { CBRHelper.LTL.configureLtlPackage(req, i, nmfc); }
      configureLtlPackage: function (shipmentRequest, packageIndex, nmfcDescription) {
        var pkg = shipmentRequest.Packages[packageIndex - 1];  // convert to 0-based
        if (!pkg) return;  // package doesn't exist — do nothing
        var d         = pkg.Dimensions || {};  // use empty object if Dimensions is undefined
        var dimString = CBRHelper.LTL.matchLtlDimensions(d.Length, d.Width, d.Height);
        if (dimString !== '0x0x0') {
          // Append the dimension string to the BOL comment with a leading space
          pkg.BolComment = ((pkg.BolComment || '') + ' ' + dimString).trim();
        }
        pkg.Description = nmfcDescription + ' L' + packageIndex;  // e.g. 'Freight Goods L1'
        if (!pkg.WaybillBolNumber) pkg.WaybillBolNumber = pkg.MiscReference20;  // default BOL number
        if (pkg.MiscReference20 && !shipmentRequest.Packages[packageIndex]?.ParentContainerCode) {
          shipmentRequest.Packages[packageIndex] = shipmentRequest.Packages[packageIndex] || {};
          if (!shipmentRequest.Packages[packageIndex].ParentContainerCode) {
            // Build a unique container code that ties packages to the same pallet
            shipmentRequest.Packages[packageIndex].ParentContainerCode = 'P' + pkg.MiscReference20 + '-' + packageIndex;
          }
        }
      },

      // configureDryIce — applies dry-ice shipping settings to all packages that
      // have MiscReference7 set (the dry-ice indicator field).
      //
      // Business rules:
      //   • Multiple packages with dry ice are not supported (throw error).
      //   • MiscReference8 must contain the dry ice weight in kg (throw if missing).
      //   • DryIceRegulationSet: 1 = domestic (US), 2 = international.
      //   • DryIcePurpose 2 = "Packed with dry ice" (IATA DGR category).
      //
      // 'kgs' suffix is stripped from the weight string because the API expects
      // a plain number.
      //
      // @param {object} shipmentRequest - the shipment to configure
      // @throws {object} if multiple packages have dry ice or weight is missing
      //
      // Examples:
      //   CBRHelper.LTL.configureDryIce(vm.currentShipment); // apply before submitting
      //   try { CBRHelper.LTL.configureDryIce(req); } catch(e) { alert(e.message); }
      configureDryIce: function (shipmentRequest) {
        var rpv = CBRHelper.Utilities.returnPropertyValue;  // shorthand alias
        var dryIceWeightRaw  = rpv(shipmentRequest.Packages[0].MiscReference8);  // weight in kg (e.g. '3kgs')
        var consigneeCountry = shipmentRequest.PackageDefaults.Consignee.Country;
        shipmentRequest.Packages.forEach(function (pkg) {
          if (!pkg.MiscReference7) return;  // no dry ice indicator on this package — skip
          if (shipmentRequest.Packages.length > 1) throw { message: 'Multiple Packages Not Supported with Dry Ice', errorCode: '' };
          if (rpv(pkg.MiscReference8) === '') throw { message: 'Missing Dry Ice Weight', errorCode: '' };
          pkg.DryIceWeight        = { Amount: dryIceWeightRaw.replace('kgs', ''), Units: 'KG' };
          pkg.DryIceRegulationSet = (consigneeCountry === 'US') ? 1 : 2;  // 1=domestic, 2=intl
          pkg.DryIcePurpose       = 2;  // IATA DGR: packed with dry ice
        });
      }
    },


    // ========================================================================
    // Shipment — convenience class with CustomData getters
    //
    // A lightweight class (constructor function) that wraps a shipment request
    // object and adds computed properties for reading CustomData fields without
    // calling getCustomField() manually each time.
    //
    // Usage:
    //   var s = new CBRHelper.Shipment(shipmentRequest);
    //   var dept = s.UserRef1;  // reads Custom1 from OriginAddress.CustomData
    //   var toRef = s.ToRef3;   // reads Custom3 from Consignee.CustomData
    // ========================================================================
    Shipment: (function () {
      // ShipmentClass is a constructor function (the 'class' in ES5 terms).
      // Calling 'new ShipmentClass(req)' creates an instance with .ShipmentRequest.
      function ShipmentClass(shipmentRequest) {
        this.ShipmentRequest = shipmentRequest;
      }
      // GetCustom is a helper method on the prototype (shared by all instances)
      // that delegates to Context.getCustomField.
      //
      // Examples:
      //   var s = new CBRHelper.Shipment(req); var dept = s.GetCustom('Custom1', 'User');
      //   var s = new CBRHelper.Shipment(req); var dest = s.GetCustom('Custom3', 'To');
      ShipmentClass.prototype.GetCustom = function (fieldName, source) {
        return CBRHelper.Context.getCustomField(fieldName, source, this.ShipmentRequest);
      };
      // Dynamically add UserRef1–10 and ToRef1–10 as computed (get-only) properties.
      //
      // Object.defineProperty adds a property with a custom getter function.
      // Computed properties run their getter function every time the property
      // is read (e.g. s.UserRef1 calls the getter, which calls GetCustom).
      //
      // 'User' = origin address CustomData; 'To' = consignee CustomData.
      [1, 2, 3, 4, 5, 6, 7, 8, 9, 10].forEach(function (n) {
        Object.defineProperty(ShipmentClass.prototype, 'UserRef' + n, {
          get: function () { return this.GetCustom('Custom' + n, 'User'); }  // e.g. Custom1 from origin
        });
        Object.defineProperty(ShipmentClass.prototype, 'ToRef' + n, {
          get: function () { return this.GetCustom('Custom' + n, 'To'); }   // e.g. Custom1 from consignee
        });
      });
      return ShipmentClass;
    })(),


    // ========================================================================
    // PageInit — onPageLoaded, focus, IOR checkbox, commodity modal, cost center
    //
    // One-time setup functions called when an Angular view finishes loading.
    // They perform DOM customisations that can't be done in the Angular template.
    // ========================================================================
    PageInit: {

      // onPageLoaded — entry point for page-specific initialisation.
      //
      // location is the Angular route (e.g. '/shipping', '/history').
      // options is an open-ended config object; supported keys:
      //   • sortShippers  {boolean}  - sort the shippers list alphabetically
      //   • initShipping  {Function} - called to run shipping-page setup
      //
      // @param {string} location - the current Angular route path
      // @param {object} vm       - the Angular view-model
      // @param {object} [options] - optional configuration flags
      //
      // Examples:
      //   CBRHelper.PageInit.onPageLoaded($location.path(), vm, { sortShippers: true, initShipping: myInit });
      //   CBRHelper.PageInit.onPageLoaded('/shipping', vm, { initShipping: function() { setupUI(); } });
      onPageLoaded: function (location, vm, options) {
        options = options || {};  // default to empty object so we can safely access keys
        if (location === '/shipping') {
          if (options.sortShippers) vm.profile.Shippers.sort(CBRHelper.Utilities.compareByKey('Name'));
          if (typeof options.initShipping === 'function') options.initShipping();
        }
      },

      // focusOnLoadInput — waits for the load-value input to appear and moves
      // keyboard focus to it, then calls initializeCustomElements.
      //
      // On the Load/Batch page the input is the primary interaction point;
      // pre-focusing it saves the user a click.
      //
      // Examples:
      //   CBRHelper.PageInit.focusOnLoadInput(); // call once on the Load page after Angular renders
      //   options.initShipping = CBRHelper.PageInit.focusOnLoadInput;
      focusOnLoadInput: function () {
        CBRHelper.DOM.waitForElement(_SEL.loadValue, true, null, null, CBRHelper.PageInit.initializeCustomElements);
      },

      // initializeCustomElements — runs element-level setup after the page loads.
      //
      // Currently: unchecks the "Do Not Rate Shop" checkbox so it never starts
      // checked by default, regardless of the user's last session state.
      //
      // Examples:
      //   CBRHelper.PageInit.initializeCustomElements(); // called automatically by focusOnLoadInput
      //   CBRHelper.DOM.waitForElement('#myEl', true, null, null, CBRHelper.PageInit.initializeCustomElements);
      initializeCustomElements: function () {
        $('#chkDoNotRateShop').prop('checked', false);
      },

      // addIorSameAsConsigneeCheckbox — injects a "Same As Consignee" checkbox
      // before the IOR (Importer of Record) button.
      //
      // The IOR address can optionally mirror the consignee address.  This
      // checkbox gives the user a one-click way to enable that.
      //
      // _pollForElement waits for the IOR button to appear (it may be inside
      // a tab that isn't visible yet).  When found, we insert the checkbox
      // using .before() and wire its click event.
      //
      // @param {string}   iorBtnSelector - CSS selector for the IOR button
      // @param {Function} onChecked      - called with (true/false) when the checkbox changes
      // @returns {jQuery}                - the created checkbox element (or undefined while pending)
      //
      // Examples:
      //   CBRHelper.PageInit.addIorSameAsConsigneeCheckbox('#btnIorAddress', function(checked) { vm.iorSameAsConsignee = checked; });
      //   CBRHelper.PageInit.addIorSameAsConsigneeCheckbox(iorBtnSel, function(v) { applyIorCopy(v); });
      addIorSameAsConsigneeCheckbox: function (iorBtnSelector, onChecked) {
        var $checkbox;
        _pollForElement(iorBtnSelector, 30, 10).then(function ($target) {
          $checkbox = $('<input type="checkbox">');
          $checkbox.click(function (e) { onChecked(e.target.checked); });  // pass true/false
          // Insert checkbox and label immediately before the IOR button
          $target.before($checkbox, '<label style="padding-left: 6px;">Same As Consignee</label>');
        });
        return $checkbox;  // may be undefined until the promise resolves
      },

      // hideCommodityModalTabs — hides specific tabs inside the commodity modal
      // by their zero-based index.
      //
      // Some clients don't use certain commodity entry methods (e.g. the
      // barcode-scanner tab or the manual-entry tab).  Hiding unwanted tabs
      // simplifies the UI without removing the underlying functionality.
      //
      // The 'index' attribute on each <li> is set by the ui-bootstrap tab directive.
      //
      // @param {jQuery}   $commodityModal - the commodity modal jQuery element
      // @param {number[]} hideIndexes     - array of tab indexes to hide
      //
      // Examples:
      //   CBRHelper.PageInit.hideCommodityModalTabs($('#commodityModal'), [1, 2]); // hide tabs at index 1 and 2
      //   CBRHelper.PageInit.hideCommodityModalTabs($modal, [0]); // hide the first tab only
      hideCommodityModalTabs: function ($commodityModal, hideIndexes) {
        $commodityModal.find('li.uib-tab.nav-item').each(function () {
          if (hideIndexes.indexOf(parseInt(this.getAttribute('index'))) > -1) $(this).hide();
        });
      },

      // loadCostCenter — reads a cost-centre code from the user's CustomData
      // and pre-fills the ShipperReference field with it, then disables the
      // field so the user can't change it.
      //
      // Cost centres are often mandatory and fixed per user, so pre-filling and
      // locking the field prevents accidental (or deliberate) changes.
      //
      // Optional chaining (?.) prevents crashes when the profile or CustomData
      // isn't loaded yet.
      //
      // @param {object} vm        - the Angular view-model
      // @param {string} customKey - the CustomData key holding the cost-centre code
      //
      // Examples:
      //   CBRHelper.PageInit.loadCostCenter(vm, 'CostCenter'); // pre-fills ShipperReference with user's cost centre
      //   CBRHelper.PageInit.loadCostCenter($scope.vm, 'Department');
      loadCostCenter: function (vm, customKey) {
        var customData = vm?.profile?.UserInformation?.Address?.CustomData;
        if (!customData) return;  // no custom data available
        var entry = customData.find(function (item) { return item.Key === customKey; });
        if (!entry) return;  // cost-centre key not found
        vm.currentShipment.Packages[vm.packageIndex].ShipperReference = entry.Value;
        // Disable the input so the user cannot modify the pre-filled value
        $('[ng-model="vm.currentShipment.Packages[vm.packageIndex].ShipperReference"]').prop('disabled', true);
      }
    }

  }; // end of returned object (the public CBRHelper API)

})(); // end of IIFE — CBRHelper is now set to the returned object
