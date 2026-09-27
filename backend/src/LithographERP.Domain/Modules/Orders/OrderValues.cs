namespace LithographERP.Domain.Modules.Orders;

public static class OrderStatuses
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
}

public static class OrderPriorities
{
    public const string Low = "low";
    public const string Normal = "normal";
    public const string High = "high";
    public const string Urgent = "urgent";

    public static readonly string[] All = [Low, Normal, High, Urgent];

    public static bool TryParse(string? value, out string priority)
    {
        priority = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!All.Contains(priority, StringComparer.Ordinal))
        {
            priority = string.Empty;
            return false;
        }

        return true;
    }
}

public static class OrderBusinessIds
{
    public const string Prefix = "ORD";
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

public static class ChecklistProgress
{
    public static (int Completed, int Total) Calculate(IEnumerable<bool> completed)
    {
        var flags = completed as IReadOnlyCollection<bool> ?? completed.ToArray();
        var done = 0;
        foreach (var flag in flags)
        {
            if (flag)
            {
                done++;
            }
        }

        return (done, flags.Count);
    }
}
