using PSI.Sox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShipExecNavigator.ClientSpecificLogic
{
    public class WesbancoClientSpecificLogic : IClientSpecificLogic
    {
        /// <summary>
        /// Matches an incoming shipper to an existing one by extracting the identifier
        /// in parentheses from the incoming name — e.g. "(429739)" — and finding an
        /// existing shipper whose Name contains that same text.
        /// Example: incoming "North ILLINOIS Street 140 (429739)" matches
        ///          existing "10201 N ILLINOIS ST 140  (429739)".
        /// </summary>
        public Shipper? FindMatchingShipper(List<Shipper> existing, Shipper incoming)
        {
            if (string.IsNullOrWhiteSpace(incoming.Name)) return null;

            var m = Regex.Match(incoming.Name, @"\(([^)]+)\)");
            if (!m.Success) return null;

            var key = m.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(key)) return null;

            return existing.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.Name) &&
                e.Name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Adds a <c>BankId</c> column to the shipper export containing the identifier
        /// extracted from the parenthesised portion of each shipper's name
        /// (e.g. "10201 N ILLINOIS ST 140  (429739)" → "429739").
        /// </summary>
        public IReadOnlyList<string> GetShipperExportExtraHeaders() => ["BankId"];

        public IReadOnlyList<string> GetShipperExportExtraValues(Shipper shipper)
        {
            if (!string.IsNullOrEmpty(shipper.Name))
            {
                var m = Regex.Match(shipper.Name, @"\(([^)]+)\)");
                if (m.Success)
                    return [m.Groups[1].Value.Trim()];
            }

            return [string.Empty];
        }
    }
}
