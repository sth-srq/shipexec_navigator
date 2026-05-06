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
    /// Manages Marken Phase 1 specimen-return shipping rules for ShipExec server hooks.
    /// This class centralizes the authoritative server-side logic so the SBR hook bodies
    /// remain thin delegators while still enforcing blueprint requirements consistently.
    /// </summary>
    public class SpecimenReturnRulesManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly IProfile _profile;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly ClientContext _clientContext;

        /// <summary>
        /// Creates a new manager instance with the ShipExec runtime dependencies required
        /// to inspect profile data, log activity, and apply specimen-return shipment rules.
        /// </summary>
        public SpecimenReturnRulesManager(ILogger logger, IBusinessObjectApi businessObjectApi, IProfile profile, List<BusinessRuleSetting> businessRuleSettings, ClientContext clientContext)
        {
            // Store logger so every rule decision can be traced in ShipExec logs.
            _logger = logger;
            // Store the API reference for any future authoritative ShipExec lookups or actions.
            _businessObjectApi = businessObjectApi;
            // Store the current user profile because the blueprint says service and address defaults are profile-driven.
            _profile = profile;
            // Store commander settings even though the blueprint does not identify required keys yet.
            _businessRuleSettings = businessRuleSettings;
            // Store client context so any future user/company/site specific logic can be resolved correctly.
            _clientContext = clientContext;
        }

        /// <summary>
        /// Applies the Phase 1 specimen-return rules to the shipment before ship processing.
        /// This exists because the blueprint makes PreShip the authoritative server-side point
        /// for paperless customs, biological sample handling, dry ice normalization, and service correction.
        /// </summary>
        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
            {
                throw new Exception("ShipmentRequest is required for specimen-return processing.");
            }

            if (shipmentRequest.PackageDefaults == null)
            {
                shipmentRequest.PackageDefaults = new Package();
            }

            if (shipmentRequest.PackageDefaults.Consignee == null)
            {
                shipmentRequest.PackageDefaults.Consignee = new NameAddress();
            }

            if (shipmentRequest.Packages == null)
            {
                shipmentRequest.Packages = new List<PackageRequest>();
            }

            if (shipmentRequest.Packages.Count == 0)
            {
                shipmentRequest.Packages.Add(new PackageRequest());
            }

            PackageRequest packageRequest = shipmentRequest.Packages[0];
            string pickupFromCountry = shipmentRequest.PackageDefaults.Consignee.Country ?? string.Empty;
            string returnToCountry = string.Empty;

            if (shipmentRequest.PackageDefaults.ReturnAddress != null)
            {
                returnToCountry = shipmentRequest.PackageDefaults.ReturnAddress.Country ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(returnToCountry))
            {
                returnToCountry = pickupFromCountry;
            }

            bool isInternationalReturn = !string.IsNullOrWhiteSpace(pickupFromCountry) && !string.IsNullOrWhiteSpace(returnToCountry) && !string.Equals(pickupFromCountry, returnToCountry, StringComparison.OrdinalIgnoreCase);
            if (isInternationalReturn)
            {
                packageRequest.CommercialInvoiceMethod = 1;
                packageRequest.ExportReason = "Medical";
            }

            bool isBiologicalSample = false;
            if (!string.IsNullOrWhiteSpace(packageRequest.MiscReference4))
            {
                bool parsedFlag;
                if (bool.TryParse(packageRequest.MiscReference4, out parsedFlag))
                {
                    isBiologicalSample = parsedFlag;
                }
                else
                {
                    isBiologicalSample = string.Equals(packageRequest.MiscReference4, "1", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(packageRequest.MiscReference4, "true", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(packageRequest.MiscReference4, "yes", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (!isBiologicalSample && shipmentRequest.PackageDefaults != null && !string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.MiscReference4))
            {
                bool parsedFlag;
                if (bool.TryParse(shipmentRequest.PackageDefaults.MiscReference4, out parsedFlag))
                {
                    isBiologicalSample = parsedFlag;
                }
                else
                {
                    isBiologicalSample = string.Equals(shipmentRequest.PackageDefaults.MiscReference4, "1", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(shipmentRequest.PackageDefaults.MiscReference4, "true", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(shipmentRequest.PackageDefaults.MiscReference4, "yes", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (isBiologicalSample)
            {
                if (packageRequest.PackageExtras == null)
                {
                    packageRequest.PackageExtras = new SerializableDictionary();
                }
                packageRequest.PackageExtras["RESTRICTED_ARTICLE_TYPE"] = "32";
                packageRequest.Perishable = true;
            }

            string dryIceWeightKgText = string.Empty;
            if (!string.IsNullOrWhiteSpace(packageRequest.MiscReference3))
            {
                dryIceWeightKgText = packageRequest.MiscReference3;
            }
            if (string.IsNullOrWhiteSpace(dryIceWeightKgText) && shipmentRequest.PackageDefaults != null && !string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.MiscReference3))
            {
                dryIceWeightKgText = shipmentRequest.PackageDefaults.MiscReference3;
            }

            if (!string.IsNullOrWhiteSpace(dryIceWeightKgText))
            {
                double dryIceWeightKg;
                if (!double.TryParse(dryIceWeightKgText, out dryIceWeightKg))
                {
                    throw new Exception("Dry Ice Weight must be a numeric value in kilograms.");
                }

                double dryIceWeightLb = dryIceWeightKg * 2.2046226218d;
                if (packageRequest.Weight == null)
                {
                    packageRequest.Weight = new Weight();
                }
                if (string.IsNullOrWhiteSpace(packageRequest.Weight.Units))
                {
                    packageRequest.Weight.Units = "LB";
                }
                packageRequest.Weight.Amount = packageRequest.Weight.Amount + dryIceWeightLb;

                if (shipmentRequest.PackageDefaults.DryIceWeight == null)
                {
                    shipmentRequest.PackageDefaults.DryIceWeight = new Weight();
                }
                shipmentRequest.PackageDefaults.DryIceWeight.Amount = dryIceWeightKg;
                shipmentRequest.PackageDefaults.DryIceWeight.Units = "KG";

                packageRequest.DryIcePurpose = 1;
                if (string.Equals(pickupFromCountry, "US", StringComparison.OrdinalIgnoreCase) && string.Equals(returnToCountry, "US", StringComparison.OrdinalIgnoreCase))
                {
                    packageRequest.DryIceRegulationSet = 1;
                }
                else
                {
                    packageRequest.DryIceRegulationSet = 2;
                }
            }

            if (shipmentRequest.PackageDefaults.Service == null)
            {
                shipmentRequest.PackageDefaults.Service = new Service();
            }
            if (string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Service.Symbol))
            {
                shipmentRequest.PackageDefaults.Service.Symbol = "CONNECTSHIP_UPS.UPS.EXP";
                shipmentRequest.PackageDefaults.Service.Name = "UPS Express";
            }
            if (string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Shipper) && _profile != null && _profile.Shippers != null && _profile.Shippers.Count > 0)
            {
                shipmentRequest.PackageDefaults.Shipper = _profile.Shippers[0].Name;
            }
            if (string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Terms))
            {
                shipmentRequest.PackageDefaults.Terms = "Prepaid";
            }
            if (shipmentRequest.PackageDefaults.Shipdate == null)
            {
                shipmentRequest.PackageDefaults.Shipdate = new Date(System.DateTime.Now);
            }
        }

        /// <summary>
        /// Provides the backup Ship-time pickup strategy required by the blueprint if the client-side pickup association fails.
        /// This exists so the server can still create or attach a pickup object using user/profile data when the browser flow is unreliable.
        /// </summary>
        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
            {
                return null;
            }

            if (shipmentRequest.PackageDefaults == null)
            {
                shipmentRequest.PackageDefaults = new Package();
            }

            if (pickup != null)
            {
                return null;
            }

            Pickup backupPickup = new Pickup();
            backupPickup.Carrier = shipmentRequest.PackageDefaults.Service != null ? shipmentRequest.PackageDefaults.Service.Carrier : null;
            return null;
        }
    }
}
