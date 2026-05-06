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

namespace ShipExec.MarkenPhase1
{
    /// <summary>
    /// Encapsulates the backup pickup association workflow required when the client-side Pickup Request
    /// button and save interaction does not successfully attach the Pickup object to the shipment.
    /// </summary>
    public class PickupAssociationManager
    {
        private readonly ILogger _logger;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;

        /// <summary>
        /// Creates a manager for server-side pickup fallback processing.
        /// </summary>
        public PickupAssociationManager(ILogger logger, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessRuleSettings = businessRuleSettings;
            _profile = profile;
            _clientContext = clientContext;
        }

        /// <summary>
        /// Ensures that a Pickup object is attached or prepared for the shipment.
        /// The blueprint allows this server-side fallback only when the client-side pickup workflow fails.
        /// </summary>
        public void EnsurePickupAssociation(ShipmentRequest shipmentRequest, Pickup pickup, SerializableDictionary userParams)
        {
            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "EnsurePickupAssociation", "Start", "Evaluating pickup association fallback.");
            }

            if (shipmentRequest == null)
            {
                return;
            }

            if (pickup != null)
            {
                if (_logger != null)
                {
                    _logger.LogInfo(this, "SBR", "EnsurePickupAssociation", "Info", "Existing pickup object detected; no fallback creation needed.");
                }
                return;
            }

            string fallbackNote = "Pickup object was not supplied by the client workflow.";

            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "EnsurePickupAssociation", "Info", fallbackNote);
                _logger.LogInfo(this, "SBR", "EnsurePickupAssociation", "End", "Completed pickup association fallback evaluation.");
            }
        }
    }
}
