using Microsoft.Extensions.Logging;
using PSI.Sox;
using ShipExecNavigator.ClientSpecificLogic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShipExecNavigator.ClientSpecificLogic
{
    public class WesbancoClientSpecificLogic : IClientSpecificLogic
    {
        private readonly ILogger<WesbancoClientSpecificLogic> _logger =
            LoggerProvider.CreateLogger<WesbancoClientSpecificLogic>();

        public Shipper? FindMatchingShipper(List<Shipper> existing, Shipper incoming)
        {
            _logger.LogTrace(">> FindMatchingShipper | Incoming={Name}", incoming.Name);
            if (string.IsNullOrWhiteSpace(incoming.Name)) { _logger.LogTrace("<< FindMatchingShipper → null (no name)"); return null; }

            var m = Regex.Match(incoming.Name, @"\(([^)]+)\)");
            if (!m.Success) { _logger.LogTrace("<< FindMatchingShipper → null (no key in parens)"); return null; }

            var key = m.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(key)) { _logger.LogTrace("<< FindMatchingShipper → null (empty key)"); return null; }

            var result = existing.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.Name) &&
                e.Name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
            _logger.LogTrace("<< FindMatchingShipper → {Result}", result?.Name ?? "null");
            return result;
        }

        public IReadOnlyList<string> GetShipperExportExtraHeaders()
        {
            _logger.LogTrace(">> GetShipperExportExtraHeaders → [BankId]");
            return ["BankId"];
        }

        public IReadOnlyList<string> GetShipperExportExtraValues(Shipper shipper)
        {
            _logger.LogTrace(">> GetShipperExportExtraValues | Shipper={Name}", shipper.Name);
            if (!string.IsNullOrEmpty(shipper.Name))
            {
                var m = Regex.Match(shipper.Name, @"\(([^)]+)\)");
                if (m.Success)
                {
                    var val = m.Groups[1].Value.Trim();
                    _logger.LogTrace("<< GetShipperExportExtraValues → {Val}", val);
                    return [val];
                }
            }
            _logger.LogTrace("<< GetShipperExportExtraValues → (empty)");
            return [string.Empty];
        }
    }
}
