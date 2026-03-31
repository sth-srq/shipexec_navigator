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
    }
}
