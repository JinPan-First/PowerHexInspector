using System.Text.RegularExpressions;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.HexInspector
{
    public static class QueryInterpretHelper
    {
        private static readonly string FilterPattern = "_,";

        private static string ReplaceFirstOccurrence(string input, string pattern, string replacement)
        {
            Regex regex = new(pattern);
            return regex.Replace(input, replacement, 1);
        }

        public static void QueryInterpret(Query query, out Base queryBase, out string queryValue, out bool isUpper)
        {
            var terms = query.Terms;
            var raw = query.RawUserQuery.Trim().Substring(query.ActionKeyword.Length).Trim();
            queryBase = Base.Invalid;
            queryValue = "";
            isUpper = true;
            // Use C-Style:
            // {value}   -> Decimal
            // 0{value}  -> Octal
            // 0x{value} -> Hex
            // 0b{value} -> Binary
            // "{value}" -> ASCII
            if (terms.Count == 1 || terms[0][0] == '"')
            {
                string decimalPattern = @"^[+-]?([1-9][0-9,_]*|0)(\.[0-9,_]+)?([hH]|[fF]|[dD])?$";
                string octalPattern   = @"^[+-]?(0[0-7,_]+)$";
                string hexPattern     = @"^[+-]?(0[Xx][0-9a-fA-F,_]+)$";
                string binaryPattern  = @"^[+-]?(0[Bb][01,_]+)$";
                string asciiPattern   = "^\".*\"$";

                if (Regex.IsMatch(raw, decimalPattern) || raw == "0" || raw == "-0")
                {
                    if (raw.Contains('.'))
                    {
                        if (raw.EndsWith("h", StringComparison.OrdinalIgnoreCase))
                        {
                            queryBase = Base.Fra16;
                            queryValue = raw[..^1];
                        }
                        else if (raw.EndsWith("f", StringComparison.OrdinalIgnoreCase))
                        {
                            queryBase = Base.Fra32;
                            queryValue = raw[..^1];
                        }
                        else if (raw.EndsWith("d", StringComparison.OrdinalIgnoreCase))
                        {
                            queryBase = Base.Fra64;
                            queryValue = raw[..^1];
                        }
                        else
                        {
                            queryBase = Base.Fra32; // Default to float32 if no suffix is provided
                            queryValue = raw;
                        }
                    }
                    else
                    {
                        queryBase = Base.Dec;
                        queryValue = raw;
                    }
                }
                else if (Regex.IsMatch(raw, octalPattern))
                {
                    queryBase = Base.Oct;
                    queryValue = ReplaceFirstOccurrence(raw, "0", "");
                }
                else if (Regex.IsMatch(raw, hexPattern))
                {
                    queryBase = Base.Hex;
                    queryValue = ReplaceFirstOccurrence(raw, "0[Xx]", "");
                }
                else if (Regex.IsMatch(raw, binaryPattern))
                {
                    queryBase = Base.Bin;
                    queryValue = ReplaceFirstOccurrence(raw, "0[Bb]", "");
                }
                else if (Regex.IsMatch(raw, asciiPattern))
                {
                    queryBase = Base.Ascii;
                    queryValue = raw[1..^1]; // only remove the first and last double quotes
                }
            }

            if (queryBase != Base.Ascii)
            {
                queryValue = Regex.Replace(queryValue, $"[{FilterPattern}]", "");
                queryValue = queryValue.Trim().Replace(" ", "");
            }

            return;
        }
    }
}