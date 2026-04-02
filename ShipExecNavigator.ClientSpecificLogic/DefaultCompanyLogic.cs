using Microsoft.Extensions.Logging;
using PSI.Sox;
using ShipExecNavigator.ClientSpecificLogic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShipExecNavigator.ClientSpecificLogic
{
    public class DefaultCompanyLogic : IClientSpecificLogic
    {
        private readonly ILogger<DefaultCompanyLogic> _logger = LoggerProvider.CreateLogger<DefaultCompanyLogic>();

        public Shipper? FindMatchingShipper(List<Shipper> existing, Shipper incoming)
        {
            _logger.LogTrace(">> FindMatchingShipper | Incoming={Symbol} ExistingCount={Count}",
                incoming.Symbol, existing.Count);
            Shipper? result = null;
            if (incoming.Id != 0)
            {
                result = existing.FirstOrDefault(e => e.Id == incoming.Id);
                if (result is not null) { _logger.LogTrace("<< FindMatchingShipper → matched by Id"); return result; }
            }

            if (!string.IsNullOrEmpty(incoming.Symbol))
                result = existing.FirstOrDefault(e =>
                    string.Equals(e.Symbol, incoming.Symbol, StringComparison.OrdinalIgnoreCase));

            _logger.LogTrace("<< FindMatchingShipper → {Result}",
                result is not null ? result.Symbol : "null");
            return result;
        }

        public IReadOnlyList<string> GetShipperExportExtraHeaders()
        {
            _logger.LogTrace(">> GetShipperExportExtraHeaders");
            return [];
        }

        public IReadOnlyList<string> GetShipperExportExtraValues(Shipper shipper)
        {
            _logger.LogTrace(">> GetShipperExportExtraValues | Shipper={Symbol}", shipper.Symbol);
            return [];
        }
    }
}
