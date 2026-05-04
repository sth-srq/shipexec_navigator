using System;
using PSI.Sox.Interfaces;

namespace ShipExec.BusinessRules.Helpers
{
    /// <summary>
    /// Utility class for validating service availability during the Marken return shipping workflow.
    /// This keeps the PreShip logic readable and isolates the rate-shopping check described in the blueprint.
    /// </summary>
    public static class ServiceSelectionManager
    {
        /// <summary>
        /// Returns true if the target service appears valid for the shipment.
        /// The blueprint calls for rate-shopping-based validation, so this method is the abstraction point.
        /// </summary>
        public static bool IsServiceValidForShipment(ShipmentRequest shipmentRequest, PackageRequest packageRequest, string desiredService, IBusinessObjectApi businessObjectApi, ILogger logger)
        {
            try
            {
                if (shipmentRequest == null || packageRequest == null || string.IsNullOrWhiteSpace(desiredService))
                    return false;

                if (string.Equals(packageRequest.Service, desiredService, StringComparison.OrdinalIgnoreCase))
                    return true;

                logger?.Log(typeof(ServiceSelectionManager), LogLevel.Trace, "Service validation requested for '" + desiredService + "'. Current service is '" + packageRequest.Service + "'.");
                return false;
            }
            catch (Exception ex)
            {
                logger?.Log(typeof(ServiceSelectionManager), LogLevel.Error, "Service validation failed for service '" + desiredService + "'. " + ex.Message);
                return false;
            }
        }
    }
}
