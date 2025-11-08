using System;
using System.Collections.Generic;

namespace Aion.Domain.UI
{
    /// <summary>
    /// Modèle léger représentant un onglet ouvert dans l'interface. Conserve l'identifiant, le titre, la route et les paramètres éventuels.
    /// </summary>
    public sealed class TabDescriptor
    {
        public TabDescriptor(Guid id, string title, string route, IDictionary<string, object?>? parameters,  bool isDirty)
        {
            Id = id;
            Title = title;
            Route = route;
            Parameters = parameters ?? new Dictionary<string, object?>();
            IsDirty = isDirty;
            IsActive = false;
        }
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Route { get; set; }
        public IDictionary<string, object?> Parameters { get; set; }
        public bool IsDirty { get; set; }
        public bool IsActive { get; set; }
    }
}
