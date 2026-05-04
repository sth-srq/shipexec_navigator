using System;
using System.Collections.Generic;
using PSI.Sox.Interfaces;

namespace ShipExec.BusinessRules.Helpers
{
    /// <summary>
    /// Handles the server-side pickup fallback strategy required by the blueprint.
    /// This class exists only as a safety net when the CBR pickup-request/save workflow is unreliable.
    /// </summary>
    public class PickupAssociationManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;

        public PickupAssociationManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _businessRuleSettings = businessRuleSettings;
            _profile = profile;
            _clientContext = clientContext;
        }

        /// <summary>
        /// Backup SBR Ship implementation.
        /// The blueprint says this should only be used if client-side pickup creation does not reliably associate the pickup.
        /// </summary>
        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            if (pickup != null)
                return null;

            var inferredPickup = BuildPickupFromUserProfile(userParams);
            if (inferredPickup == null)
                return null;

            if (shipmentRequest != null)
                shipmentRequest.Pickup = inferredPickup;

            return null;
        }

        private Pickup BuildPickupFromUserProfile(SerializableDictionary userParams)
        {
            var pickup = new Pickup();

            try
            {
                if (userParams != null)
                {
                    SetStringIfPresent(userParams, pickup, "Custom1", "Reference1");
                    SetStringIfPresent(userParams, pickup, "Custom2", "Reference2");
                    SetStringIfPresent(userParams, pickup, "Custom3", "Reference3");
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(this, LogLevel.Error, "Unable to build pickup fallback from user parameters. " + ex.Message);
                return null;
            }

            return pickup;
        }

        private static void SetStringIfPresent(SerializableDictionary userParams, Pickup pickup, string userKey, string pickupPropertyName)
        {
            if (userParams == null || pickup == null)
                return;

            if (!userParams.ContainsKey(userKey))
                return;

            var value = userParams[userKey] == null ? null : userParams[userKey].ToString();
            if (string.IsNullOrWhiteSpace(value))
                return;

            var prop = pickup.GetType().GetProperty(pickupPropertyName);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
                prop.SetValue(pickup, value, null);
        }
    }
}
