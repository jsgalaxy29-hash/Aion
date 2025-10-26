using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Aion.DataEngine.Services
{
    /// <summary>
    /// Basic implementation of <see cref="IValidationService"/> that enforces
    /// nullability, string length and regular expressions defined in the
    /// metadata.  This implementation can be extended to support numeric ranges,
    /// date ranges and custom rules.
    /// </summary>
    public class SimpleValidationService : IValidationService
    {
        /// <summary>
        /// Container for variables exposed to validation scripts.  When
        /// evaluating a <see cref="SField.ValidationScript"/> this class
        /// provides access to the current value, the full record dictionary,
        /// the column definition and the table definition.  Scripts can
        /// reference these properties to perform custom checks.
        /// </summary>
        private class ScriptGlobals
        {
            /// <summary>
            /// The value currently being validated.  This may be null.
            /// </summary>
            public object? Value { get; set; }

            /// <summary>
            /// The dictionary of all values for the record being processed.
            /// </summary>
            public IDictionary<string, object?> Values { get; set; } = new Dictionary<string, object?>();

            /// <summary>
            /// The column definition associated with this value.
            /// </summary>
            public SField Field { get; set; } = default!;

            /// <summary>
            /// The table definition associated with this value.
            /// </summary>
            public STable Table { get; set; } = default!;
        }
        /// <inheritdoc />
        public async Task ValidateAsync(STable table, IEnumerable<SField> fields, IDictionary<string, object?> values)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            if (values == null) throw new ArgumentNullException(nameof(values));

            foreach (var field in fields)
            {
                // Attempt to get the value from the record dictionary.  If the
                // value is missing it will be treated as null.
                values.TryGetValue(field.Libelle, out var value);

                // Nullability check for all types
                if (!field.IsNulleable && value is null)
                {
                    throw new InvalidOperationException($"The column '{field.Libelle}' cannot be null.");
                }

                // Validate ranges for numeric or date values.  Convert the
                // provided value to a comparable type when possible.
                if (value != null && !string.IsNullOrEmpty(field.Min))
                {
                    // Attempt to parse the value and the min bound as either
                    // numbers or DateTime.  If parsing fails the bound is
                    // ignored.
                    if (double.TryParse(value.ToString(), out var doubleVal) && double.TryParse(field.Min, out var minVal))
                    {
                        if (doubleVal < minVal)
                        {
                            throw new InvalidOperationException($"The value for '{field.Libelle}' is less than the minimum allowed ({field.Min}).");
                        }
                    }
                    else if (DateTime.TryParse(value.ToString(), out var dateVal) && DateTime.TryParse(field.Min, out var minDate))
                    {
                        if (dateVal < minDate)
                        {
                            throw new InvalidOperationException($"The value for '{field.Libelle}' is earlier than the minimum allowed ({field.Min}).");
                        }
                    }
                }
                if (value != null && !string.IsNullOrEmpty(field.Max))
                {
                    if (double.TryParse(value.ToString(), out var doubleVal) && double.TryParse(field.Max, out var maxVal))
                    {
                        if (doubleVal > maxVal)
                        {
                            throw new InvalidOperationException($"The value for '{field.Libelle}' exceeds the maximum allowed ({field.Max}).");
                        }
                    }
                    else if (DateTime.TryParse(value.ToString(), out var dateVal) && DateTime.TryParse(field.Max, out var maxDate))
                    {
                        if (dateVal > maxDate)
                        {
                            throw new InvalidOperationException($"The value for '{field.Libelle}' is later than the maximum allowed ({field.Max}).");
                        }
                    }
                }

                // If the value is a string perform string-specific checks
                if (value is string strVal)
                {
                    // Length check
                    if (field.Taille > 0 && strVal.Length > field.Taille)
                    {
                        throw new InvalidOperationException($"The value for '{field.Libelle}' exceeds the maximum length of {field.Taille}.");
                    }
                }

                // Custom script validation.  Scripts should return either a
                // boolean (indicating success/failure) or a string with a
                // custom error message.  Any other return type is ignored.
                if (!string.IsNullOrWhiteSpace(field.ValidationScript))
                {
                    var globals = new ScriptGlobals
                    {
                        Value = value,
                        Values = values,
                        Field = field,
                        Table = table
                    };
                    // Configure script options to reference common assemblies
                    // and import useful namespaces.  Additional references
                    // (e.g., System.Linq) can be added if needed.
                    var scriptOptions = ScriptOptions.Default
                        .AddReferences(typeof(object).Assembly)
                        .AddReferences(typeof(Enumerable).Assembly)
                        .AddImports("System", "System.Linq", "System.Collections.Generic");
                    try
                    {
                        var result = await CSharpScript.EvaluateAsync<object>(field.ValidationScript, scriptOptions, globals).ConfigureAwait(false);
                        if (result is bool boolResult)
                        {
                            if (!boolResult)
                            {
                                throw new InvalidOperationException($"Validation script for '{field.Libelle}' returned false.");
                            }
                        }
                        else if (result is string errorMessage)
                        {
                            throw new InvalidOperationException(errorMessage);
                        }
                    }
                    catch (CompilationErrorException cex)
                    {
                        // Surface compilation errors to the caller.  This
                        // allows misconfigured scripts to be diagnosed.
                        var errors = string.Join("; ", cex.Diagnostics.Select(d => d.ToString()));
                        throw new InvalidOperationException($"Validation script compilation failed for column '{field.Libelle}': {errors}");
                    }
                    catch (Exception ex)
                    {
                        // Any exception thrown by the script is wrapped into
                        // an InvalidOperationException so the caller knows the
                        // validation failed.
                        throw new InvalidOperationException($"Validation script threw an exception for column '{field.Libelle}': {ex.Message}");
                    }
                }

                // YAML validation expression.  At present YAML rules are not
                // implemented.  Developers can extend this block to parse
                // field.ValidationYaml using a YAML parser such as YamlDotNet
                // and evaluate the rules at runtime.  If YAML is provided
                // this implementation raises an exception to signal that
                // functionality is not available.
                if (!string.IsNullOrWhiteSpace(field.ValidationYaml))
                {
                    throw new NotImplementedException($"YAML validation is not implemented for column '{field.Libelle}'.");
                }

                // Regular expression validation.  If a regex is provided and no
                // custom script or YAML validation was executed, apply it to
                // string values.
                if (!string.IsNullOrWhiteSpace(field.Regex) && value is string val)
                {
                    var regex = new Regex(field.Regex);
                    if (!regex.IsMatch(val))
                    {
                        throw new InvalidOperationException($"The value for '{field.Libelle}' does not match the expected format.");
                    }
                }
            }
        }
    }
}