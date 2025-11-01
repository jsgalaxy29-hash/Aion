using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.UI;

namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Gère l'état des onglets ouverts pour un utilisateur. Permet d'en ouvrir ou de fermer.
    /// </summary>
    public interface ITabService
    {
        public event Action? TabsChanged;
        IReadOnlyList<TabDescriptor> Tabs { get; }
        Task<TabDescriptor> OpenAsync(string title, string route, IDictionary<string, object?>? parameters, bool activate, CancellationToken ct);
        Task CloseAsync(Guid tabId, CancellationToken ct);
    }
}
