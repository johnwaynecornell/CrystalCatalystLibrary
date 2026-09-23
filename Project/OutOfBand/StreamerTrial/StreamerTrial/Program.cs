// See https://aka.ms/new-console-template for more information
using CrystalCatalystLibrary.net;
using StreamerTrial;

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
