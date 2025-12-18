using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aion.Domain.Contracts
{
    /// <summary>
    /// Interface de résolution des DataQueryRef des widgets. Permet de décorréler le widget de la manière dont on obtient les données.
    /// </summary>
    public interface IDataQueryResolver
    {
        /// <summary>
        /// Exécute la requête référencée et renvoie un objet quelconque (souvent un tableau ou un type projeté).
        /// Les settings peuvent servir à personnaliser la requête (période, filtrage…).
        /// </summary>
        Task<object?> ExecuteAsync(string dataQueryRef, IDictionary<string, object?>? settings, CancellationToken ct);
    }
}
