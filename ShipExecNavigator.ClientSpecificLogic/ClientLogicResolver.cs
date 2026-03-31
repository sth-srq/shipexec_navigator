using System;

namespace ShipExecNavigator.ClientSpecificLogic
{
    public static class ClientLogicResolver
    {
        public static IClientSpecificLogic Resolve(string? companyName)
        {
            if (companyName is not null)
            {
                if (companyName.IndexOf("wesbanco", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new WesbancoClientSpecificLogic();
            }

            return new DefaultCompanyLogic();
        }
    }
}
