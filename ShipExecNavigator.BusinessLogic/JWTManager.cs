using ShipExecNavigator.Model;

namespace ShipExecNavigator.BusinessLogic
{
    public class JWTManager
    {
        public JWT ConvertToObject(string rawJWT)
            => JsonHelper.Deserialize<JWT>(rawJWT);

        public string GetAccessToken(string rawJWT)
            => ConvertToObject(rawJWT).access_token;
    }
}
