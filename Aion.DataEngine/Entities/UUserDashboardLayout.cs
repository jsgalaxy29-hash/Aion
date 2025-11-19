using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Positionnement persistant des widgets pour un utilisateur donné.
    /// </summary>
    public class UUserDashboardLayout : BaseEntity
    {
        public int UserId { get; set; }

        [Required, MaxLength(128)]
        public string WidgetCode { get; set; } = string.Empty;

        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        public string? SettingsJson { get; set; }
    }
}
