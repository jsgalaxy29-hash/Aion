using Aion.DataEngine.Entities;

namespace Aion.DataEngine.Interfaces
{
    public interface IDocumentService
    {
        public Task<int> LinkAsync(string tableName, int recId, string path, string? categorie = null, string? extension = null);
        public Task UnlinkAsync(int docId);
        public Task<IEnumerable<FDocument>> GetAsync(string tableName, int recId, bool withDeleted = false);
    }
}
