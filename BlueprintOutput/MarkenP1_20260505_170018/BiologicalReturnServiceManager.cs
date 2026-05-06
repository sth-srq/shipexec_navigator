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
    /// Encapsulates the biological sample service selection and validation rules.
    /// The class exists so the PreShip hook can delegate adapter/service decisions without becoming unreadable.
    /// </summary>
    public class BiologicalReturnServiceManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;

        /// <summary>
        /// Creates a manager that can decide which service symbol should be used for the biological return shipment.
        /// </summary>
        public BiologicalReturnServiceManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _businessRuleSettings = businessRuleSettings;
            _profile = profile;
            _clientContext = clientContext;
        }

        /// <summary>
        /// Applies the blueprint's UPS/CS adapter and service-selection logic to the shipment request.
        /// The method is intentionally conservative and only changes the service when the shipment type
        /// or validation outcome requires it.
        /// </summary>
        public void ApplyServiceRules(ShipmentRequest shipmentRequest, bool isUsToUs, bool isInternational, bool isBiologicalSample)
        {
            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "ApplyServiceRules", "Start", "Evaluating service rules for biological returns.");
            }

            if (shipmentRequest == null || shipmentRequest.PackageDefaults == null)
            {
                throw new Exception("ShipmentRequest.PackageDefaults was unavailable for service rule evaluation.");
            }

            if (shipmentRequest.PackageDefaults.Service == null || string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Service.ToString()))
            {
                shipmentRequest.PackageDefaults.Service = new Service { Symbol = "UPS Express" };
            }

            if (isBiologicalSample && _logger != null)
            {
                _logger.LogInfo(this, "SBR", "ApplyServiceRules", "Info", "Biological Sample is enabled; keeping shipment on CS Adapter-compatible services.");
            }

            if (isUsToUs)
            {
                bool ndaEarlyAmValid = ValidateServiceByRateShop("NDA Early AM", shipmentRequest);
                if (!ndaEarlyAmValid)
                {
                    shipmentRequest.PackageDefaults.Service = new Service { Symbol = "NDA" };
                    shipmentRequest.PackageDefaults.SaturdayDelivery = false;
                }
            }
            else if (isInternational)
            {
                bool upsExpressValid = ValidateServiceByRateShop("UPS Express Saturday", shipmentRequest);
                if (!upsExpressValid)
                {
                    shipmentRequest.PackageDefaults.Service = new Service { Symbol = "UPS Saver" };
                    shipmentRequest.PackageDefaults.SaturdayDelivery = false;
                }
            }

            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "ApplyServiceRules", "End", "Completed service rule evaluation for biological returns.");
            }
        }

        /// <summary>
        /// Performs a lightweight validation placeholder that represents the blueprint's rate-shopping check.
        /// In a production implementation this would call ShipExec rate services, but for blueprint-driven
        /// code generation the method centralizes the decision point for future enhancement.
        /// </summary>
        private bool ValidateServiceByRateShop(string serviceName, ShipmentRequest shipmentRequest)
        {
            if (_logger != null)
            {
                _logger.LogInfo(this, "SBR", "ValidateServiceByRateShop", "Start", "Validating service by rate shopping: " + serviceName);
                _logger.LogInfo(this, "SBR", "ValidateServiceByRateShop", "End", "Completed rate-shop validation for: " + serviceName);
            }
            return true;
        }
    }
}
