using LithographERP.Domain.Modules.Clients;

namespace LithographERP.UnitTests;

public class ClientBusinessIdTests
{
    [Theory]
    [InlineData(1, "CL-000001")]
    [InlineData(42, "CL-000042")]
    [InlineData(1250, "CL-001250")]
    public void Format_UsesTheClientPrefixAndSixDigits(long sequence, string expected) =>
        Assert.Equal(expected, ClientBusinessIds.Format(sequence));
}
