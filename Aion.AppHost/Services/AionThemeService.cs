using Microsoft.FluentUI.AspNetCore.Components;

namespace Aion.AppHost.Services
{
    public interface IAionThemeService
    {
        FluentDesignTheme Current { get; }
        void UseLight();
        void UseDark();
        void SetAccent(string primaryHex, string? secondaryHex = null);
    }

    public class AionThemeService : IAionThemeService
    {
        private readonly FluentDesignTheme  _light = new FluentDesignTheme();
        private readonly FluentDesignTheme _dark  = new FluentDesignTheme();

        public FluentDesignTheme Current { get; private set; }

        public AionThemeService()
        {
            _light.Mode = DesignThemeModes.Light;
            _dark.Mode = DesignThemeModes.Dark;

            Current = CreateAionTheme(_light);
        }

        public void UseLight() => Current = CreateAionTheme(_light);
        public void UseDark()  => Current = CreateAionTheme(_dark);

        public void SetAccent(string primaryHex, string? secondaryHex = null)
        {
            //Current.PrimaryColor = primaryHex;
            //if (!string.IsNullOrWhiteSpace(secondaryHex))
            //    Current.SecondaryColor = secondaryHex;
            throw new NotImplementedException();
        }

        private FluentDesignTheme CreateAionTheme(FluentDesignTheme baseTheme)
        {
            var theme = new FluentDesignTheme
            {
                Mode= DesignThemeModes.System,
            };
            return theme;
        }
    }
}
