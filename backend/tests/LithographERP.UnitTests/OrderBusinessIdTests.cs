using LithographERP.Domain.Modules.Orders;

namespace LithographERP.UnitTests;

public class OrderBusinessIdTests
{
    [Theory]
    [InlineData(2026, 1, "ORD-2026-000001")]
    [InlineData(2026, 425, "ORD-2026-000425")]
    [InlineData(2027, 1, "ORD-2027-000001")]
    public void Format_UsesYearAndSixDigits(int year, long sequence, string expected) =>
        Assert.Equal(expected, OrderBusinessIds.Format(year, sequence));

    [Theory]
    [InlineData("draft", true)]
    [InlineData("Draft", true)]
    [InlineData("on_hold", true)]
    [InlineData("completed", true)]
    [InlineData("cancelled", true)]
    [InlineData("finished", false)]
    public void Status_ParsesCanonicalValues(string value, bool expected) =>
        Assert.Equal(expected, OrderStatuses.TryParse(value, out _));

    [Theory]
    [InlineData("low", true)]
    [InlineData("Normal", true)]
    [InlineData("high", true)]
    [InlineData("urgent", true)]
    [InlineData("critical", false)]
    public void Priority_ParsesCanonicalValues(string value, bool expected) =>
        Assert.Equal(expected, OrderPriorities.TryParse(value, out _));

    [Fact]
    public void ChecklistProgress_CountsCompletedItems()
    {
        var empty = ChecklistProgress.Calculate([]);
        Assert.Equal((0, 0), empty);
        var progress = ChecklistProgress.Calculate([true, false, true, true]);
        Assert.Equal((3, 4), progress);
    }
}
