using System.ComponentModel.DataAnnotations;
namespace Aion.DataEngine.Entities
{
    public class STenant : BaseEntity
    {
        [Required, MaxLength(128)]
        public string Name { get; set; } = "DefaultTenant";
    }
}
