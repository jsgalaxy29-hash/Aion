using System;

namespace Aion.Security.Models
{
    /// <summary>
    /// Flags représentant les 5 axes de droits disponibles.
    /// La sémantique exacte dépend du SRightType (Right1Name, Right2Name...).
    /// </summary>
    [Flags]
    public enum RightFlag
    {
        None = 0,
        Right1 = 1,     // Ex: Lecture/Voir/Accéder
        Right2 = 2,     // Ex: Écriture/Modifier/Créer
        Right3 = 4,     // Ex: Suppression
        Right4 = 8,     // Ex: Exécution/Validation
        Right5 = 16,    // Ex: Administration
        All = Right1 | Right2 | Right3 | Right4 | Right5
    }

    /// <summary>
    /// Représente les droits effectifs d'un utilisateur sur une ressource.
    /// </summary>
    public class UserRights
    {
        public string Target { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public bool Right1 { get; set; }
        public bool Right2 { get; set; }
        public bool Right3 { get; set; }
        public bool Right4 { get; set; }
        public bool Right5 { get; set; }

        public bool HasRight(RightFlag flag)
        {
            return flag switch
            {
                RightFlag.Right1 => Right1,
                RightFlag.Right2 => Right2,
                RightFlag.Right3 => Right3,
                RightFlag.Right4 => Right4,
                RightFlag.Right5 => Right5,
                _ => false
            };
        }

        public bool HasAnyRight() => Right1 || Right2 || Right3 || Right4 || Right5;
    }
}