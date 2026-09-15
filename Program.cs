namespace EclatPlus;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        using var vibrance = new DriverVibrance();
        Application.ApplicationExit += (_, _) => Magnification.Shutdown();

        var startInTray = args.Any(a => string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(vibrance, startInTray));
    }
}
