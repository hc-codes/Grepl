using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Grepl;

static class ReplacementBreakout
{
    const BindingFlags _bf = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

    static string ExpandReplacement(Match match, string replacement)
    {
        if (replacement == null)
            return string.Empty;

        // Try to get the original input string via reflection (internal property 'Text')
        string inputText = null;
        try
        {
            var pi = match.GetType().GetProperty("Text", _bf);
            if (pi != null)
                inputText = pi.GetValue(match) as string;
        }
        catch { }

        if (inputText == null)
        {
            // fallback to using the whole match value as input (limits some features)
            inputText = match.Value;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < replacement.Length; i++)
        {
            char c = replacement[i];
            if (c == '$' && i + 1 < replacement.Length)
            {
                char next = replacement[i + 1];
                if (next == '$')
                {
                    sb.Append('$');
                    i++;
                    continue;
                }
                else if (next == '&')
                {
                    sb.Append(match.Value);
                    i++;
                    continue;
                }
                else if (next == '`')
                {
                    // left substring
                    if (match.Index >= 0 && match.Index <= inputText.Length)
                        sb.Append(inputText.Substring(0, match.Index));
                    i++;
                    continue;
                }
                else if (next == '\'')
                {
                    // right substring using $' (apostrophe)
                    int start = match.Index + match.Length;
                    if (start >= 0 && start <= inputText.Length)
                        sb.Append(inputText.Substring(start));
                    i++;
                    continue;
                }
                else if (next == '+')
                {
                    // last captured group ($+)
                    for (int g = match.Groups.Count - 1; g >= 1; g--)
                    {
                        var gr = match.Groups[g];
                        if (gr.Success)
                        {
                            sb.Append(gr.Value);
                            break;
                        }
                    }
                    i++;
                    continue;
                }
                else if (char.IsDigit(next))
                {
                    int j = i + 1;
                    int val = 0;
                    while (j < replacement.Length && char.IsDigit(replacement[j]))
                    {
                        val = val * 10 + (replacement[j] - '0');
                        j++;
                        if (val > 99) break;
                    }
                    if (val > 0)
                    {
                        if (val < match.Groups.Count)
                        {
                            var g = match.Groups[val];
                            if (g.Success)
                                sb.Append(g.Value);
                        }
                        i = j - 1;
                        continue;
                    }
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    internal static string Call(MethodInfo methodInfo, Match match, Type typeRr, object rr, Type typeVsb)
    {
        // Best-effort: try to extract pattern from internal replacement object and expand
        if (rr != null)
        {
            try
            {
                var pi = rr.GetType().GetProperty("Pattern", _bf);
                if (pi != null)
                {
                    var pattern = pi.GetValue(rr) as string;
                    if (pattern != null)
                        return ExpandReplacement(match, pattern);
                }
            }
            catch { }
        }

        // As a last resort return empty
        return string.Empty;
    }

    public static IEnumerable<string> ReplaceBreakout(this Regex rx, string input, string replacement)
    {
        if (input == null) yield break;
        foreach (Match match in rx.Matches(input))
        {
            yield return ReplaceBreakout(rx, match, input, replacement);
        }
    }

    public static string ReplaceBreakout(this Regex rx, Match match, string input, string replacement)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));
        return ExpandReplacement(match, replacement);
    }
}
