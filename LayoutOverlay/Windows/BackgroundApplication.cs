using System.ComponentModel;
using LayoutOverlay.Windows.Services;
using LayoutOverlay.Windows.Models;

namespace LayoutOverlay.Windows;

public class BackgroundApplication : Form
{
    private readonly ConfigurationService _configService;
    private readonly LayoutService _layoutService;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly OverlayWindowService _overlayService;
    private readonly KeyboardHookService _keyboardHookService;
    private NotifyIcon? _notifyIcon;

    public BackgroundApplication(
        ConfigurationService configService,
        LayoutService layoutService, 
        GlobalHotkeyService hotkeyService, 
        OverlayWindowService overlayService, 
        KeyboardHookService keyboardHookService)
    {
        _configService = configService;
        _layoutService = layoutService;
        _hotkeyService = hotkeyService;
        _overlayService = overlayService;
        _keyboardHookService = keyboardHookService;
        
        InitializeHiddenForm();
        SetupSystemTray();
        SetupServices();
    }

    private void InitializeHiddenForm()
    {
        // Create completely hidden form for message handling
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;
        this.Size = new Size(1, 1);
        this.FormBorderStyle = FormBorderStyle.None;
    }

    private void SetupSystemTray()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Layout Overlay - Running in background",
            Visible = true
        };

        // Create a simple icon programmatically
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.FillRectangle(Brushes.Orange, 2, 2, 12, 12);
            g.DrawRectangle(Pens.White, 2, 2, 12, 12);
        }
        _notifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Toggle Overlay (F24)", null, (s, e) => _layoutService.ToggleOverlay());
        contextMenu.Items.Add("Switch to QWERTY (F13+Q)", null, (s, e) => _layoutService.SetActiveLayout(LayoutType.Qwerty));
        contextMenu.Items.Add("Switch to Custom Split (F13+X)", null, (s, e) => _layoutService.SetActiveLayout(LayoutType.Custom));
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Reload Configuration", null, (s, e) => ReloadConfiguration());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => _layoutService.ToggleOverlay();
    }

    private void SetupServices()
    {
        // Apply configuration to services
        ApplyConfiguration();

        // Setup global hotkeys
        _hotkeyService.Initialize(this.Handle);
        
        // Setup keyboard hook for key highlighting
        if (_configService.Configuration.EnableKeyHighlighting)
        {
            _keyboardHookService.KeyPressed += OnKeyPressed;
            _keyboardHookService.KeyReleased += OnKeyReleased;
            _keyboardHookService.StartHook();
        }
        
        // Show overlay if configured to be visible
        if (_layoutService.IsOverlayVisible)
        {
            _overlayService.ShowOverlay();
        }

        Console.WriteLine("Background application started successfully");
        Console.WriteLine($"Configuration file: layout-overlay-config.json");
        Console.WriteLine("Global hotkeys: F24 (toggle), F13+Q (QWERTY), F13+X (Custom)");
    }

    private void ApplyConfiguration()
    {
        var config = _configService.Configuration;

        // Apply transparency settings
        _layoutService.SetBackgroundTransparency(config.BackgroundTransparency);
        _layoutService.SetKeyTransparency(config.KeyTransparency);

        // Set active layout
        var layoutType = config.ActiveLayout.ToLower() switch
        {
            "custom" => LayoutType.Custom,
            _ => LayoutType.Qwerty
        };
        _layoutService.SetActiveLayout(layoutType);

        // Set overlay visibility
        if (config.IsOverlayVisible != _layoutService.IsOverlayVisible)
        {
            _layoutService.ToggleOverlay();
        }
    }

    private void ReloadConfiguration()
    {
        var newConfig = _configService.LoadConfiguration();
        ApplyConfiguration();
        
        _notifyIcon!.ShowBalloonTip(2000, "Configuration Reloaded", 
            "Layout Overlay configuration has been reloaded from file.", ToolTipIcon.Info);
    }

    private void OnKeyPressed(object? sender, KeyPressedEventArgs e)
    {
        if (_configService.Configuration.EnableKeyHighlighting)
        {
            _layoutService.HandleKeyPressed(e.Key);
        }
    }

    private void OnKeyReleased(object? sender, KeyReleasedEventArgs e)
    {
        if (_configService.Configuration.EnableKeyHighlighting)
        {
            _layoutService.HandleKeyReleased(e.Key);
        }
    }

    protected override void SetVisibleCore(bool value)
    {
        // Keep form completely hidden
        base.SetVisibleCore(false);
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_HOTKEY = 0x0312;
        
        if (m.Msg == WM_HOTKEY)
        {
            var hotkeyId = m.WParam.ToInt32();
            _hotkeyService.ProcessHotkey(hotkeyId);
        }

        base.WndProc(ref m);
    }

    private void ExitApplication()
    {
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // Hide to system tray instead of closing
            e.Cancel = true;
            this.Hide();
            return;
        }

        // Cleanup resources
        _hotkeyService.Dispose();
        _overlayService.Dispose();
        _keyboardHookService.Dispose();
        _notifyIcon?.Dispose();
        
        base.OnFormClosing(e);
    }
}