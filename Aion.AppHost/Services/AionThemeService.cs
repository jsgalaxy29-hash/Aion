using Microsoft.FluentUI.AspNetCore.Components;

namespace Aion.AppHost.Services
{
    public interface IAionThemeService
    {
        FluentDesignTheme Current { get; }
        void UseLight();
        void UseDark();
        /// <summary>
        /// D�finit la couleur d�accent (hex, rgb/rgba, etc.). Optionnellement la base neutre.
        /// </summary>
        void SetAccent(string accentCssColor, string? neutralBaseCssColor = null);
    }

    public class AionThemeService : IAionThemeService
    {
        private readonly FluentDesignTheme _light = new() { Mode = DesignThemeModes.Light };
        private readonly FluentDesignTheme _dark = new() { Mode = DesignThemeModes.Dark };

        public FluentDesignTheme Current { get; private set; }

        public AionThemeService()
        {
            Current = CloneFrom(_light);
        }

        public void UseLight() => Current = CloneFrom(_light);
        public void UseDark() => Current = CloneFrom(_dark);

        public void SetAccent(string accentCssColor, string? neutralBaseCssColor = null)
        {
            // En v4.x, on utilise CustomColor / NeutralBaseColor (pas Primary/Secondary).
            Current.CustomColor = accentCssColor;          // ex: "#0f6cbd" ou "rgb(15,108,189)"
            if (!string.IsNullOrWhiteSpace(neutralBaseCssColor))
                Current.NeutralBaseColor = neutralBaseCssColor; // optionnel
        }

        private static FluentDesignTheme CloneFrom(FluentDesignTheme baseTheme)
            => new()
            {
                Mode = baseTheme.Mode,
                CustomColor = baseTheme.CustomColor,
                NeutralBaseColor = baseTheme.NeutralBaseColor,
                OfficeColor = baseTheme.OfficeColor,
                StorageName = baseTheme.StorageName
            };
    }
}
