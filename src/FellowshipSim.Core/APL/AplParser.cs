using System.Globalization;
using FellowshipSim.Core.APL.Expressions;

namespace FellowshipSim.Core.APL;

public class AplParser
{
    public static List<AplAction> ParseScript(string script)
    {
        var actions = new List<AplAction>();
        using var reader = new StringReader(script);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var action = ParseActionLine(trimmed);
            if (action != null)
            {
                actions.Add(action);
            }
        }

        return actions;
    }

    public static AplAction? ParseActionLine(string line)
    {
        string trimmed = line.Trim();

        // Skip non-action declarations
        if (trimmed.Contains("run_action_list") || trimmed.Contains("snapshot_stats"))
            return null;

        // Find the action specification after = or += or /
        string actionPart = trimmed;
        int eqIndex = trimmed.IndexOf('=');
        if (eqIndex >= 0)
        {
            actionPart = trimmed.Substring(eqIndex + 1).Trim();
        }

        if (actionPart.StartsWith('/'))
        {
            actionPart = actionPart.Substring(1).Trim();
        }

        if (string.IsNullOrEmpty(actionPart))
            return null;

        // Split by comma to extract ability name and arguments (like if=...)
        string[] parts = actionPart.Split(',');
        string abilityName = parts[0].Trim();

        IAplExpression? condition = null;
        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (part.StartsWith("if=", StringComparison.OrdinalIgnoreCase))
            {
                string exprText = part.Substring(3).Trim();
                condition = ParseExpression(exprText);
            }
        }

        return new AplAction
        {
            AbilityName = abilityName,
            Condition = condition,
            RawLine = line
        };
    }

    public static IAplExpression ParseExpression(string text)
    {
        var tokens = Tokenize(text);
        var parser = new ExpressionParser(tokens);
        return parser.Parse();
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        int i = 0;
        int len = text.Length;

        while (i < len)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c is '(' or ')' or '&' or '|')
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            if (c is '!' or '<' or '>' or '=')
            {
                if (i + 1 < len && text[i + 1] == '=')
                {
                    tokens.Add(text.Substring(i, 2));
                    i += 2;
                    continue;
                }
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            if (char.IsLetterOrDigit(c) || c is '_' or '.' or '-')
            {
                int start = i;
                while (i < len && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '.' or '-'))
                {
                    i++;
                }
                tokens.Add(text.Substring(start, i - start));
                continue;
            }

            // Fallback for unhandled characters
            tokens.Add(c.ToString());
            i++;
        }

        return tokens;
    }

    private class ExpressionParser
    {
        private readonly List<string> _tokens;
        private int _pos;

        public ExpressionParser(List<string> tokens) => _tokens = tokens;

        private string? Peek => _pos < _tokens.Count ? _tokens[_pos] : null;

        private string Consume() => _pos < _tokens.Count ? _tokens[_pos++] : string.Empty;

        public IAplExpression Parse()
        {
            var expr = ParseOr();
            return expr;
        }

        // Logical OR: '|'
        private IAplExpression ParseOr()
        {
            var left = ParseAnd();

            while (Peek == "|")
            {
                Consume();
                var right = ParseAnd();
                left = new BinaryOpExpression(left, "|", right);
            }

            return left;
        }

        // Logical AND: '&'
        private IAplExpression ParseAnd()
        {
            var left = ParseComparison();

            while (Peek == "&")
            {
                Consume();
                var right = ParseComparison();
                left = new BinaryOpExpression(left, "&", right);
            }

            return left;
        }

        // Comparison: '=', '==', '!=', '<', '<=', '>', '>='
        private IAplExpression ParseComparison()
        {
            var left = ParsePrimary();

            if (Peek is "=" or "==" or "!=" or "<" or "<=" or ">" or ">=")
            {
                string op = Consume();
                var right = ParsePrimary();
                return new BinaryOpExpression(left, op, right);
            }

            return left;
        }

        // Primary: '!' Primary, '(' Expression ')', Number, Property
        private IAplExpression ParsePrimary()
        {
            if (Peek == "!")
            {
                Consume();
                var inner = ParsePrimary();
                return new UnaryOpExpression("!", inner);
            }

            if (Peek == "(")
            {
                Consume(); // '('
                var inner = ParseOr();
                if (Peek == ")")
                {
                    Consume(); // ')'
                }
                return inner;
            }

            string token = Consume();

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double numVal))
            {
                return new ConstantExpression(numVal);
            }

            return new PropertyExpression(token);
        }
    }
}

