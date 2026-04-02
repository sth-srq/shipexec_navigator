using Microsoft.Extensions.Logging;
using ShipExecNavigator.BusinessLogic.Logging;
using ShipExecNavigator.Model;

namespace ShipExecNavigator.BusinessLogic
{
    public class JWTManager
    {
        private readonly ILogger<JWTManager> _logger = LoggerProvider.CreateLogger<JWTManager>();

        public JWT ConvertToObject(string rawJWT)
        {
            _logger.LogTrace(">> ConvertToObject | InputLength={Len}", rawJWT?.Length ?? 0);
            var result = JsonHelper.Deserialize<JWT>(rawJWT);
            _logger.LogTrace("<< ConvertToObject → JWT obtained");
            return result;
        }

        public string GetAccessToken(string rawJWT)
        {
            _logger.LogTrace(">> GetAccessToken | InputLength={Len}", rawJWT?.Length ?? 0);
            var token = ConvertToObject(rawJWT).access_token;
            _logger.LogTrace("<< GetAccessToken → [token length {Len}]", token?.Length ?? 0);
            return token;
        }
    }
}
