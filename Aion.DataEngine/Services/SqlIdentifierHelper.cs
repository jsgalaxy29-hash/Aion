using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aion.DataEngine.Services;

internal static class SqlIdentifierHelper
{
    private static readonly Regex IdentifierPattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static string QuoteTable(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be empty.", nameof(tableName));
        }

        var segments = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            throw new ArgumentException("Invalid table name.", nameof(tableName));
        }

        return string.Join('.', segments.Select(QuoteSingle));
    }

    public static string QuoteColumn(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ArgumentException("Column name cannot be empty.", nameof(columnName));
        }

        return QuoteSingle(columnName.Trim());
    }

    private static string QuoteSingle(string identifier)
    {
        if (!IdentifierPattern.IsMatch(identifier))
        {
            throw new ArgumentException($"Invalid identifier '{identifier}'.", nameof(identifier));
        }

        return $"[{identifier}]";
    }
}
