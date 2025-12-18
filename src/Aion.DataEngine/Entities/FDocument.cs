using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    public class FDocument : BaseEntity
    {
        [Required]
        public string TableName { get; set; } = string.Empty;
        [Required] 
        public int RecordId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string? Extension { get; internal set; }
        public string Path { get; internal set; }
        public string? Categorie { get; internal set; }
    }
}