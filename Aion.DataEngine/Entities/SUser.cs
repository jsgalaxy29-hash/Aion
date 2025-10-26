using System;
using System.ComponentModel.DataAnnotations;

namespace Aion.DataEngine.Entities
{
    /// <summary>
    /// Utilisateur du syst�me Aion.
    /// G�re l'authentification et les informations utilisateur de base.
    /// Les droits sont g�r�s via l'appartenance aux groupes (<see cref="SUserGroup"/>).
    /// </summary>
    public class SUser : BaseEntity
    {
        /// <summary>
        /// Nom d'utilisateur (login).
        /// </summary>
        [Required, MaxLength(128)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Version normalis�e du nom d'utilisateur (uppercase pour recherche).
        /// </summary>
        [MaxLength(128)]
        public string NormalizedUserName { get; set; } = string.Empty;

        /// <summary>
        /// Email de l'utilisateur.
        /// </summary>
        [MaxLength(256)]
        public string? Email { get; set; }

        /// <summary>
        /// Version normalis�e de l'email.
        /// </summary>
        [MaxLength(256)]
        public string? NormalizedEmail { get; set; }

        /// <summary>
        /// Hash du mot de passe (BCrypt ou PBKDF2).
        /// </summary>
        [MaxLength(512)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Nom complet de l'utilisateur.
        /// </summary>
        [MaxLength(256)]
        public string? FullName { get; set; }

        /// <summary>
        /// Indique si l'utilisateur est actif (peut se connecter).
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date de derni�re connexion.
        /// </summary>
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Nombre d'�checs de connexion cons�cutifs.
        /// </summary>
        public int AccessFailedCount { get; set; } = 0;

        /// <summary>
        /// Date de verrouillage du compte (si d�passement tentatives).
        /// </summary>
        public DateTime? LockoutEnd { get; set; }

        // Navigation properties
        public virtual ICollection<SUserGroup> UserGroups { get; set; } = new List<SUserGroup>();
    }
}