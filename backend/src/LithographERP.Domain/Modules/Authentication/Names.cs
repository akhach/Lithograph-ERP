namespace LithographERP.Domain.Modules.Authentication;

public static class Names
{
    public const int MaxLength = 100;

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
