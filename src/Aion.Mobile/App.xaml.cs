using Microsoft.Maui.Controls;
namespace Aion.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // La navigation MAUI démarre sur le Shell configuré pour exposer la page Home.
        MainPage = new AppShell();
    }
}