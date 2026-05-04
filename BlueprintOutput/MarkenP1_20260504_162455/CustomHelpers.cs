using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PSI.Sox
{
    public class BiologicalReturnsManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly IProfile _profile;
        private readonly List<BusinessRuleSetting> _settings;
        private readonly ClientContext _clientContext;
        private readonly Tools _tools;

        public BiologicalReturnsManager(ILogger logger, IBusinessObjectApi businessObjectApi, IProfile profile, List<BusinessRuleSetting> settings, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _profile = profile;
            _settings = settings;
            _clientContext = clientContext;
            _tools = new Tools(logger);
        }

        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
                throw new Exception("Shipment request is required.");

            if (shipmentRequest.PackageDefaults == null)
                shipmentRequest.PackageDefaults = new Package();

            if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
                throw new Exception("At least one package is required.");

            var pkg = shipmentRequest.Packages[0];
            if (pkg == null)
                throw new Exception("Package is required.");

            bool isInternational = IsInternational(shipmentRequest);
            bool isUsToUs = IsUsToUs(shipmentRequest);
            bool isBiologicalSample = ToBool(GetValue(shipmentRequest, "MiscReference4"));
            string temperature = (GetValue(shipmentRequest, "ConsigneeReference") ?? string.Empty).Trim();
            decimal dryIceKg = ToDecimal(GetValue(shipmentRequest, "MiscReference3"));

            if (isInternational)
            {
                SetCommercialInvoiceMethod(pkg, 1);
                SetExportReason(pkg, "Medical");
            }

            if (isBiologicalSample)
            {
                SetPackageExtra(pkg, "RESTRICTED_ARTICLE_TYPE", "32");
            }

            if (dryIceKg > 0)
            {
                decimal dryIceLbs = ConvertKgToLbs(dryIceKg);
                SetDryIce(pkg, dryIceLbs, isUsToUs);
                AddWeight(pkg, dryIceLbs);
            }

            if (isUsToUs)
            {
                ValidateAndSetService(shipmentRequest, "NDA Early AM", "NDA without Saturday Delivery");
            }
            else
            {
                ValidateAndSetService(shipmentRequest, "UPS Express with Saturday Delivery", "UPS Saver without Saturday Delivery");
            }

            if (!string.IsNullOrWhiteSpace(temperature) && temperature.Equals("Frozen", StringComparison.OrdinalIgnoreCase) && dryIceKg <= 0)
            {
                _logger?.Warning("Frozen shipment detected with no dry ice weight provided.");
            }
        }

        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            try
            {
                if (pickup == null)
                {
                    pickup = BuildPickupFromUserProfile();
                }

                if (shipmentRequest != null && shipmentRequest.PackageDefaults != null)
                {
                    shipmentRequest.PackageDefaults.Pickup = pickup;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex);
                throw;
            }
        }

        private bool IsInternational(ShipmentRequest shipmentRequest)
        {
            var from = GetCountry(shipmentRequest, true);
            var to = GetCountry(shipmentRequest, false);
            return !string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to) && !from.Equals(to, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsUsToUs(ShipmentRequest shipmentRequest)
        {
            var from = GetCountry(shipmentRequest, true);
            var to = GetCountry(shipmentRequest, false);
            return string.Equals(from, "US", StringComparison.OrdinalIgnoreCase) && string.Equals(to, "US", StringComparison.OrdinalIgnoreCase);
        }

        private string GetCountry(ShipmentRequest shipmentRequest, bool pickupFrom)
        {
            var addr = pickupFrom ? shipmentRequest?.PackageDefaults?.Consignee : shipmentRequest?.PackageDefaults?.Shipper;
            return addr?.Country;
        }

        private string GetValue(ShipmentRequest shipmentRequest, string field)
        {
            if (shipmentRequest == null)
                return null;

            var pd = shipmentRequest.PackageDefaults;
            if (pd == null)
                return null;

            switch (field)
            {
                case "ConsigneeReference": return pd.ConsigneeReference;
                case "MiscReference1": return pd.MiscReference1;
                case "MiscReference2": return pd.MiscReference2;
                case "MiscReference3": return pd.MiscReference3;
                case "MiscReference4": return pd.MiscReference4;
                default: return null;
            }
        }

        private bool ToBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (bool.TryParse(value, out var b)) return b;
            return value == "1" || value.Equals("y", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private decimal ToDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
            return d;
        }

        private decimal ConvertKgToLbs(decimal kg)
        {
            return Math.Round(kg * 2.2046226218m, 2, MidpointRounding.AwayFromZero);
        }

        private void SetCommercialInvoiceMethod(PackageRequest pkg, int method)
        {
            if (pkg == null) return;
            pkg.CommercialInvoiceMethod = method;
        }

        private void SetExportReason(PackageRequest pkg, string reason)
        {
            if (pkg == null) return;
            pkg.ExportReason = reason;
        }

        private void SetPackageExtra(PackageRequest pkg, string key, string value)
        {
            if (pkg == null) return;
            if (pkg.PackageExtras == null)
                pkg.PackageExtras = new SerializableDictionary();
            pkg.PackageExtras[key] = value;
        }

        private void SetDryIce(PackageRequest pkg, decimal weightLbs, bool isUsToUs)
        {
            if (pkg == null) return;
            pkg.DryIceWeight = weightLbs;
            pkg.DryIcePurpose = "Medical";
            pkg.DryIceRegulationSet = isUsToUs ? "International Air Transportation Association regulations." : "US 49 CFR regulations.";
        }

        private void AddWeight(PackageRequest pkg, decimal lbs)
        {
            if (pkg == null) return;
            if (pkg.Weight == null) pkg.Weight = new Weight();
            pkg.Weight.Amount = (pkg.Weight.Amount) + lbs;
        }

        private void ValidateAndSetService(ShipmentRequest shipmentRequest, string preferredService, string fallbackService)
        {
            if (shipmentRequest?.PackageDefaults == null)
                return;

            shipmentRequest.PackageDefaults.Service = preferredService;
            var services = new List<Service>();
            var sortType = SortType.NoOrder;
            var rates = _businessObjectApi?.Rate(shipmentRequest, services, sortType, null);
            bool valid = rates != null && rates.Count > 0;
            if (!valid)
                shipmentRequest.PackageDefaults.Service = fallbackService;
        }

        private Pickup BuildPickupFromUserProfile()
        {
            var pickup = new Pickup();
            var userInfo = _profile?.UserInformation;
            pickup.Consignee = new NameAddress
            {
                Company = userInfo?.Company,
                Contact = userInfo?.Contact,
                Address1 = userInfo?.Address1,
                Address2 = userInfo?.Address2,
                City = userInfo?.City,
                StateProvince = userInfo?.StateProvince,
                PostalCode = userInfo?.PostalCode,
                Country = userInfo?.Country,
                Phone = userInfo?.Phone
            };
            return pickup;
        }
    }
}
