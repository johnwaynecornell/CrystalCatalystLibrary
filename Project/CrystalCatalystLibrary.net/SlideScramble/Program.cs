using CrystalCatalystLibrary.net;
using SlideScramble;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.Init(args);

        Window wnd = new Window();

        wnd.Show(true);
        Application.SetDiagnosticsCallback((msg) => { });
        Application.Run();
    }
}
