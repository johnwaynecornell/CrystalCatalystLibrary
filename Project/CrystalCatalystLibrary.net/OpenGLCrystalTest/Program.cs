using CrystalCatalystLibrary.net;

namespace OpenGLCrystalTest
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