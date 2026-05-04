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

        public string GetStringValueFromBusinessRuleSettings(string keyName, List<BusinessRuleSetting> businessRuleSettings)
        {
            if (businessRuleSettings == null || string.IsNullOrWhiteSpace(keyName))
                return string.Empty;

            BusinessRuleSetting setting = businessRuleSettings.FirstOrDefault(x => string.Equals(x.Key, keyName, StringComparison.OrdinalIgnoreCase));
            return setting == null ? string.Empty : (setting.Value ?? string.Empty);
        }
    }
}
