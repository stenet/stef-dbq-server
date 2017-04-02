using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stef.DatabaseQuery.Business.Enumerations;

namespace Stef.DatabaseQuery.Business
{
    public static class Utils
    {
        private const string ParameterPattern = "<#{0}#>";
        private static Regex _ParameterRegex = new Regex("(?<startExpr>(<#(?<name>(.*?))#>))", RegexOptions.Singleline);

        public static IEnumerable<string> GetParameters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            return _ParameterRegex
                .Matches(text)
                .OfType<Match>()
                .Select(c => c.Groups["name"].Value)
                .Distinct()
                .ToList();
        }
        public static string GetParameter(string name)
        {
            return string.Format(ParameterPattern, name);
        }

        public static IEnumerable<string> GetCommaSeparatedItemList(string text)
        {
            var matches = Regex.Matches(text, ",", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            var checkedMatches = matches
                .OfType<Match>()
                .Where(c => IsOuterSegment(text, c.Index))
                .OrderBy(c => c.Index)
                .ToList();

            var resultList = new List<string>();

            if (checkedMatches.Count == 0)
            {
                resultList.Add(GetRemovedWhitespaces(text));
            }
            else
            {
                for (var i = 0; i < checkedMatches.Count; i++)
                {
                    var match = checkedMatches[i];

                    if (i == 0)
                        resultList.Add(GetRemovedWhitespaces(text.Substring(0, match.Index)));

                    var start = match.Index + match.Length;

                    var length = i + 1 == checkedMatches.Count
                        ? text.Length
                        : checkedMatches[i + 1].Index;

                    resultList.Add(GetRemovedWhitespaces(text.Substring(start, length - start)));
                }
            }

            return resultList;
        }
        public static string GetFormattedStatement(string statement, SqlFormatRule formatRule)
        {
            var matches = Regex.Matches(statement, @"\s+");

            var checkedMatches = matches
                .OfType<Match>()
                .Where(c => IsOuterSegment(statement, c.Index, true))
                .OrderBy(c => c.Index)
                .ToList();

            var builder = new StringBuilder();

            for (var i = 0; i < checkedMatches.Count; i++)
            {
                var match = checkedMatches[i];

                if (i == 0)
                    builder.Append(statement.Substring(0, match.Index));

                if (i + 1 == checkedMatches.Count)
                {
                    if (statement.Length >= match.Index + match.Length)
                    {
                        builder.Append(" ");
                        builder.Append(statement.Substring(match.Index + match.Length));
                    }
                }
                else
                {
                    builder.Append(" ");

                    var start = match.Index + match.Length;
                    builder.Append(statement.Substring(start, checkedMatches[i + 1].Index - start));
                }

            }

            statement = builder.ToString();

            matches = Regex.Matches(statement, @"\b(select|from|where|group by|having|order by|create|values|set|and|or)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            checkedMatches = matches
                .OfType<Match>()
                .Where(c => IsOuterSegment(statement, c.Index, true))
                .OrderByDescending(c => c.Index)
                .ToList();

            var preOneNewLine = new List<string>()
                {
                    "values",
                    "and",
                    "or"
                };
            var preTwoNewLines = new List<string>()
                {
                    "from",
                    "where",
                    "group by",
                    "having",
                    "order by",
                    "set",
                    "union"
                };
            var postOneNewLine = new List<string>()
                {
                    "select",
                    "union"
                };

            foreach (var match in checkedMatches)
            {
                var start = match.Index;
                var end = match.Index + match.Length;

                if (postOneNewLine.Contains(match.Value.ToLower()))
                {
                    if (statement.Length >= end + 1 && statement.Substring(end, 1) == " ")
                        statement = statement.Remove(end, 1);

                    statement = statement.Insert(match.Index + match.Length, Environment.NewLine);
                }

                if (preTwoNewLines.Contains(match.Value.ToLower()))
                {
                    statement = statement.Insert(match.Index, Environment.NewLine + Environment.NewLine);

                    if (start > 1 && statement.Substring(start - 1, 1) == " ")
                        statement = statement.Remove(start - 1, 1);
                }
                else if (preOneNewLine.Contains(match.Value.ToLower()))
                {
                    statement = statement.Insert(match.Index, Environment.NewLine);

                    if (start > 1 && statement.Substring(start - 1, 1) == " ")
                        statement = statement.Remove(start - 1, 1);
                }
            }

            if (formatRule == SqlFormatRule.EveryColumnInOneLine)
            {
                Regex
                    .Matches(statement, @"\s*,\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline)
                    .OfType<Match>()
                    .Where(c => IsOuterSegment(statement, c.Index))
                    .OrderByDescending(c => c.Index)
                    .ToList()
                    .ForEach(c => statement = statement.Insert(c.Index + c.Length, Environment.NewLine));
            }

            return statement;
        }
        public static string GetRemovedWhitespaces(string text)
        {
            return text.TrimStart().TrimEnd();
        }
        public static ScriptType GetScriptType(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return ScriptType.Unknown;

            if (sql.ToLower().TrimStart().TrimEnd() == "commit")
                return ScriptType.Commit;
            else if (sql.ToLower().TrimStart().TrimEnd() == "rollback")
                return ScriptType.Rollback;
            else if (sql.ToLower().TrimStart().StartsWith("::"))
                return ScriptType.Script;
            else if (sql.ToLower().TrimStart().StartsWith("select"))
                return ScriptType.Query;
            else
                return ScriptType.NonQuery;
        }
        public static bool IsOuterSegment(string text, int index, bool onlyStrings = false)
        {
            var subText = text.Substring(0, index);

            if (!onlyStrings)
            {
                var count1 = subText.Count(c => c == '(');
                var count2 = subText.Count(c => c == ')');

                if (count1 != count2)
                    return false;

                count1 = subText.Count(c => c == '[');
                count2 = subText.Count(c => c == ']');

                if (count1 != count2)
                    return false;
            }

            {
                var count1 = subText.Count(c => c == '"');

                if (count1 % 2 != 0)
                    return false;

                count1 = subText.Count(c => c == '\'');

                if (count1 % 2 != 0)
                    return false;
            }

            return true;
        }
        public static bool IsAllUpper(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsLower(value[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
