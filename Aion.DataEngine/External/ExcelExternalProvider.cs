using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.External
{
    /// <summary>
    /// External source provider for Excel workbooks.  This provider is a
    /// placeholder: reading and writing Excel files require additional
    /// dependencies (e.g. ClosedXML).  Until those dependencies are added
    /// to the project, the provider throws <see cref="NotImplementedException"/>.
    /// </summary>
    public class ExcelExternalProvider : IExternalSourceProvider
    {
        public string Type => "EXCEL";
        public Task<IEnumerable<IDictionary<string, object?>>> ReadAsync(SSourceExterne source, SSourceBinding binding, IDictionary<string, object?>? context)
        {
            throw new NotImplementedException("Excel provider is not implemented yet. Install ClosedXML or another library to enable reading.");
        }
        public Task WriteAsync(SSourceExterne source, SSourceBinding binding, IEnumerable<IDictionary<string, object?>> rows)
        {
            throw new NotImplementedException("Excel provider is not implemented yet. Install ClosedXML or another library to enable writing.");
        }
    }
}