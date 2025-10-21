using System;
using System.Collections.Generic;

namespace Aion.Domain.UI
{
    /// <summary>
    /// Modèle léger représentant un onglet ouvert dans l'interface. Conserve l'identifiant, le titre, la route et les paramètres éventuels.
    /// </summary>
    public sealed record TabDescriptor(Guid Id, string Title, string Route, IDictionary<string, object?>? Parameters, bool IsDirty);
}
