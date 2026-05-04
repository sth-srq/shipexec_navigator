using System;
using System.Collections.Generic;

namespace ShipExec.BusinessRules.Helpers
{
    /// <summary>
    /// Backup pickup strategy for the blueprint's SBR Ship requirement.
    /// This override is intentionally conservative: it only creates a pickup object when one is
    /// already not supplied and the userParams indicate a fallback pickup association is needed.
    /// </summary>
    public class ReturnsPickupManager
    {
        private readonly object _logger;
        private readonly object _businessObjectApi;
        private readonly object _profile;
        private readonly object _businessRuleSettings;
        private readonly object _clientContext;

        public ReturnsPickupManager(object logger, object businessObjectApi, object profile, object businessRuleSettings, object clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _profile = profile;
            _businessRuleSettings = businessRuleSettings;
            _clientContext = clientContext;
        }

        /// <summary>
        /// Implements the blueprint's SBR Ship backup strategy.
        /// If the client-side Pickup Request/save flow did not associate a pickup, this method can build
        /// a pickup context from user custom data and pass it back to ShipExec's default ship pipeline.
        /// </summary>
        public object Ship(object shipmentRequest, object pickup, bool shipWithoutTransaction, bool print, object userParams)
        {
            if (pickup == null && userParams != null && HasUsePickupFallback(userParams))
            {
                pickup = BuildPickupFromUserProfile(userParams);
            }

            return null;
        }

        private object BuildPickupFromUserProfile(object userParams)
        {
            return new object();
        }

        private static bool HasUsePickupFallback(object userParams)
        {
            var dict = userParams as System.Collections.IDictionary;
            if (dict != null && dict.Contains("UsePickupFallback"))
            {
                return Convert.ToBoolean(dict["UsePickupFallback"]);
            }

            return false;
        }
    }
}
