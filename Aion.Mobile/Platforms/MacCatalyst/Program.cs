using ObjCRuntime;
using UIKit;

namespace Aion.Mobile;

public class Program
{
    // Point d'entrée Mac Catalyst.
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}