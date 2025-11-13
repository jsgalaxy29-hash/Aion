using Microsoft.FluentUI.AspNetCore.Components;

namespace Aion.AppHost.Services
{
    public interface IAionThemeService
    {
        FluentDesignTheme Current { get; }
        DesignThemeModes CurrentMode { get; }
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
        private const string DefaultAionAccentColor = "#EFBF04";
        private const string DefaultAionNeutralColor = "#F7DF82"; // mélange de #EFBF04 et blanc

        // _currentTheme stocke l'instance actuelle du thème. Nous la rendons réassignable.
        private FluentDesignTheme _currentTheme;
        private DesignThemeModes _currentThemeMode;

        // Current retourne l'instance actuelle.
        public FluentDesignTheme Current => _currentTheme;

        public DesignThemeModes CurrentMode => _currentThemeMode;

        public event Action? ThemeChanged;

        public AionThemeService()
        {
            // Initialisation : on commence avec le thème Aion clair (accent doré + neutre doux)
            _currentTheme = CreateNewTheme(DesignThemeModes.Light);
            _currentThemeMode = _currentTheme.Mode;
        }

        public void UseLight()
        {
            // IMPORTANT : Créer une NOUVELLE instance de thème
            _currentTheme = CreateNewTheme(DesignThemeModes.Light, _currentTheme.CustomColor, _currentTheme.NeutralBaseColor);
            _currentThemeMode = _currentTheme.Mode;
            OnThemeChanged();
        }

        public void UseDark()
        {
            // IMPORTANT : Créer une NOUVELLE instance de thème
            _currentTheme = CreateNewTheme(DesignThemeModes.Dark, _currentTheme.CustomColor, _currentTheme.NeutralBaseColor);
            _currentThemeMode = _currentTheme.Mode;
            OnThemeChanged();
        }

        public void SetAccent(string accentCssColor, string? neutralBaseCssColor = null)
        {
            // C'EST CETTE LIGNE QUI EST CRUCIALE POUR LE REFRESH DE L'ACCENT.
            // Elle remplace l'ancien objet _currentTheme par un NOUVEAU.
            _currentTheme = CreateNewTheme(
                _currentTheme.Mode, // Conserver le mode (Light/Dark)
                string.IsNullOrWhiteSpace(accentCssColor) ? DefaultAionAccentColor : accentCssColor,
                neutralBaseCssColor ?? _currentTheme.NeutralBaseColor ?? DefaultAionNeutralColor
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
                CustomColor = customColor ?? DefaultAionAccentColor,
                NeutralBaseColor = neutralBaseColor ?? DefaultAionNeutralColor,
                OfficeColor = customColor ?? DefaultAionAccentColor,
            };

        private void OnThemeChanged() => ThemeChanged?.Invoke();

        // Suppression des champs _light, _dark et de la méthode ApplyBaseTheme, car ils ne sont plus nécessaires avec cette approche.
    }
}