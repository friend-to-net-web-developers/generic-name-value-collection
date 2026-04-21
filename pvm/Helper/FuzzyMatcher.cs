using System;
using System.Collections.Generic;
using System.Linq;

namespace pvm.Helper;

public static class FuzzyMatcher
{
    public static string? Suggest(string input, IEnumerable<string> candidates, int maxDistance = 3)
    {
        string? bestMatch = null;
        int minDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            int distance = LevenshteinDistance(input.ToLowerInvariant(), candidate.ToLowerInvariant());
            if (distance < minDistance && distance <= maxDistance)
            {
                minDistance = distance;
                bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
