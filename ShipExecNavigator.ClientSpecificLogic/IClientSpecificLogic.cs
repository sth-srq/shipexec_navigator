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

        /// <summary>
        /// Returns additional CSV column headers to append after the standard shipper
        /// export columns. Return an empty list if no extra columns are needed.
        /// </summary>
        IReadOnlyList<string> GetShipperExportExtraHeaders();

        /// <summary>
        /// Returns the values for the extra columns defined by
        /// <see cref="GetShipperExportExtraHeaders"/> for the given shipper.
        /// The returned list must have the same length as the headers list.
        /// </summary>
        IReadOnlyList<string> GetShipperExportExtraValues(Shipper shipper);
    }
}
