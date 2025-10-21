using System;
using System.Collections.Generic;

namespace Aion.Domain.UI
{
    /// <summary>
    /// Résout une route logique vers un Type de composant Blazor.
    /// Les modules appellent Register au démarrage pour publier leurs écrans.
    /// </summary>
    public static class RouteRegistry
    {
        private static readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase);

        public static void Register(string route, Type componentType) => _map[route] = componentType;
        public static Type Resolve(string route) => _map.TryGetValue(route, out var t) ? t : typeof(object);

        // Helper pour paramètres (optionnel pour l’instant)
        public static IDictionary<string, object?>? ToParameters(IDictionary<string, object?>? p) => p;
    }
}
