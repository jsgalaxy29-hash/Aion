using System;
using System.Collections.Generic;

namespace Aion.UI.Models
{
    /// <summary>
    /// Représente une ligne affichée dans la grille dynamique.
    /// </summary>
    public sealed class DynamicRow
    {
        public DynamicRow(Dictionary<string, object?> values)
        {
            Values = values;
        }

        public Dictionary<string, object?> Values { get; }
        public Dictionary<string, string?> EditValues { get; } = new();
        public bool IsEditing { get; set; }
        public bool IsNew { get; set; }
        public string? PrimaryKeyName { get; set; }
        public object? PrimaryKeyValue { get; set; }
        public string? DisplayLabel { get; set; }

        public object? GetValue(string fieldName)
            => Values.TryGetValue(fieldName, out var value) ? value : null;

        public string? GetValueAsString(string fieldName)
        {
            var value = GetValue(fieldName);
            if (value is null || value is System.DBNull)
            {
                return null;
            }

            return value switch
            {
                DateTime dt => dt.ToString("g"),
                DateOnly d => d.ToString("d"),
                TimeOnly t => t.ToString("t"),
                _ => Convert.ToString(value)
            };
        }

        public void SetValue(string fieldName, object? value)
            => Values[fieldName] = value;

        public bool HasPrimaryKey => !string.IsNullOrWhiteSpace(PrimaryKeyName);

        public string? GetEditValue(string fieldName)
        {
            if (EditValues.TryGetValue(fieldName, out var value))
            {
                return value;
            }

            var original = GetValue(fieldName);
            return original?.ToString();
        }

        public void SetEditValue(string fieldName, string? value)
            => EditValues[fieldName] = value;

        public bool GetEditBool(string fieldName)
        {
            if (EditValues.TryGetValue(fieldName, out var value) && bool.TryParse(value, out var b))
            {
                return b;
            }

            var original = GetValue(fieldName);
            if (original is bool boolValue)
            {
                return boolValue;
            }

            return false;
        }

        public void SetEditBool(string fieldName, bool value)
            => EditValues[fieldName] = value ? bool.TrueString : bool.FalseString;

        public void ClearEditValues()
            => EditValues.Clear();

        public object? this[string fieldName]
        {
            get => GetValue(fieldName);
            set => SetValue(fieldName, value);
        }
    }
}
