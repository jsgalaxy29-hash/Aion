using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Providers
{
    /// <summary>
    /// Placeholder implementation of <see cref="IDataProvider"/> for Amazon S3.
    /// This provider is not implemented in the current version.  To use
    /// Amazon S3 as a data source, one would need to reference AWS SDK and
    /// implement the necessary logic to read objects from S3 and map them to
    /// tables.
    /// </summary>
    public class S3DataProvider : IDataProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="S3DataProvider"/>.
        /// </summary>
        /// <param name="bucketName">The S3 bucket name.</param>
        /// <param name="region">The AWS region.</param>
        public S3DataProvider(string bucketName, string region)
        {
            // This constructor is intentionally left empty.  The provider
            // requires external dependencies (AWS SDK) which are not included.
        }

        /// <inheritdoc />
        public string ConnectionString => "s3://";

        /// <inheritdoc />
        public Task<IDbTransaction> BeginTransactionAsync()
        {
            throw new NotSupportedException("Transactions are not supported by S3DataProvider.");
        }

        /// <inheritdoc />
        public Task<int> ExecuteNonQueryAsync(string commandText, IDictionary<string, object?>? parameters = null)
        {
            throw new NotSupportedException("ExecuteNonQueryAsync is not supported by S3DataProvider.");
        }

        /// <inheritdoc />
        public Task<DataTable> ExecuteQueryAsync(string commandText, IDictionary<string, object?>? parameters = null)
        {
            throw new NotSupportedException("ExecuteQueryAsync is not implemented for S3DataProvider.  AWS SDK integration is required.");
        }
    }
}