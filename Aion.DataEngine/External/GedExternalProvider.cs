using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.External
{
    /// <summary>
    /// External source provider for GED (Gestion Électronique de Documents) or
    /// any custom document storage system.  This provider is a stub and does
    /// not implement actual interactions with a GED system.  It illustrates
    /// where such integration could be added in the future.
    /// </summary>
    public class GedExternalProvider : IExternalSourceProvider
    {
        public string Type => "GED";
        public Task<IEnumerable<IDictionary<string, object?>>> ReadAsync(SSourceExterne source, SSourceBinding binding, IDictionary<string, object?>? context)
        {
            throw new NotImplementedException("GED provider is not implemented. Implement according to your GED system's API.");
        }
        public Task WriteAsync(SSourceExterne source, SSourceBinding binding, IEnumerable<IDictionary<string, object?>> rows)
        {
            throw new NotImplementedException("GED provider is not implemented. Implement according to your GED system's API.");
        }
    }
}