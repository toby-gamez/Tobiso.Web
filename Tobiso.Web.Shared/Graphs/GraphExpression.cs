using System.Globalization;
using System.Text.RegularExpressions;

namespace Tobiso.Web.Shared.Graphs;

/// <summary>
/// Parses the right-hand side of a graph function line ("y = 2x^2 - 3sin(x) + 1") into a
/// delegate of x. Written for how people type maths in an article rather than code:
/// implicit multiplication (2x, 3(x+1), (x+1)(x-1), 2sin(x), 2|x|), "^" and ²/³ powers,
/// decimal commas, π, Czech function names (tg, cotg) and |x| for absolute value.
/// </summary>
public static class GraphExpression
{
    private static readonly Dictionary<string, Func<double, double>> Functions = new()
    {
        ["sqrt"] = Math.Sqrt, ["odm"] = Math.Sqrt,
        ["abs"] = Math.Abs,
        ["sin"] = Math.Sin, ["cos"] = Math.Cos,
        ["tan"] = Math.Tan, ["tg"] = Math.Tan,
        ["cot"] = v => 1 / Math.Tan(v), ["cotg"] = v => 1 / Math.Tan(v),
        ["asin"] = Math.Asin, ["arcsin"] = Math.Asin,
        ["acos"] = Math.Acos, ["arccos"] = Math.Acos,
        ["atan"] = Math.Atan, ["arctan"] = Math.Atan, ["arctg"] = Math.Atan,
        ["ln"] = Math.Log, ["log"] = Math.Log10,
        ["exp"] = Math.Exp,
    };

    private static readonly Dictionary<string, double> Constants = new()
    {
        ["pi"] = Math.PI, ["e"] = Math.E,
    };

    // Longest names first so "exp" wins over "e", "cotg" over "cot", "arcsin" over "asin"...
    private static readonly string[] Names =
        Functions.Keys.Concat(Constants.Keys).Append("x").OrderByDescending(n => n.Length).ToArray();

    /// <summary>Strips an optional "y =" / "f(x) =" prefix and parses the rest.</summary>
    public static bool TryParse(string text, out Func<double, double> function, out string error)
    {
        function = _ => double.NaN;
        error = "";

        var src = Regex.Replace(text, @"^\s*(?:y|f\s*\(\s*x\s*\))\s*=\s*", "", RegexOptions.IgnoreCase);
        src = src.Replace('−', '-').Replace('–', '-').Replace('·', '*').Replace('⋅', '*').Replace('×', '*')
                 .Replace('÷', '/').Replace("π", "pi").Replace("²", "^2").Replace("³", "^3");
        src = Regex.Replace(src, @"(\d),(\d)", "$1.$2").ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(src))
        {
            error = "chybí předpis funkce";
            return false;
        }

        try
        {
            var parser = new Parser(src);
            function = parser.ParseAll();
            return true;
        }
        catch (FormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private sealed class Parser(string src)
    {
        private int _pos;
        private int _absDepth;

        public Func<double, double> ParseAll()
        {
            var result = ParseSum();
            SkipSpaces();
            if (_pos < src.Length)
                throw new FormatException($"nečekaný znak „{src[_pos]}“");
            return result;
        }

        private Func<double, double> ParseSum()
        {
            var left = ParseProduct();
            while (true)
            {
                SkipSpaces();
                if (Eat('+')) { var l = left; var r = ParseProduct(); left = x => l(x) + r(x); }
                else if (Eat('-')) { var l = left; var r = ParseProduct(); left = x => l(x) - r(x); }
                else return left;
            }
        }

        private Func<double, double> ParseProduct()
        {
            var left = ParseUnary();
            while (true)
            {
                SkipSpaces();
                if (Eat('*')) { var l = left; var r = ParseUnary(); left = x => l(x) * r(x); }
                else if (Eat('/')) { var l = left; var r = ParseUnary(); left = x => l(x) / r(x); }
                else if (StartsImplicitFactor()) { var l = left; var r = ParsePower(); left = x => l(x) * r(x); }
                else return left;
            }
        }

        // 2x, 2(x+1), x sin(x), 2|x| - a factor directly following another one multiplies it.
        private bool StartsImplicitFactor()
        {
            if (_pos >= src.Length) return false;
            var c = src[_pos];
            return char.IsDigit(c) || c == '.' || char.IsLetter(c) || c == '(' || (c == '|' && _absDepth == 0);
        }

        private Func<double, double> ParseUnary()
        {
            SkipSpaces();
            if (Eat('-')) { var inner = ParseUnary(); return x => -inner(x); }
            if (Eat('+')) return ParseUnary();
            return ParsePower();
        }

        // Right-associative, and binds tighter than unary minus: -x^2 = -(x^2), 2^-1 = 0.5.
        private Func<double, double> ParsePower()
        {
            var b = ParsePrimary();
            SkipSpaces();
            if (!Eat('^')) return b;
            var e = ParseUnary();
            return x => Math.Pow(b(x), e(x));
        }

        private Func<double, double> ParsePrimary()
        {
            SkipSpaces();
            if (_pos >= src.Length) throw new FormatException("výraz končí předčasně");

            var c = src[_pos];
            if (char.IsDigit(c) || c == '.')
            {
                var start = _pos;
                while (_pos < src.Length && (char.IsDigit(src[_pos]) || src[_pos] == '.')) _pos++;
                if (!double.TryParse(src[start.._pos], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    throw new FormatException($"špatné číslo „{src[start.._pos]}“");
                return _ => number;
            }

            if (Eat('('))
            {
                var inner = ParseSum();
                SkipSpaces();
                if (!Eat(')')) throw new FormatException("chybí zavírací závorka )");
                return inner;
            }

            if (c == '|' && Eat('|'))
            {
                _absDepth++;
                var inner = ParseSum();
                _absDepth--;
                SkipSpaces();
                if (!Eat('|')) throw new FormatException("chybí zavírací |");
                return x => Math.Abs(inner(x));
            }

            if (char.IsLetter(c))
            {
                var name = Names.FirstOrDefault(n => string.CompareOrdinal(src, _pos, n, 0, n.Length) == 0)
                    ?? throw new FormatException($"neznámý název „{ReadWord()}“");
                _pos += name.Length;

                if (name == "x") return x => x;
                if (Constants.TryGetValue(name, out var constant)) return _ => constant;

                // sin(x) takes just the bracket, so sin(x)^2 = (sin x)^2; without brackets the
                // argument is the next power-level factor (sin x, sin x^2).
                var fn = Functions[name];
                SkipSpaces();
                var arg = _pos < src.Length && src[_pos] == '(' ? ParsePrimary() : ParsePower();
                return x => fn(arg(x));
            }

            throw new FormatException($"nečekaný znak „{c}“");
        }

        private string ReadWord()
        {
            var end = _pos;
            while (end < src.Length && char.IsLetter(src[end])) end++;
            return src[_pos..end];
        }

        private bool Eat(char c)
        {
            SkipSpaces();
            if (_pos < src.Length && src[_pos] == c) { _pos++; return true; }
            return false;
        }

        private void SkipSpaces()
        {
            while (_pos < src.Length && char.IsWhiteSpace(src[_pos])) _pos++;
        }
    }
}
