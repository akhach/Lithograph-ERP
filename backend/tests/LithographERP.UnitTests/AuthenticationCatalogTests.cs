using LithographERP.Domain.Modules.Authentication;

namespace LithographERP.UnitTests;

public class AuthenticationCatalogTests
{
    [Fact]
    public void PermissionCatalog_HasUniqueModuleActionCodes()
    {
        var codes = PermissionCatalog.All.Select(permission => permission.Code).ToArray();
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(40, codes.Length);
        Assert.All(PermissionCatalog.All, permission =>
        {
            Assert.Equal(permission.Code, permission.Code.ToLowerInvariant());
            Assert.StartsWith(permission.Module.ToLowerInvariant() + ".", permission.Code);
        });
        Assert.Contains(PermissionCatalog.Users.View, codes);
        Assert.Contains(PermissionCatalog.Orders.ViewCostPrice, codes);
    }

    [Theory]
    [InlineData(" director ", "DIRECTOR")]
    [InlineData("Aram", "ARAM")]
    public void Names_Normalize_TrimsAndUppercases(string value, string expected) =>
        Assert.Equal(expected, Names.Normalize(value));

    [Theory]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    [InlineData("short", false)]
    [InlineData("long-enough", true)]
    public void PasswordRules_RejectEmptyAndShortPasswords(string? password, bool acceptable) =>
        Assert.Equal(acceptable, PasswordRules.IsAcceptable(password));
}
