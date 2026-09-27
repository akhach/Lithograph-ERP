using System.Globalization;

namespace LithographERP.Application.Modules.Calculator;

public static class FormulaTokenizer
{
    public static IReadOnlyList<FormulaToken> Tokenize(string formula)
    {
        var tokens = new List<FormulaToken>();
        var index = 0;
        while (index < formula.Length)
        {
            var current = formula[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (char.IsDigit(current))
            {
                tokens.Add(ReadNumber(formula, ref index));
                continue;
            }

            if (char.IsLetter(current) || current == '_')
            {
                var start = index;
                index++;
                while (index < formula.Length && (char.IsLetterOrDigit(formula[index]) || formula[index] == '_'))
                {
                    index++;
                }

                tokens.Add(new FormulaToken(FormulaTokenKind.Identifier, formula[start..index], start));
                continue;
            }

            if (TryReadOperator(formula, ref index, out var token))
            {
                tokens.Add(token);
                continue;
            }

            throw new FormulaParseException(
                CalculatorIssueCodes.InvalidFormulaSyntax,
                $"Unexpected character '{current}'.",
                index);
        }

        tokens.Add(new FormulaToken(FormulaTokenKind.End, string.Empty, formula.Length));
        return tokens;
    }

    private static FormulaToken ReadNumber(string formula, ref int index)
    {
        var start = index;
        index++;
        while (index < formula.Length && char.IsDigit(formula[index]))
        {
            index++;
        }

        if (index < formula.Length && formula[index] == '.')
        {
            var dot = index;
            index++;
            if (index >= formula.Length || !char.IsDigit(formula[index]))
            {
                throw new FormulaParseException(CalculatorIssueCodes.InvalidFormulaSyntax, "Invalid number.", dot);
            }

            while (index < formula.Length && char.IsDigit(formula[index]))
            {
                index++;
            }
        }

        var text = formula[start..index];
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            throw new FormulaParseException(CalculatorIssueCodes.InvalidFormulaSyntax, "Invalid number.", start);
        }

        return new FormulaToken(FormulaTokenKind.Number, text, start, number);
    }

    private static bool TryReadOperator(string formula, ref int index, out FormulaToken token)
    {
        var start = index;
        if (index + 1 < formula.Length)
        {
            var pair = formula.Substring(index, 2);
            var kind = pair switch
            {
                "!=" => FormulaTokenKind.NotEqual,
                ">=" => FormulaTokenKind.GreaterOrEqual,
                "<=" => FormulaTokenKind.LessOrEqual,
                _ => (FormulaTokenKind?)null,
            };
            if (kind is not null)
            {
                index += 2;
                token = new FormulaToken(kind.Value, pair, start);
                return true;
            }
        }

        var single = formula[index] switch
        {
            '+' => FormulaTokenKind.Plus,
            '-' => FormulaTokenKind.Minus,
            '*' => FormulaTokenKind.Star,
            '/' => FormulaTokenKind.Slash,
            '%' => FormulaTokenKind.Percent,
            '=' => FormulaTokenKind.Equal,
            '>' => FormulaTokenKind.Greater,
            '<' => FormulaTokenKind.Less,
            '(' => FormulaTokenKind.LeftParen,
            ')' => FormulaTokenKind.RightParen,
            ',' => FormulaTokenKind.Comma,
            _ => (FormulaTokenKind?)null,
        };
        if (single is null)
        {
            token = default;
            return false;
        }

        index++;
        token = new FormulaToken(single.Value, formula[start].ToString(), start);
        return true;
    }
}
