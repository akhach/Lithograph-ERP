using System.Text.RegularExpressions;

namespace LithographERP.Domain.Modules.Calculator;

public static partial class FieldKeyRules
{
    // Boolean literals are reserved so a formula can contain true/false without colliding with a field.
    public static readonly IReadOnlySet<string> ReservedKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "true",
        "false",
    };

    public static bool TryValidate(string key, out string message)
    {
        if (key.Length is < 1 or > CalculatorLimits.MaxFieldKeyLength || !KeyPattern().IsMatch(key))
        {
            message = "Field key must use lowercase snake_case.";
            return false;
        }

        if (ReservedKeys.Contains(key))
        {
            message = $"Field key '{key}' is reserved.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
