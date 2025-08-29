using LayoutOverlay.Windows;
using LayoutOverlay.Windows.Services;

namespace LayoutOverlay.Windows;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        Console.WriteLine("Starting Layout Overlay...");

        // Initialize configuration service first
        var configService = new ConfigurationService();
        
        // Initialize services with configuration
        var layoutService = new LayoutService(configService);
        var hotkeyService = new GlobalHotkeyService(layoutService);
        var overlayService = new OverlayWindowService(layoutService);
        var keyboardHookService = new KeyboardHookService();

        // Create background application (no visible UI)
        var backgroundApp = new BackgroundApplication(configService, layoutService, hotkeyService, overlayService, keyboardHookService);

        Console.WriteLine("Layout Overlay is running in the background.");
        Console.WriteLine("Edit 'layout-overlay-config.json' to change settings.");
        Console.WriteLine("Right-click the system tray icon to access options.");

        Application.Run(backgroundApp);
    }
}