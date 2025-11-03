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
        private readonly FluentDesignTheme _light = new() { Mode = DesignThemeModes.Light };
        private readonly FluentDesignTheme _dark = new() { Mode = DesignThemeModes.Dark };
        private readonly FluentDesignTheme _current = new();

        public FluentDesignTheme Current => _current;

        public event Action? ThemeChanged;

        public AionThemeService()
        {
            ApplyBaseTheme(_light);
        }

        public void UseLight()
        {
            ApplyBaseTheme(_light);
        }

        public void UseDark()
        {
            ApplyBaseTheme(_dark);
        }

        public void SetAccent(string accentCssColor, string? neutralBaseCssColor = null)
        {
            var hasChanges = false;

            if (!string.Equals(_current.CustomColor, accentCssColor, StringComparison.OrdinalIgnoreCase))
            {
                _current.CustomColor = accentCssColor; // ex: "#0f6cbd" ou "rgb(15,108,189)"
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(neutralBaseCssColor) &&
                !string.Equals(_current.NeutralBaseColor, neutralBaseCssColor, StringComparison.OrdinalIgnoreCase))
            {
                _current.NeutralBaseColor = neutralBaseCssColor; // optionnel
                hasChanges = true;
            }

            if (hasChanges)
            {
                OnThemeChanged();
            }
        }

        private void ApplyBaseTheme(FluentDesignTheme baseTheme)
        {
            var hasChanges = false;

            if (_current.Mode != baseTheme.Mode)
            {
                _current.Mode = baseTheme.Mode;
                hasChanges = true;
            }

            if (!string.Equals(_current.CustomColor, baseTheme.CustomColor, StringComparison.OrdinalIgnoreCase))
            {
                _current.CustomColor = baseTheme.CustomColor;
                hasChanges = true;
            }

            if (!string.Equals(_current.NeutralBaseColor, baseTheme.NeutralBaseColor, StringComparison.OrdinalIgnoreCase))
            {
                _current.NeutralBaseColor = baseTheme.NeutralBaseColor;
                hasChanges = true;
            }

            if (!string.Equals(_current.OfficeColor, baseTheme.OfficeColor, StringComparison.OrdinalIgnoreCase))
            {
                _current.OfficeColor = baseTheme.OfficeColor;
                hasChanges = true;
            }

            if (!string.Equals(_current.StorageName, baseTheme.StorageName, StringComparison.Ordinal))
            {
                _current.StorageName = baseTheme.StorageName;
                hasChanges = true;
            }

            if (hasChanges)
            {
                OnThemeChanged();
            }
        }

        private void OnThemeChanged() => ThemeChanged?.Invoke();
    }
}
