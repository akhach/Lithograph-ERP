using LithographERP.Domain.Modules.Projects;

namespace LithographERP.UnitTests;

public class ProjectBusinessIdTests
{
    [Theory]
    [InlineData(2026, 1, "PRJ-2026-000001")]
    [InlineData(2026, 425, "PRJ-2026-000425")]
    [InlineData(2027, 1, "PRJ-2027-000001")]
    public void Format_UsesYearAndSixDigits(int year, long sequence, string expected) =>
        Assert.Equal(expected, ProjectBusinessIds.Format(year, sequence));

    [Theory]
    [InlineData("draft", true)]
    [InlineData("on_hold", true)]
    [InlineData("finished", false)]
    public void Status_ParsesCanonicalValues(string value, bool expected) =>
        Assert.Equal(expected, ProjectStatuses.TryParse(value, out _));

    [Fact]
    public void Dates_RejectDeadlineBeforeStart()
    {
        var start = new DateOnly(2026, 5, 2);
        Assert.False(ProjectDates.IsValidRange(start, new DateOnly(2026, 5, 1)));
        Assert.True(ProjectDates.IsValidRange(start, start));
        Assert.True(ProjectDates.IsValidRange(null, start));
    }
}
