using System;
using System.Collections.Generic;
using System.Linq;
using PSI.Sox.Api;
using PSI.Sox.Client;
using PSI.Sox.Interfaces;
using PSI.Sox.Models;

namespace PSI.Sox
{
    public class PickupAssociationManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;
        private readonly Tools _tools;

        public PickupAssociationManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _businessRuleSettings = businessRuleSettings;
            _profile = profile;
            _clientContext = clientContext;
            _tools = new Tools(logger);
        }

        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            try
            {
                if (!_tools.GetBooleanValueFromBusinessRuleSettings("PickupFallbackEnabled", _businessRuleSettings))
                    return null;

                if (pickup != null)
                    return null;

                Pickup fallbackPickup = BuildPickupFromUserData(userParams);
                if (fallbackPickup == null)
                    return null;

                if (shipmentRequest != null)
                    shipmentRequest.Pickup = fallbackPickup;

                _logger?.Info("Marken pickup fallback was attached to the shipment request.");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Marken pickup fallback failed: {ex}");
                throw;
            }
        }

        private Pickup BuildPickupFromUserData(SerializableDictionary userParams)
        {
            string address1Key = _tools.GetStringValueFromBusinessRuleSettings("PickupFallbackPickupAddressCustom1Key", _businessRuleSettings);
            string contactKey = _tools.GetStringValueFromBusinessRuleSettings("PickupFallbackPickupContactKey", _businessRuleSettings);

            if (string.IsNullOrWhiteSpace(address1Key) && string.IsNullOrWhiteSpace(contactKey))
                return null;

            string address1 = GetUserParam(userParams, address1Key);
            string address2 = GetUserParam(userParams, _tools.GetStringValueFromBusinessRuleSettings("PickupFallbackPickupAddressCustom2Key", _businessRuleSettings));
            string address3 = GetUserParam(userParams, _tools.GetStringValueFromBusinessRuleSettings("PickupFallbackPickupAddressCustom3Key", _businessRuleSettings));
            string contact = GetUserParam(userParams, contactKey);
            string phone = GetUserParam(userParams, _tools.GetStringValueFromBusinessRuleSettings("PickupFallbackPickupPhoneKey", _businessRuleSettings));

            if (string.IsNullOrWhiteSpace(address1) && string.IsNullOrWhiteSpace(contact))
                return null;

            var pickup = new Pickup();
            pickup.Name = contact;
            pickup.Phone = phone;
            pickup.Address = new NameAddress
            {
                Address1 = address1,
                Address2 = address2,
                Address3 = address3
            };

            return pickup;
        }

        private string GetUserParam(SerializableDictionary userParams, string key)
        {
            if (userParams == null || string.IsNullOrWhiteSpace(key) || !userParams.ContainsKey(key) || userParams[key] == null)
                return string.Empty;

            return userParams[key].ToString();
        }
    }
}
