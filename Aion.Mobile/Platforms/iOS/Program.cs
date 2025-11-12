using ObjCRuntime;
using UIKit;

namespace Aion.Mobile;

public class Program
{
    // Classe principale iOS, point d'entrée requis par MAUI.
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}