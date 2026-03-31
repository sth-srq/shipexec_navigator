using PSI.Sox;
using System.Collections.Generic;

namespace ShipExecNavigator.ClientSpecificLogic
{
    public interface IClientSpecificLogic
    {
        /// <summary>
        /// Attempts to find a matching shipper in <paramref name="existing"/> for
        /// the <paramref name="incoming"/> shipper using client-specific matching rules.
        /// Returns <c>null</c> if no match is found.
        /// </summary>
        Shipper? FindMatchingShipper(List<Shipper> existing, Shipper incoming);
    }
}
