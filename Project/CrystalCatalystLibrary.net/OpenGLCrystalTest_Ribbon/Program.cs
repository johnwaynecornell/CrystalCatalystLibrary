using CrystalCatalystLibrary.net;

namespace OpenGLCrystalTest_Ribbon
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Init(args);
            
            var window = new Window();
            window.Run();
        }
    }
}
