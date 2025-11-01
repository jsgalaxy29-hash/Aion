using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Domain.UI;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Implémentation en mémoire de ITabService. Gère la liste des onglets ouverts pour la session courante.
    /// </summary>
    public sealed class TabService : ITabService
    {
        public event Action? TabsChanged;
        private readonly List<TabDescriptor> _tabs = new();
        public IReadOnlyList<TabDescriptor> Tabs => _tabs;

        public Task<TabDescriptor> OpenAsync(string title, string route, IDictionary<string, object?>? parameters, bool activate, CancellationToken ct)
        {
            var tab = new TabDescriptor(Guid.NewGuid(), title, route,
                parameters is null ? null : new Dictionary<string, object?>(parameters), false);
            _tabs.Add(tab);
            TabsChanged?.Invoke(); 
            return Task.FromResult(tab);
        }
        public void Activate(Guid id)
        {
            //foreach (var t in _tabs) t.IsActive = (t.Id == id);
            TabsChanged?.Invoke();
        }

        public Task CloseAsync(Guid tabId, CancellationToken ct)
        {
            _tabs.RemoveAll(t => t.Id == tabId);
            return Task.CompletedTask;
        }
    }
}
