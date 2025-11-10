using System.Collections.Generic;

namespace Aion.Infrastructure.Options
{
    /// <summary>
    /// Options for <see cref="Services.DataQueryResolver"/> to restrict outbound API calls.
    /// </summary>
    public sealed class DataQueryResolverOptions
    {
        /// <summary>
        /// Collection of absolute base URLs allowed for <c>api:</c> queries.
        /// Only URLs that are a descendant of one of these bases will be executed.
        /// </summary>
        public IList<string> AllowedApiBaseUrls { get; } = new List<string>();
    }
}
