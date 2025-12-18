using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aion.Domain.UI.DynamicLayouts;
using Microsoft.Extensions.Hosting;

namespace Aion.Infrastructure.Services
{
    /// <summary>
    /// Stocke les layouts de grille dans des fichiers JSON sur le disque.
    /// </summary>
    public sealed class FileDynamicLayoutStore : IDynamicLayoutStore
    {
        private readonly IHostEnvironment _environment;
        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public FileDynamicLayoutStore(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<DynamicGridLayout?> LoadLayoutAsync(string tableName, int tenantId, int userId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return null;
            }

            var path = GetLayoutPath(tableName, tenantId, userId);
            if (!File.Exists(path))
            {
                return null;
            }

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<DynamicGridLayout>(stream, _serializerOptions, ct).ConfigureAwait(false);
        }

        public async Task SaveLayoutAsync(string tableName, int tenantId, int userId, DynamicGridLayout layout, CancellationToken ct)
        {
            if (layout is null) throw new ArgumentNullException(nameof(layout));

            var path = GetLayoutPath(tableName, tenantId, userId);
            var directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);

            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, layout, _serializerOptions, ct).ConfigureAwait(false);
        }

        private string GetLayoutPath(string tableName, int tenantId, int userId)
        {
            var root = Path.Combine(_environment.ContentRootPath, "App_Data", "layouts");
            var fileName = $"{tenantId}_{userId}_{tableName}.json";
            return Path.Combine(root, fileName);
        }
    }
}
