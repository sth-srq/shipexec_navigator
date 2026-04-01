using PSI.Sox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShipExecNavigator.ClientSpecificLogic
{
    public class DefaultCompanyLogic : IClientSpecificLogic
    {
        public Shipper? FindMatchingShipper(List<Shipper> existing, Shipper incoming)
        {
            if (incoming.Id != 0)
            {
                var byId = existing.FirstOrDefault(e => e.Id == incoming.Id);
                if (byId is not null) return byId;
            }

            if (!string.IsNullOrEmpty(incoming.Symbol))
            {
                return existing.FirstOrDefault(e =>
                    string.Equals(e.Symbol, incoming.Symbol, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        public IReadOnlyList<string> GetShipperExportExtraHeaders() => [];

        public IReadOnlyList<string> GetShipperExportExtraValues(Shipper shipper) => [];
    }
}
