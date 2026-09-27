namespace LithographERP.Application.Modules.Calculator;

public static class CalculatorMath
{
    private const decimal Ln2 = 0.6931471805599453094172321215m;

    public static decimal Add(decimal left, decimal right) => Checked(() => left + right);

    public static decimal Subtract(decimal left, decimal right) => Checked(() => left - right);

    public static decimal Multiply(decimal left, decimal right) => Checked(() => left * right);

    public static decimal Divide(decimal left, decimal right)
    {
        if (right == 0)
        {
            throw DivisionByZero();
        }

        return Checked(() => left / right);
    }

    public static decimal Remainder(decimal left, decimal right)
    {
        if (right == 0)
        {
            throw DivisionByZero();
        }

        return Checked(() => left % right);
    }

    public static decimal Abs(decimal value) => Checked(() => value < 0 ? -value : value);

    public static decimal Round(decimal value, int digits) =>
        Checked(() => Scale(value, digits, scaled => decimal.Round(scaled, 0, MidpointRounding.AwayFromZero)));

    public static decimal RoundUp(decimal value, int digits) =>
        Checked(() => Scale(value, digits, RoundAwayFromZero));

    public static decimal RoundDown(decimal value, int digits) =>
        Checked(() => Scale(value, digits, decimal.Truncate));

    public static decimal Ceiling(decimal value, decimal significance)
    {
        if (significance <= 0)
        {
            throw Invalid("CEILING significance must be greater than zero.");
        }

        return Checked(() => ToMultiple(value, significance, decimal.Ceiling));
    }

    public static decimal Floor(decimal value, decimal significance)
    {
        if (significance <= 0)
        {
            throw Invalid("FLOOR significance must be greater than zero.");
        }

        return Checked(() => ToMultiple(value, significance, decimal.Floor));
    }

    public static decimal Sqrt(decimal value)
    {
        if (value < 0)
        {
            throw Invalid("SQRT requires a non-negative number.");
        }

        return Root(value, 2);
    }

    public static decimal Pow(decimal baseValue, decimal exponent)
    {
        if (IsWhole(exponent))
        {
            if (exponent is < int.MinValue or > int.MaxValue)
            {
                throw Overflow();
            }

            return IntegerPow(baseValue, (int)exponent);
        }

        if (baseValue < 0)
        {
            throw Invalid("POWER of a negative number requires an integer exponent.");
        }

        if (baseValue == 0)
        {
            if (exponent < 0)
            {
                throw DivisionByZero();
            }

            return 0;
        }

        if (exponent < 0)
        {
            return Divide(1m, Pow(baseValue, -exponent));
        }

        if (TryRational(exponent, out var numerator, out var denominator))
        {
            return Snap(Root(IntegerPow(baseValue, numerator), denominator));
        }

        return Snap(Exp(Multiply(exponent, Ln(baseValue))));
    }

    public static decimal IntegerPow(decimal value, int exponent)
    {
        if (exponent == 0)
        {
            if (value == 0)
            {
                throw Invalid("POWER(0, 0) is undefined.");
            }

            return 1m;
        }

        try
        {
            if (exponent < 0)
            {
                if (value == 0)
                {
                    throw DivisionByZero();
                }

                return 1m / IntegerPow(value, checked(-exponent));
            }

            decimal result = 1m;
            decimal factor = value;
            var remaining = exponent;
            while (remaining > 0)
            {
                if ((remaining & 1) == 1)
                {
                    result *= factor;
                }

                remaining >>= 1;
                if (remaining > 0)
                {
                    factor *= factor;
                }
            }

            return result;
        }
        catch (OverflowException)
        {
            throw Overflow();
        }
        catch (FormulaEvaluationException)
        {
            throw;
        }
    }

    public static decimal Root(decimal value, int degree)
    {
        if (degree <= 0)
        {
            throw Invalid("Root degree must be positive.");
        }

        if (degree == 1)
        {
            return value;
        }

        if (value < 0)
        {
            if (degree % 2 == 0)
            {
                throw Invalid("A negative number cannot use an even root.");
            }

            return -Root(-value, degree);
        }

        if (value == 0)
        {
            return 0;
        }

        decimal guess = value > 1 ? value / degree : 1m;
        if (guess == 0)
        {
            guess = 1m;
        }

        for (var iteration = 0; iteration < 80; iteration++)
        {
            decimal powered;
            try
            {
                powered = IntegerPow(guess, degree - 1);
            }
            catch (FormulaEvaluationException)
            {
                guess /= 2;
                continue;
            }

            if (powered == 0)
            {
                guess = 1m;
                continue;
            }

            decimal next;
            try
            {
                next = (((degree - 1) * guess) + (value / powered)) / degree;
            }
            catch (OverflowException)
            {
                guess /= 2;
                continue;
            }

            if (next == guess)
            {
                return Snap(guess);
            }

            guess = next;
        }

        return Snap(guess);
    }

    public static decimal Ln(decimal value)
    {
        if (value <= 0)
        {
            throw Invalid("Logarithm requires a positive number.");
        }

        var scale = 0;
        var reduced = value;
        while (reduced >= 2m)
        {
            reduced /= 2m;
            scale++;
        }

        while (reduced < 1m)
        {
            reduced *= 2m;
            scale--;
        }

        var y = reduced - 1m;
        decimal term = y;
        decimal sum = 0m;
        for (var n = 1; n <= 200; n++)
        {
            var next = sum + (term / n);
            if (next == sum)
            {
                break;
            }

            sum = next;
            term *= -y;
        }

        return sum + (scale * Ln2);
    }

    public static decimal Exp(decimal value)
    {
        var scale = (int)decimal.Round(value / Ln2, 0, MidpointRounding.ToEven);
        var reduced = value - (scale * Ln2);
        decimal term = 1m;
        decimal sum = 1m;
        for (var n = 1; n <= 80; n++)
        {
            term *= reduced / n;
            var next = sum + term;
            if (next == sum)
            {
                break;
            }

            sum = next;
        }

        if (scale == 0)
        {
            return sum;
        }

        return Multiply(sum, IntegerPow(2m, scale));
    }

    private static decimal Scale(decimal value, int digits, Func<decimal, decimal> roundInteger)
    {
        if (digits is < -28 or > 28)
        {
            throw Invalid("Rounding digits must be between -28 and 28.");
        }

        if (digits == 0)
        {
            return roundInteger(value);
        }

        if (digits > 0)
        {
            var factor = Pow10(digits);
            return roundInteger(value * factor) / factor;
        }

        var divisor = Pow10(-digits);
        return roundInteger(value / divisor) * divisor;
    }

    private static decimal ToMultiple(decimal value, decimal significance, Func<decimal, decimal> round)
    {
        if (significance == 1m)
        {
            return round(value);
        }

        var quotient = value / significance;
        var nearest = decimal.Round(quotient, 0, MidpointRounding.AwayFromZero);
        if (Abs(quotient - nearest) <= 0.0000000000001m)
        {
            quotient = nearest;
        }

        return round(quotient) * significance;
    }

    private static decimal RoundAwayFromZero(decimal value)
    {
        var truncated = decimal.Truncate(value);
        if (truncated == value)
        {
            return value;
        }

        return truncated + Math.Sign(value);
    }

    private static decimal Pow10(int digits)
    {
        decimal result = 1m;
        for (var i = 0; i < digits; i++)
        {
            result *= 10m;
        }

        return result;
    }

    private static bool TryRational(decimal exponent, out int numerator, out int denominator)
    {
        for (var candidate = 2; candidate <= 12; candidate++)
        {
            var scaled = exponent * candidate;
            if (IsWhole(scaled) && scaled is >= int.MinValue and <= int.MaxValue)
            {
                numerator = (int)scaled;
                denominator = candidate;
                return true;
            }
        }

        numerator = 0;
        denominator = 0;
        return false;
    }

    private static bool IsWhole(decimal value) => value == decimal.Truncate(value);

    private static decimal Snap(decimal value)
    {
        var rounded = decimal.Round(value, 12, MidpointRounding.AwayFromZero);
        return Abs(value - rounded) <= 0.000000000001m ? rounded : value;
    }

    private static decimal Checked(Func<decimal> calculation)
    {
        try
        {
            return calculation();
        }
        catch (OverflowException)
        {
            throw Overflow();
        }
    }

    private static FormulaEvaluationException DivisionByZero() =>
        new(FormulaErrorCodes.DivisionByZero, "Division by zero.");

    private static FormulaEvaluationException Invalid(string message) =>
        new(FormulaErrorCodes.InvalidArguments, message);

    private static FormulaEvaluationException Overflow() =>
        new(FormulaErrorCodes.NumericOverflow, "The calculation is too large.");
}
