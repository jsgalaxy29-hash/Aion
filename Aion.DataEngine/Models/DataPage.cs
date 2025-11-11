using System.Collections.Generic;

namespace Aion.DataEngine.Models;

/// <summary>
/// Represents a page request for a dynamic table.
/// </summary>
public sealed record DataPageRequest(
    string TableName,
    int Skip,
    int Take,
    IReadOnlyList<DataFilter> Filters,
    IReadOnlyList<DataSort> Sorts);

/// <summary>
/// Describes a filter applied to a column.
/// </summary>
public sealed record DataFilter(string FieldName, string? Value, string? Operator);

/// <summary>
/// Describes a sort order applied to a column.
/// </summary>
public sealed record DataSort(string FieldName, bool Descending);

/// <summary>
/// Represents a page of results returned by the data engine.
/// </summary>
public sealed record DataPage(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalCount);
