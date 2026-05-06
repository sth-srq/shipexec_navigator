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
