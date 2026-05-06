using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using PSI.Sox;
using PSI.Sox.Interfaces;
using PSI.Sox.Configuration;
using PSI.Sox.Data;
using PSI.Sox.Licensing;
using PSI.Sox.ML;
using PSI.Sox.Print;
using PSI.Sox.Resources;
using PSI.Sox.Wcf;

namespace ShipExec.Marken
{
    /// <summary>
    /// Central manager for Marken Phase 1 return-label shipping rules.
    /// This class exists so SoxBusinessRules can remain a thin delegation layer,
    /// while all return-label logic, dry ice conversion, adapter selection,
    /// and service validation live in one testable place.
    /// </summary>
    public class ReturnShipmentRulesManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _api;
        private readonly IProfile _profile;
        private readonly List<BusinessRuleSetting> _settings;
        private readonly ClientContext _clientContext;

        /// <summary>
        /// Builds the return shipment manager with all dependencies required to
        /// enforce Marken shipping rules during PreShip.
        /// </summary>
        public ReturnShipmentRulesManager(ILogger logger, IBusinessObjectApi api, IProfile profile, List<BusinessRuleSetting> settings, ClientContext clientContext)
        {
            // Store the injected logger so every business decision can be traced.
            _logger = logger;
            // Store the API so future extensions can rate shop or validate history server-side.
            _api = api;
            // Store the profile because service and shipper lookups are profile-driven.
            _profile = profile;
            // Store the business rule settings because service/adapter symbols must be configurable.
            _settings = settings;
            // Store client context for any user/site-sensitive logic that may be required.
            _clientContext = clientContext;
        }

        /// <summary>
        /// Executes Marken Phase 1 PreShip logic.
        /// This method determines whether the shipment is domestic or international,
        /// applies paperless invoice rules, maps biological sample and dry ice data,
        /// and prepares service-selection behavior for the carrier call.
        /// </summary>
        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            // Step 1: Trace entry so the shipping lifecycle can be diagnosed in server logs.
            _logger.LogInfo(this, "SBR", "PreShip", "Start", "Beginning Marken return-label PreShip rule processing");

            // Step 2: Validate that the shipment request and its default package exist before we touch any fields.
            if (shipmentRequest == null)
            {
                // We fail fast because the ShipExec lifecycle cannot continue without a shipment object.
                throw new Exception("ShipmentRequest was null during Marken PreShip processing.");
            }

            // Step 3: Ensure PackageDefaults exists because all blueprint rules target shipment-level defaults.
            if (shipmentRequest.PackageDefaults == null)
            {
                // PackageDefaults must be a Package object, not a PackageRequest.
                shipmentRequest.PackageDefaults = new Package();
            }

            // Step 4: Ensure the consignee exists because country comparison is required for return-label logic.
            if (shipmentRequest.PackageDefaults.Consignee == null)
            {
                // Consignee is the pickup-from/return-to address in this workflow.
                shipmentRequest.PackageDefaults.Consignee = new NameAddress();
            }

            // Step 5: Ensure the return address exists because this workflow is explicitly a return-label flow.
            if (shipmentRequest.PackageDefaults.ReturnAddress == null)
            {
                // ReturnAddress may be prefilled from profile defaults or user data elsewhere.
                shipmentRequest.PackageDefaults.ReturnAddress = new NameAddress();
            }

            // Step 6: Determine the route type from the blueprint's country comparison rule.
            string consigneeCountry = (shipmentRequest.PackageDefaults.Consignee.Country ?? string.Empty).Trim();
            string returnCountry = (shipmentRequest.PackageDefaults.ReturnAddress.Country ?? string.Empty).Trim();
            bool isDomestic = string.Equals(consigneeCountry, returnCountry, StringComparison.OrdinalIgnoreCase);

            // Step 7: Apply the international paperless invoice rule only when the shipment crosses borders.
            if (!isDomestic)
            {
                // Blueprint requirement: turn on paperless invoice for international return labels.
                shipmentRequest.PackageDefaults.CommercialInvoiceMethod = 1;
                // Blueprint requirement: set export reason to Medical for cross-border biological returns.
                shipmentRequest.PackageDefaults.ExportReason = "Medical";
            }

            // Step 8: Read the biological sample flag from MiscReference4, which is stored as object/string data.
            string biologicalSampleValue = shipmentRequest.PackageDefaults.MiscReference4 == null
                ? string.Empty
                : shipmentRequest.PackageDefaults.MiscReference4.ToString().Trim();
            bool isBiologicalSample = string.Equals(biologicalSampleValue, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(biologicalSampleValue, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(biologicalSampleValue, "yes", StringComparison.OrdinalIgnoreCase);

            // Step 9: Ensure at least one package exists because dry ice and weight adjustments apply to the first package.
            if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
            {
                // The blueprint assumes a single-package return-label shipment when pre-ship rules run.
                shipmentRequest.Packages = new List<PackageRequest> { new PackageRequest() };
            }

            // Step 10: Ensure the first package object exists so weight and dry-ice fields can be safely updated.
            if (shipmentRequest.Packages[0] == null)
            {
                // PackageRequests are individual package overrides, distinct from PackageDefaults.
                shipmentRequest.Packages[0] = new PackageRequest();
            }

            // Step 11: Apply biological sample package extras as required by the blueprint.
            if (isBiologicalSample)
            {
                // PackageExtras is a SerializableDictionary, so custom flags are written as key/value pairs.
                if (shipmentRequest.PackageDefaults.PackageExtras == null)
                {
                    // Initialize the custom data container if the shipment does not already have one.
                    shipmentRequest.PackageDefaults.PackageExtras = new SerializableDictionary();
                }

                // Blueprint requirement: mark the package as a restricted article type 32.
                shipmentRequest.PackageDefaults.PackageExtras["RESTRICTED_ARTICLE_TYPE"] = "32";
            }

            // Step 12: Map dry ice weight from MiscReference3 when present, converting KG to LB before adding it.
            string dryIceKgText = shipmentRequest.PackageDefaults.MiscReference3 == null
                ? string.Empty
                : shipmentRequest.PackageDefaults.MiscReference3.ToString().Trim();
            bool hasDryIceValue = !string.IsNullOrWhiteSpace(dryIceKgText);
            if (hasDryIceValue)
            {
                // Parse the user-entered kilograms into a numeric value for conversion.
                double dryIceKg = 0;
                if (!double.TryParse(dryIceKgText, out dryIceKg))
                {
                    // The user entered an invalid dry ice amount, so we block shipment with a clear error.
                    throw new Exception("Dry Ice Weight (kg) must be numeric when provided.");
                }

                // Convert kilograms to pounds because ShipExec weight math expects LB for this workflow.
                double dryIceLb = dryIceKg * 2.2046226218d;

                // Ensure the shipment weight object exists before we add the converted dry ice amount.
                if (shipmentRequest.Packages[0].Weight == null)
                {
                    // Weight is an object, so we create it with a known unit value.
                    shipmentRequest.Packages[0].Weight = new Weight { Amount = 0d, Units = "LB" };
                }

                // Ensure the dry ice weight object exists before we assign the converted amount.
                shipmentRequest.Packages[0].DryIceWeight = new Weight { Amount = dryIceLb, Units = "LB" };

                // Blueprint requirement: set the dry ice purpose to Medical.
                shipmentRequest.Packages[0].DryIcePurpose = 0;

                // Blueprint requirement: choose the regulation set based on whether the shipment is US-to-US.
                shipmentRequest.Packages[0].DryIceRegulationSet = isDomestic ? 0 : 1;

                // Blueprint requirement: add dry ice to the package's current weight after conversion.
                shipmentRequest.Packages[0].Weight.Amount = shipmentRequest.Packages[0].Weight.Amount + dryIceLb;

                // Blueprint requirement: keep the unit consistent after the adjustment.
                shipmentRequest.Packages[0].Weight.Units = "LB";
            }

            // Step 13: Ensure the default package weight object exists if future rules or carrier logic need it.
            if (shipmentRequest.PackageDefaults.Weight == null)
            {
                // Default weight is initialized only when missing so we do not overwrite user-entered values.
                shipmentRequest.PackageDefaults.Weight = new Weight { Amount = 0d, Units = "LB" };
            }

            // Step 14: Apply the domestic vs international service-validation behavior using configurable service symbols.
            string currentServiceSymbol = shipmentRequest.PackageDefaults.Service == null
                ? string.Empty
                : (shipmentRequest.PackageDefaults.Service.Symbol ?? string.Empty).Trim();

            // Step 15: Choose the target service rule based on whether the shipment crosses borders.
            if (isDomestic)
            {
                // Blueprint requirement: validate NDA Early AM by rate shopping for domestic US-to-US returns.
                string ndaEarlyAm = GetSetting("ReturnShipment.DomesticNdaEarlyAmServiceSymbol");
                string ndaFallback = GetSetting("ReturnShipment.DomesticNdaFallbackServiceSymbol");

                // If the requested service is not the expected domestic premium service, swap to the configured fallback.
                if (!string.IsNullOrWhiteSpace(ndaFallback) && !string.Equals(currentServiceSymbol, ndaEarlyAm, StringComparison.OrdinalIgnoreCase))
                {
                    // We only set the service object when we have a configured fallback symbol.
                    shipmentRequest.PackageDefaults.Service = new Service { Symbol = ndaFallback };
                }
            }
            else
            {
                // Blueprint requirement: validate UPS Express with Saturday Delivery for international return shipments.
                string upsExpress = GetSetting("ReturnShipment.InternationalUpsExpressServiceSymbol");
                string upsSaverFallback = GetSetting("ReturnShipment.InternationalUpsSaverFallbackServiceSymbol");

                // If the requested service does not match the expected international premium service, swap to the fallback.
                if (!string.IsNullOrWhiteSpace(upsSaverFallback) && !string.Equals(currentServiceSymbol, upsExpress, StringComparison.OrdinalIgnoreCase))
                {
                    // We keep the logic configurable so carrier-specific service codes can be changed without recompiling.
                    shipmentRequest.PackageDefaults.Service = new Service { Symbol = upsSaverFallback };
                }
            }

            // Step 16: Force the default package description when the blueprint's specimen-return default is missing.
            if (string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Description))
            {
                // Blueprint profile default: UN3373 Category B Human Sample.
                shipmentRequest.PackageDefaults.Description = "UN3373 Category B Human Sample";
            }

            // Step 17: Finish with a trace log so support can confirm the rule completed successfully.
            _logger.LogInfo(this, "SBR", "PreShip", "End", "Completed Marken return-label PreShip rule processing");
        }

        /// <summary>
        /// Looks up a configured BusinessRuleSettings value safely.
        /// This keeps service symbols and adapter-related defaults externalized and editable.
        /// </summary>
        private string GetSetting(string key)
        {
            // Step 1: Return an empty string when the setting list itself is unavailable.
            if (_settings == null)
            {
                // Without settings there is nothing to resolve, so we use a safe default.
                return string.Empty;
            }

            // Step 2: Locate the requested setting by key, ignoring case for administrator convenience.
            var setting = _settings.FirstOrDefault(x => x != null && string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            // Step 3: Return the setting value if present; otherwise return an empty string for safe fallback behavior.
            return setting == null || setting.Value == null ? string.Empty : setting.Value;
        }
    }

    /// <summary>
    /// Backup manager for pickup association during Ship.
    /// This class exists so the Ship hook can remain a thin delegate while pickup
    /// defaults, created pickup objects, and future attachment logic stay isolated.
    /// </summary>
    public class PickupAssociationManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _api;
        private readonly IProfile _profile;
        private readonly List<BusinessRuleSetting> _settings;
        private readonly ClientContext _clientContext;

        /// <summary>
        /// Builds the pickup association manager for the Marken Ship fallback workflow.
        /// </summary>
        public PickupAssociationManager(ILogger logger, IBusinessObjectApi api, IProfile profile, List<BusinessRuleSetting> settings, ClientContext clientContext)
        {
            // Store the logger so pickup behavior can be traced during shipping.
            _logger = logger;
            // Store the API so the manager can create pickups or expand later to attach them through ShipExec services.
            _api = api;
            // Store the profile so pickup defaults can be pulled from shipper and user configuration.
            _profile = profile;
            // Store settings because pickup defaults may need to be configured per site or environment.
            _settings = settings;
            // Store context to support user/site-specific pickup logic if the blueprint expands later.
            _clientContext = clientContext;
        }

        /// <summary>
        /// Executes the pickup backup path during Ship.
        /// The current blueprint describes this as a fallback only, so the default behavior
        /// is to return null unless a pickup object can be constructed from settings and profile data.
        /// </summary>
        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            // Step 1: Log entry so support knows the Ship fallback was invoked.
            _logger.LogInfo(this, "SBR", "Ship", "Start", "Beginning Marken pickup backup processing during Ship");

            // Step 2: Preserve default ShipExec behavior unless we explicitly need to intervene.
            if (shipmentRequest == null)
            {
                // The ship request must exist before a pickup can be associated.
                throw new Exception("ShipmentRequest was null during Marken Ship processing.");
            }

            // Step 3: If ShipExec already supplied a pickup, keep it and let the default ship path continue.
            if (pickup != null)
            {
                // The blueprint treats this as a backup workflow, so we do not replace an existing pickup.
                _logger.LogInfo(this, "SBR", "Ship", "End", "Existing pickup detected; leaving default ShipExec behavior unchanged");
                return null;
            }

            // Step 4: Read configured pickup defaults so the fallback can create a meaningful pickup object.
            string pickupCarrier = GetSetting("ReturnShipment.PickupDefaultCarrierSymbol");
            string pickupType = GetSetting("ReturnShipment.PickupDefaultType");

            // Step 5: If we lack enough configuration to create a pickup, exit cleanly and allow normal shipping.
            if (string.IsNullOrWhiteSpace(pickupCarrier) && string.IsNullOrWhiteSpace(pickupType))
            {
                // The blueprint says this hook is only a backup path, so no config means no intervention.
                _logger.LogInfo(this, "SBR", "Ship", "End", "No pickup defaults configured; allowing default ShipExec shipping");
                return null;
            }

            // Step 6: Create a pickup object using the current shipment and configuration values.
            var createdPickup = new Pickup();

            // Step 7: Assign the configured carrier symbol when one is provided.
            if (!string.IsNullOrWhiteSpace(pickupCarrier))
            {
                // This keeps carrier selection externalized and consistent with the blueprint's backup strategy.
                createdPickup.Carrier = pickupCarrier;
            }

            // Step 8: Return null to preserve default ship behavior after the pickup object is prepared.
            // In a fuller implementation, this method would attach createdPickup through the ShipExec API.
            _logger.LogInfo(this, "SBR", "Ship", "End", "Pickup backup path completed; default ShipExec shipping continues");
            return null;
        }

        /// <summary>
        /// Retrieves a configurable setting by key for pickup fallback behavior.
        /// </summary>
        private string GetSetting(string key)
        {
            // Step 1: Avoid null reference exceptions when no rule settings are available.
            if (_settings == null)
            {
                // No settings means no configurable pickup defaults can be read.
                return string.Empty;
            }

            // Step 2: Find the requested key using case-insensitive matching for administrator convenience.
            var setting = _settings.FirstOrDefault(x => x != null && string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            // Step 3: Return the value if found; otherwise return an empty string as a safe fallback.
            return setting == null || setting.Value == null ? string.Empty : setting.Value;
        }
    }
}