using Microsoft.Maui.Hosting;
using Microsoft.Maui;
using Microsoft.UI.Xaml;
namespace Aion.Mobile.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}