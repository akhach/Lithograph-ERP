namespace LithographERP.Domain.Modules.Authentication;

public static class PasswordRules
{
    public const int MinimumLength = 8;

    public static bool IsAcceptable(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Trim().Length >= MinimumLength;
}
