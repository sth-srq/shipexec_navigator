using System;
using System.Collections.Generic;
using System.Linq;
using PSI.Sox.Interfaces;

namespace PSI.Sox
{
    public class Tools
    {
        private readonly ILogger _logger;

        public Tools(ILogger logger)
        {
            _logger = logger;
        }

        public string GetStringValueFromBusinessRuleSettings(string key, List<BusinessRuleSetting> settings)
        {
            if (settings == null || string.IsNullOrWhiteSpace(key))
                return string.Empty;

            var setting = settings.FirstOrDefault(s => s != null && string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
            return setting != null ? (setting.Value ?? string.Empty) : string.Empty;
        }

        public bool GetBooleanValueFromBusinessRuleSettings(string key, List<BusinessRuleSetting> settings)
        {
            bool parsed;
            return bool.TryParse(GetStringValueFromBusinessRuleSettings(key, settings), out parsed) && parsed;
        }
    }
}
