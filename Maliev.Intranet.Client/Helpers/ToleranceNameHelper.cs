namespace Maliev.Intranet.Client.Helpers;

/// <summary>Utilities for rendering tolerance display names.</summary>
public static class ToleranceNameHelper
{
    /// <summary>
    /// Splits a tolerance name like "Fine (ISO 2768-f)" into ("Fine", "ISO 2768-f").
    /// Returns ("name", "") when there is no parenthetical.
    /// </summary>
    public static (string Head, string Tail) Split(string name)
    {
        var open = name.IndexOf('(');
        if (open < 0) return (name, "");
        var close = name.IndexOf(')', open);
        var head = name[..open].TrimEnd();
        var tail = close > open ? name.Substring(open + 1, close - open - 1) : name[(open + 1)..];
        return (head, tail);
    }
}
