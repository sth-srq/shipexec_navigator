using System;
using Microsoft.Extensions.Logging;
using ShipExecAgent.ClientSpecificLogic.Logging;

namespace ShipExecAgent.ClientSpecificLogic
{
    /// <summary>
    /// Factory that resolves the correct <see cref="IClientSpecificLogic"/> implementation
    /// for a given company name.
    /// <para>
    /// Company names are matched case-insensitively using substring checks so that minor
    /// naming variations (e.g. "WesbancoTest" vs "WesBanco Prod") are handled correctly.
    /// </para>
    /// <para>
    /// <b>Adding a new client:</b>
    /// <list type="number">
    ///   <item>Create a class that implements <see cref="IClientSpecificLogic"/>.</item>
    ///   <item>Add a new <c>if</c> branch to <see cref="Resolve"/> with the relevant
    ///         company-name substring.</item>
    /// </list>
    /// When no specific match is found, <see cref="DefaultCompanyLogic"/> is returned,
    /// which provides standard non-customised behaviour.
    /// </para>
    /// </summary>
    public static class ClientLogicResolver
    {
        private static ILogger Logger => LoggerProvider.CreateLogger<DefaultCompanyLogic>();

        /// <summary>
        /// Returns the <see cref="IClientSpecificLogic"/> implementation that applies to
        /// <paramref name="companyName"/>.
        /// </summary>
        /// <param name="companyName">
        /// Display name of the company as returned by the Management Studio API.
        /// May be <see langword="null"/> — in that case <see cref="DefaultCompanyLogic"/> is returned.
        /// </param>
        /// <returns>
        /// A client-specific logic instance, or <see cref="DefaultCompanyLogic"/> if no
        /// specific override exists for the supplied company name.
        /// </returns>
        public static IClientSpecificLogic Resolve(string? companyName)
        {
            Logger.LogTrace(">> Resolve({CompanyName})", companyName);
            IClientSpecificLogic result;
            if (companyName is not null)
            {
                if (companyName.IndexOf("wesbanco", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Logger.LogTrace("<< Resolve → WesbancoClientSpecificLogic");
                    return new WesbancoClientSpecificLogic();
                }
            }
            result = new DefaultCompanyLogic();
            Logger.LogTrace("<< Resolve → DefaultCompanyLogic");
            return result;
        }
    }
}
