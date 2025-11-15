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
        // Thème Dark : Noir brillant (#000000) + Or (#FFD700)
        private const string DarkAccentColor = "#FFD700"; // Or
        private const string DarkNeutralColor = "#000000"; // Noir brillant

        // Thème Light : Blanc (#FFFFFF) + Or (#FFD700)
        private const string LightAccentColor = "#FFD700"; // Or
        private const string LightNeutralColor = "#FFFFFF"; // Blanc

        private FluentDesignTheme _currentTheme;
        private DesignThemeModes _currentThemeMode;

        public FluentDesignTheme Current => _currentTheme;
        public DesignThemeModes CurrentMode => _currentThemeMode;

        public event Action? ThemeChanged;

        public AionThemeService()
        {
            // Initialisation : on commence désormais sur le thème sombre, demandé par défaut.
            _currentTheme = CreateNewTheme(DesignThemeModes.Dark);
            _currentThemeMode = _currentTheme.Mode;
        }

        public void UseLight()
        {
            _currentTheme = CreateNewTheme(DesignThemeModes.Light, LightAccentColor, LightNeutralColor);
            _currentThemeMode = _currentTheme.Mode;
            OnThemeChanged();
        }

        public void UseDark()
        {
            _currentTheme = CreateNewTheme(DesignThemeModes.Dark, DarkAccentColor, DarkNeutralColor);
            _currentThemeMode = _currentTheme.Mode;
            OnThemeChanged();
        }

        public void SetAccent(string accentCssColor, string? neutralBaseCssColor = null)
        {
            _currentTheme = CreateNewTheme(
                _currentTheme.Mode,
                string.IsNullOrWhiteSpace(accentCssColor) ? DarkAccentColor : accentCssColor,
                neutralBaseCssColor ?? _currentTheme.NeutralBaseColor ?? DarkNeutralColor
            );

            OnThemeChanged();
        }

        private static FluentDesignTheme CreateNewTheme(DesignThemeModes mode, string? customColor = null, string? neutralBaseColor = null)
        {
            if (mode == DesignThemeModes.Dark)
            {
                return new FluentDesignTheme
                {
                    Mode = mode,
                    CustomColor = customColor ?? DarkAccentColor,
                    NeutralBaseColor = neutralBaseColor ?? DarkNeutralColor,
                    OfficeColor = null,
                };
            }
            else
            {
                return new FluentDesignTheme
                {
                    Mode = mode,
                    CustomColor = customColor ?? LightAccentColor,
                    NeutralBaseColor = neutralBaseColor ?? LightNeutralColor,
                    OfficeColor = null,
                };
            }
        }

        private void OnThemeChanged() => ThemeChanged?.Invoke();
    }
}