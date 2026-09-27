namespace LithographERP.Domain.Modules.Projects;

public static class ProjectStatuses
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string OnHold = "on_hold";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] All = [Draft, Active, OnHold, Completed, Cancelled];

    public static bool TryParse(string? value, out string status)
    {
        status = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!All.Contains(status, StringComparer.Ordinal))
        {
            status = string.Empty;
            return false;
        }

        return true;
    }

    public static bool IsOpen(string status) => status is Draft or Active or OnHold;
}

public static class ProjectRoles
{
    public const string Owner = "owner";
    public const string Assignee = "assignee";
    public const string Participant = "participant";
    public const string Observer = "observer";

    public static readonly string[] All = [Owner, Assignee, Participant, Observer];
}

public static class ProjectBusinessIds
{
    public const string Prefix = "PRJ";
    public const int SequenceDigits = 6;

    public static string Format(int year, long sequence)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        return $"{Prefix}-{year}-{sequence.ToString().PadLeft(SequenceDigits, '0')}";
    }
}

public static class ProjectDates
{
    public static bool IsValidRange(DateOnly? start, DateOnly? deadline) =>
        start is null || deadline is null || deadline.Value >= start.Value;
}
