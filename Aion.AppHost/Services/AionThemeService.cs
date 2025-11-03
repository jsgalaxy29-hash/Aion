using Microsoft.FluentUI.AspNetCore.Components;

namespace Aion.AppHost.Services
{
    public interface IAionThemeService
    {
        FluentDesignTheme Current { get; }
        event Action? ThemeChanged;
        void UseLight();
        void UseDark();
        /// <summary>
        /// Définit la couleur d'accent (hex, rgb/rgba, etc.). Optionnellement la base neutre.
        /// </summary>
        void SetAccent(string accentCssColor, string? neutralBaseCssColor = null);
    }

    public class AionThemeService : IAionThemeService
    {
        // _currentTheme stocke l'instance actuelle du thème. Nous la rendons réassignable.
        private FluentDesignTheme _currentTheme;

        // Current retourne l'instance actuelle.
        public FluentDesignTheme Current => _currentTheme;

        public event Action? ThemeChanged;

        public AionThemeService()
        {
            // Initialisation : on commence avec un nouveau thème clair
            _currentTheme = CreateNewTheme(DesignThemeModes.Light);
        }

        public void UseLight()
        {
            // IMPORTANT : Créer une NOUVELLE instance de thème
            _currentTheme = CreateNewTheme(DesignThemeModes.Light, _currentTheme.CustomColor, _currentTheme.NeutralBaseColor);
            OnThemeChanged();
        }

        public void UseDark()
        {
            // IMPORTANT : Créer une NOUVELLE instance de thème
            _currentTheme = CreateNewTheme(DesignThemeModes.Dark, _currentTheme.CustomColor, _currentTheme.NeutralBaseColor);
            OnThemeChanged();
        }

        public void SetAccent(string accentCssColor, string? neutralBaseCssColor = null)
        {
            // C'EST CETTE LIGNE QUI EST CRUCIALE POUR LE REFRESH DE L'ACCENT.
            // Elle remplace l'ancien objet _currentTheme par un NOUVEAU.
            _currentTheme = CreateNewTheme(
                _currentTheme.Mode, // Conserver le mode (Light/Dark)
                accentCssColor,
                neutralBaseCssColor ?? _currentTheme.NeutralBaseColor
            );

            // Si la méthode était appelée sans changement d'accent, elle sera inutilement appelée, 
            // mais elle force la mise à jour pour le changement réel.
            OnThemeChanged();
        }

        // Méthode utilitaire pour créer une nouvelle instance de thème, assurant une nouvelle référence.
        private static FluentDesignTheme CreateNewTheme(DesignThemeModes mode, string? customColor = null, string? neutralBaseColor = null)
            => new()
            {
                Mode = mode,
                CustomColor = customColor,
                NeutralBaseColor = neutralBaseColor,
                // Les autres propriétés (comme OfficeColor) peuvent être ajoutées ici si elles sont utilisées
            };

        private void OnThemeChanged() => ThemeChanged?.Invoke();

        // Suppression des champs _light, _dark et de la méthode ApplyBaseTheme, car ils ne sont plus nécessaires avec cette approche.
    }
}