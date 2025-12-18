using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.UI;
using Aion.Domain.UI.Navigation;
using Microsoft.AspNetCore.Components;

namespace Aion.AppHost.Services.Navigation;

/// <summary>
/// Default implementation of <see cref="IAionNavigationService"/>.
/// </summary>
public sealed class AionNavigationService : IAionNavigationService
{
    private readonly AionNavigationState _state;
    private readonly IModuleCatalog _catalog;

    public AionNavigationService(AionNavigationState state, IModuleCatalog catalog)
    {
        _state = state;
        _catalog = catalog;
    }

    public async Task OpenModuleAsync(string moduleKey, object? parameters = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(moduleKey))
        {
            throw new ArgumentException("Module key is required.", nameof(moduleKey));
        }

        var module = await _catalog.TryGetByKeyAsync(moduleKey, ct).ConfigureAwait(false);
        if (module is null)
        {
            throw new InvalidOperationException($"Module '{moduleKey}' introuvable ou non autorisé.");
        }

        var componentType = RouteRegistry.Resolve(module.Route);
        if (componentType == typeof(object))
        {
            throw new InvalidOperationException($"Aucun composant enregistré pour la route '{module.Route}'.");
        }

        var mergedParameters = MergeParameters(module.DefaultParameters, parameters);
        var fragment = BuildFragment(componentType, mergedParameters);

        var existing = FindTab(module.Key);
        if (existing is not null)
        {
            existing.Title = module.Title;
            existing.Update(fragment, mergedParameters);
            _state.SetActive(existing.ModuleKey);
            return;
        }

        var tab = new OpenModuleTab(module.Key, module.Title, fragment, mergedParameters, module.Icon);
        _state.AddOrActivate(tab);
    }

    public Task CloseModuleAsync(string moduleKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(moduleKey))
        {
            return Task.CompletedTask;
        }

        _state.Remove(moduleKey);
        return Task.CompletedTask;
    }

    public Task CloseAllAsync(CancellationToken ct = default)
    {
        _state.Clear();
        return Task.CompletedTask;
    }

    public Task FocusModuleAsync(string moduleKey, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(moduleKey))
        {
            _state.SetActive(moduleKey);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<OpenModuleTab> GetOpenTabs() => _state.Tabs.ToList();

    private OpenModuleTab? FindTab(string moduleKey)
    {
        foreach (var tab in _state.Tabs)
        {
            if (string.Equals(tab.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase))
            {
                return tab;
            }
        }

        return null;
    }

    private static RenderFragment BuildFragment(Type componentType, IReadOnlyDictionary<string, object?> parameters)
    {
        return builder =>
        {
            builder.OpenComponent(0, componentType);
            var sequence = 1;
            foreach (var kvp in parameters)
            {
                builder.AddAttribute(sequence++, kvp.Key, kvp.Value);
            }

            builder.CloseComponent();
        };
    }

    private static IReadOnlyDictionary<string, object?> MergeParameters(IReadOnlyDictionary<string, object?> defaults, object? incoming)
    {
        var merged = new Dictionary<string, object?>(defaults, StringComparer.OrdinalIgnoreCase);
        if (incoming is null)
        {
            return merged;
        }

        if (incoming is IReadOnlyDictionary<string, object?> readOnlyDictionary)
        {
            foreach (var kvp in readOnlyDictionary)
            {
                merged[kvp.Key] = kvp.Value;
            }

            return merged;
        }

        if (incoming is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is string key)
                {
                    merged[key] = entry.Value;
                }
            }

            return merged;
        }

        foreach (var property in incoming.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead)
            {
                continue;
            }

            var value = property.GetValue(incoming);
            var key = RouteRegistry.NormalizeParameterKey(property.Name);
            merged[key] = value;
        }

        return merged;
    }
}
