using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.External
{
    /// <summary>
    /// External source provider for Amazon S3.  This provider is a stub
    /// illustrating where integration with the AWS SDK would occur.  The
    /// implementation is intentionally left incomplete because the AWS
    /// SDK is not referenced by this project.  To enable S3 access, add
    /// the AWS SDK packages and implement the methods accordingly.
    /// </summary>
    public class S3ExternalProvider : IExternalSourceProvider
    {
        public string Type => "S3";
        public Task<IEnumerable<IDictionary<string, object?>>> ReadAsync(SSourceExterne source, SSourceBinding binding, IDictionary<string, object?>? context)
        {
            throw new NotImplementedException("S3 provider is not implemented. Add AWS SDK packages to support S3.");
        }
        public Task WriteAsync(SSourceExterne source, SSourceBinding binding, IEnumerable<IDictionary<string, object?>> rows)
        {
            throw new NotImplementedException("S3 provider is not implemented. Add AWS SDK packages to support S3.");
        }
    }
}