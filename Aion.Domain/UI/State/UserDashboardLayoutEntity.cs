using System;

namespace Aion.Domain.UI.State
{
    /// <summary>
    /// Positionnement d'un widget sur le tableau de bord d'un utilisateur.
    /// X et Y sont des cellules de la grille, W et H définissent la largeur/hauteur.
    /// Les paramètres supplémentaires sont sérialisés en JSON.
    /// </summary>
    public sealed class UserDashboardLayoutEntity
    {
        public long Id { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string WidgetCode { get; set; } = default!;
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public string? SettingsJson { get; set; }
    }
}
