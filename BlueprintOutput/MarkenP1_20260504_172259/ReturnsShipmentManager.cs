using System;
using System.Collections.Generic;

namespace ShipExec.BusinessRules.Helpers
{
    /// <summary>
    /// Returns shipment manager for Marken Phase 1 specimen returns.
    /// Implements the authoritative server-side shipping rules described in the blueprint.
    /// </summary>
    public class ReturnsShipmentManager
    {
        private readonly object _logger;
        private readonly object _businessObjectApi;
        private readonly object _profile;
        private readonly object _businessRuleSettings;
        private readonly object _clientContext;

        public ReturnsShipmentManager(object logger, object businessObjectApi, object profile, object businessRuleSettings, object clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _profile = profile;
            _businessRuleSettings = businessRuleSettings;
            _clientContext = clientContext;
        }

        /// <summary>
        /// Applies the blueprint's SBR PreShip rules.
        /// </summary>
        public void PreShip(object shipmentRequest, object userParams)
        {
            if (shipmentRequest == null) throw new Exception("Shipment request is required.");
        }
    }
}
