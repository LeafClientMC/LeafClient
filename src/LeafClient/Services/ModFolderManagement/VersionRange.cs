using System;
using System.Collections.Generic;
using System.Linq;

namespace LeafClient.Services.ModFolderManagement
{
    public static class VersionRange
    {
        public static bool Matches(string versionRange, string actualVersion)
        {
            if (string.IsNullOrWhiteSpace(versionRange) || versionRange == "*") return true;
            if (string.IsNullOrWhiteSpace(actualVersion)) return false;

            try
            {
                foreach (var alt in versionRange.Split("||", StringSplitOptions.RemoveEmptyEntries))
                {
                    if (MatchesSingle(alt.Trim(), actualVersion.Trim())) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool MatchesSingle(string range, string actual)
        {
            if (range == "*" || range == "any") return true;

            var actualParsed = ParseSemver(actual);
            if (actualParsed == null) return string.Equals(range, actual, StringComparison.OrdinalIgnoreCase);

            if (range.StartsWith("[") || range.StartsWith("("))
            {
                return MatchInterval(range, actualParsed.Value);
            }

            if (range.StartsWith(">="))
                return CompareSemver(actualParsed.Value, ParseSemverOrZero(range[2..])) >= 0;
            if (range.StartsWith("<="))
                return CompareSemver(actualParsed.Value, ParseSemverOrZero(range[2..])) <= 0;
            if (range.StartsWith(">"))
                return CompareSemver(actualParsed.Value, ParseSemverOrZero(range[1..])) > 0;
            if (range.StartsWith("<"))
                return CompareSemver(actualParsed.Value, ParseSemverOrZero(range[1..])) < 0;
            if (range.StartsWith("="))
                return EqualsLoose(actual, range[1..]);
            if (range.StartsWith("~"))
                return MatchTilde(range[1..], actualParsed.Value);
            if (range.StartsWith("^"))
                return MatchCaret(range[1..], actualParsed.Value);

            return EqualsLoose(actual, range);
        }

        private static bool MatchInterval(string range, (int major, int minor, int patch, string? pre) actual)
        {
            bool incLow = range.StartsWith("[");
            bool incHigh = range.EndsWith("]");
            var inner = range.Trim('[', ']', '(', ')');
            var parts = inner.Split(',', 2);
            if (parts.Length != 2) return false;
            var lowStr = parts[0].Trim();
            var highStr = parts[1].Trim();

            if (!string.IsNullOrEmpty(lowStr))
            {
                var low = ParseSemverOrZero(lowStr);
                int cmp = CompareSemver(actual, low);
                if (incLow ? cmp < 0 : cmp <= 0) return false;
            }

            if (!string.IsNullOrEmpty(highStr))
            {
                var high = ParseSemverOrZero(highStr);
                int cmp = CompareSemver(actual, high);
                if (incHigh ? cmp > 0 : cmp >= 0) return false;
            }

            return true;
        }

        private static bool MatchTilde(string baseStr, (int major, int minor, int patch, string? pre) actual)
        {
            var parsed = ParseSemverOrZero(baseStr);
            if (actual.major != parsed.major) return false;
            if (actual.minor != parsed.minor) return false;
            return actual.patch >= parsed.patch;
        }

        private static bool MatchCaret(string baseStr, (int major, int minor, int patch, string? pre) actual)
        {
            var parsed = ParseSemverOrZero(baseStr);
            if (actual.major != parsed.major) return false;
            if (CompareSemver(actual, parsed) < 0) return false;
            return true;
        }

        private static bool EqualsLoose(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            var ap = ParseSemver(a);
            var bp = ParseSemver(b);
            if (ap == null || bp == null) return false;
            return CompareSemver(ap.Value, bp.Value) == 0;
        }

        private static (int major, int minor, int patch, string? pre)? ParseSemver(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return null;
            var v = version.Trim();
            var plusIdx = v.IndexOf('+');
            if (plusIdx >= 0) v = v[..plusIdx];

            string? pre = null;
            var dashIdx = v.IndexOf('-');
            if (dashIdx >= 0)
            {
                pre = v[(dashIdx + 1)..];
                v = v[..dashIdx];
            }

            var parts = v.Split('.');
            int major = 0, minor = 0, patch = 0;
            if (parts.Length >= 1 && !TryParseInt(parts[0], out major)) return null;
            if (parts.Length >= 2) TryParseInt(parts[1], out minor);
            if (parts.Length >= 3) TryParseInt(parts[2], out patch);
            return (major, minor, patch, pre);
        }

        private static (int major, int minor, int patch, string? pre) ParseSemverOrZero(string s)
        {
            return ParseSemver(s) ?? (0, 0, 0, null);
        }

        private static bool TryParseInt(string s, out int result)
        {
            result = 0;
            int i = 0;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            if (i == 0) return false;
            return int.TryParse(s[..i], out result);
        }

        private static int CompareSemver(
            (int major, int minor, int patch, string? pre) a,
            (int major, int minor, int patch, string? pre) b)
        {
            if (a.major != b.major) return a.major.CompareTo(b.major);
            if (a.minor != b.minor) return a.minor.CompareTo(b.minor);
            if (a.patch != b.patch) return a.patch.CompareTo(b.patch);

            bool aPre = !string.IsNullOrEmpty(a.pre);
            bool bPre = !string.IsNullOrEmpty(b.pre);
            if (aPre && !bPre) return -1;
            if (!aPre && bPre) return 1;
            if (!aPre && !bPre) return 0;
            return string.Compare(a.pre, b.pre, StringComparison.Ordinal);
        }
    }
}
