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
    /// Central manager for the Marken Phase 1 biological returns server-side business logic.
    /// This class exists so SoxBusinessRules remains a thin delegation layer while all carrier
    /// and shipment mutation rules stay in one maintainable place.
    /// </summary>
    public class ReturnShipmentManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;

        /// <summary>
        /// Initializes the manager with the ShipExec runtime dependencies required to inspect,
        /// validate, and adjust shipment data for the biological returns workflow.
        /// </summary>
        /// <param name="logger">ShipExec logger for traceability.</param>
        /// <param name="businessObjectApi">ShipExec business API for history or carrier-related operations.</param>
        /// <param name="businessRuleSettings">Commander-configured business rule settings.</param>
        /// <param name="profile">Current user profile containing shipper/service context.</param>
        /// <param name="clientContext">Calling client context for tenant-aware behavior.</param>
        public ReturnShipmentManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _businessRuleSettings = businessRuleSettings;
            _profile = profile;
            _clientContext = clientContext;
        }

        /// <summary>
        /// Applies the server-side biological returns validation and mutation rules before shipping.
        /// This method runs during SBR PreShip, after the client has already prepared the shipment.
        /// It fulfills the blueprint requirement for international invoice settings, biological sample
        /// handling, service validation, and dry ice conversion/injection.
        /// </summary>
        /// <param name="shipmentRequest">The shipment request to validate and mutate.</param>
        /// <param name="userParams">Caller-provided parameters passed through ShipExec.</param>
        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "PreShip", "Start", "Beginning biological returns pre-ship processing.");
            }

            if (shipmentRequest == null)
            {
                throw new Exception("ShipmentRequest was null during PreShip processing.");
            }

            if (shipmentRequest.PackageDefaults == null)
            {
                shipmentRequest.PackageDefaults = new Package();
            }

            if (shipmentRequest.PackageDefaults.Consignee == null)
            {
                shipmentRequest.PackageDefaults.Consignee = new NameAddress();
            }

            if (shipmentRequest.PackageDefaults.ReturnAddress == null)
            {
                shipmentRequest.PackageDefaults.ReturnAddress = new NameAddress();
            }

            if (shipmentRequest.PackageDefaults.Service == null)
            {
                shipmentRequest.PackageDefaults.Service = new Service();
            }

            string consigneeCountry = shipmentRequest.PackageDefaults.Consignee.Country == null ? string.Empty : shipmentRequest.PackageDefaults.Consignee.Country.ToString().Trim().ToUpperInvariant();
            string shipperCountry = shipmentRequest.PackageDefaults.ReturnAddress.Country == null ? string.Empty : shipmentRequest.PackageDefaults.ReturnAddress.Country.ToString().Trim().ToUpperInvariant();

            bool isUsToUs = consigneeCountry == "US" && shipperCountry == "US";
            bool isInternational = !string.IsNullOrWhiteSpace(consigneeCountry) && !string.IsNullOrWhiteSpace(shipperCountry) && consigneeCountry != shipperCountry;

            bool isBiologicalSample = false;
            if (shipmentRequest.PackageDefaults.UserData4 != null)
            {
                bool parsedFlag;
                if (bool.TryParse(shipmentRequest.PackageDefaults.UserData4.ToString(), out parsedFlag))
                {
                    isBiologicalSample = parsedFlag;
                }
                else
                {
                    isBiologicalSample = string.Equals(shipmentRequest.PackageDefaults.UserData4.ToString().Trim(), "true", StringComparison.OrdinalIgnoreCase) || shipmentRequest.PackageDefaults.UserData4.ToString().Trim() == "1";
                }
            }

            if (isInternational)
            {
                shipmentRequest.PackageDefaults.CommercialInvoiceMethod = 1;
                shipmentRequest.PackageDefaults.ExportReason = "Medical";
            }

            if (shipmentRequest.PackageDefaults.PackageExtras == null)
            {
                shipmentRequest.PackageDefaults.PackageExtras = new List<PackageRequest>();
            }

            if (isBiologicalSample)
            {
                var marker = new PackageRequest();
                marker.UserData1 = "RESTRICTED_ARTICLE_TYPE";
                marker.UserData2 = "32";
                shipmentRequest.PackageDefaults.PackageExtras.Add(marker);
            }

            if (shipmentRequest.PackageDefaults.UserData3 != null && !string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.UserData3.ToString()))
            {
                decimal dryIceWeightKg;
                if (!decimal.TryParse(shipmentRequest.PackageDefaults.UserData3.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out dryIceWeightKg))
                {
                    throw new Exception("Dry Ice Weight (kg) must be a numeric value.");
                }

                double dryIceWeightLb = (double)decimal.Round(dryIceWeightKg * 2.2046226218m, 2);
                if (shipmentRequest.PackageDefaults.Weight == null)
                {
                    shipmentRequest.PackageDefaults.Weight = new Weight();
                }

                shipmentRequest.PackageDefaults.Weight.Amount = dryIceWeightLb;
                shipmentRequest.PackageDefaults.DryIceWeight = dryIceWeightLb;
                shipmentRequest.PackageDefaults.DryIcePurpose = 1;

                if (isUsToUs)
                {
                    shipmentRequest.PackageDefaults.DryIceRegulationSet = 1;
                }
                else
                {
                    shipmentRequest.PackageDefaults.DryIceRegulationSet = 2;
                }
            }

            BiologicalReturnServiceManager serviceManager = new BiologicalReturnServiceManager(_logger, _businessObjectApi, _businessRuleSettings, _profile, _clientContext);
            serviceManager.ApplyServiceRules(shipmentRequest, isUsToUs, isInternational, isBiologicalSample);

            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "PreShip", "End", "Completed biological returns pre-ship processing.");
            }
        }

        /// <summary>
        /// Provides the SBR Ship override fallback used when the client-side pickup association does not work.
        /// This method runs during the ship lifecycle and may create or attach a Pickup object using return
        /// workflow data so the normal ShipExec carrier transaction can proceed.
        /// </summary>
        /// <param name="shipmentRequest">The shipment request being processed.</param>
        /// <param name="pickup">The pickup object supplied by ShipExec, if any.</param>
        /// <param name="shipWithoutTransaction">Whether ShipExec should skip the carrier transaction.</param>
        /// <param name="print">Whether labels should be printed.</param>
        /// <param name="userParams">Caller-provided parameters passed through ShipExec.</param>
        /// <returns>Null to continue the standard ShipExec shipment flow after fallback processing.</returns>
        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "Ship", "Start", "Beginning biological returns ship fallback processing.");
            }

            PickupAssociationManager pickupManager = new PickupAssociationManager(_logger, _businessRuleSettings, _profile, _clientContext);
            pickupManager.EnsurePickupAssociation(shipmentRequest, pickup, userParams);

            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "Ship", "End", "Completed biological returns ship fallback processing.");
            }

            return null;
        }
    }
}
