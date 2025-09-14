using System.Text.RegularExpressions;

namespace Serilog.MemoryMapped;

internal static class NameNormalizer
{
    private static readonly Regex Allowed = new("[^A-Za-z0-9_.-]", RegexOptions.Compiled);

    public static string Normalize(string raw, string prefix)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Name cannot be null/empty.", nameof(raw));

        // Replace all disallowed chars with '_'
        string safe = Allowed.Replace(raw, "_");

        // Ensure it has a prefix (helps avoid collisions and enforces a valid start char)
        return $"{prefix}{safe}";
    }
}