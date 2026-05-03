using ShipExecAgent.BusinessLogic;
using ShipExecAgent.Model;

namespace ShipExecAgent.Tests.BusinessLogic;

public class JWTManagerTests
{
    private readonly JWTManager _sut = new();

    // ── ConvertToObject ────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToObject_ValidJson_ReturnsPopulatedJWT()
    {
        var json = """{"access_token":"tok123","refresh_token":"ref456","token_type":"Bearer","expires_in":3600}""";

        var result = _sut.ConvertToObject(json);

        Assert.NotNull(result);
        Assert.Equal("tok123", result.access_token);
        Assert.Equal("ref456", result.refresh_token);
        Assert.Equal("Bearer", result.token_type);
        Assert.Equal(3600, result.expires_in);
    }

    [Fact]
    public void ConvertToObject_MinimalJson_DoesNotThrow()
    {
        var json = """{"access_token":"abc","refresh_token":"","token_type":"","expires_in":0}""";

        var result = _sut.ConvertToObject(json);

        Assert.Equal("abc", result.access_token);
    }

    // ── GetAccessToken ─────────────────────────────────────────────────────────

    [Fact]
    public void GetAccessToken_ValidJson_ReturnsAccessToken()
    {
        var json = """{"access_token":"mytoken","refresh_token":"","token_type":"Bearer","expires_in":1800}""";

        var token = _sut.GetAccessToken(json);

        Assert.Equal("mytoken", token);
    }

    [Fact]
    public void GetAccessToken_SameJsonAsConvertToObject_AccessTokenMatches()
    {
        var json = """{"access_token":"abc123","refresh_token":"r","token_type":"t","expires_in":100}""";

        var direct = _sut.ConvertToObject(json).access_token;
        var via    = _sut.GetAccessToken(json);

        Assert.Equal(direct, via);
    }
}
