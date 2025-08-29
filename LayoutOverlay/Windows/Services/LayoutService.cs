using System.Drawing;
using System.Windows.Forms;
using LayoutOverlay.Windows.Models;

namespace LayoutOverlay.Windows.Services;

public class LayoutService
{
    private readonly Dictionary<LayoutType, KeyboardLayout> _layouts = new();
    private LayoutType _activeLayout = LayoutType.Qwerty;
    private bool _isOverlayVisible = true; // Start visible for debugging
    private int _backgroundTransparency = 204; // 0-255, default 80% (204)
    private int _keyTransparency = 204; // 0-255, default 80% (204)
    private readonly HashSet<Keys> _pressedKeys = new();
    private readonly Dictionary<Keys, KeyInfo> _keyMapping = new();

    public event EventHandler<LayoutChangedEventArgs>? LayoutChanged;
    public event EventHandler<OverlayVisibilityChangedEventArgs>? OverlayVisibilityChanged;
    public event EventHandler<TransparencyChangedEventArgs>? TransparencyChanged;
    public event EventHandler<KeyHighlightChangedEventArgs>? KeyHighlightChanged;

    private readonly ConfigurationService? _configService;

    public LayoutService(ConfigurationService? configService = null)
    {
        _configService = configService;
        
        // Load settings from configuration if available
        if (_configService?.Configuration != null)
        {
            var config = _configService.Configuration;
            _isOverlayVisible = config.IsOverlayVisible;
            _backgroundTransparency = config.BackgroundTransparency;
            _keyTransparency = config.KeyTransparency;
            
            _activeLayout = config.ActiveLayout.ToLower() switch
            {
                "custom" => LayoutType.Custom,
                _ => LayoutType.Qwerty
            };
        }
        
        InitializeLayouts();
        InitializeKeyMapping();
    }

    public KeyboardLayout GetActiveLayout() => _layouts[_activeLayout];
    public LayoutType ActiveLayoutType => _activeLayout;
    public bool IsOverlayVisible => _isOverlayVisible;
    public int BackgroundTransparency => _backgroundTransparency;
    public int KeyTransparency => _keyTransparency;
    public Dictionary<LayoutType, KeyboardLayout> GetAllLayouts() => _layouts;
    public bool IsKeyPressed(KeyInfo keyInfo) => _keyMapping.ContainsValue(keyInfo) && 
        _keyMapping.Where(kvp => kvp.Value == keyInfo).Any(kvp => _pressedKeys.Contains(kvp.Key));

    // Configuration-based properties
    public bool ShowOverlayBackground => _configService?.Configuration?.ShowOverlayBackground ?? false;
    public bool ShowKeyBackground => _configService?.Configuration?.ShowKeyBackground ?? false;
    public string KeyBorderColor => _configService?.Configuration?.KeyBorderColor ?? "#FF6A00";
    public string KeyBorderColorPressed => _configService?.Configuration?.KeyBorderColorPressed ?? "#FFFFFF";
    public string KeyFontColor => _configService?.Configuration?.KeyFontColor ?? "#FFFFFF";
    public string KeyFontColorPressed => _configService?.Configuration?.KeyFontColorPressed ?? "#FFFF00";
    public string ShiftFontColor => _configService?.Configuration?.ShiftFontColor ?? "#FFFFFF";

    public void SetActiveLayout(LayoutType layoutType)
    {
        if (_activeLayout != layoutType && _layouts.ContainsKey(layoutType))
        {
            _activeLayout = layoutType;
            _configService?.UpdateActiveLayout(layoutType.ToString());
            InitializeKeyMapping(); // Refresh key mapping for new layout
            LayoutChanged?.Invoke(this, new LayoutChangedEventArgs(layoutType, _layouts[layoutType]));
        }
    }

    public void ToggleOverlay()
    {
        _isOverlayVisible = !_isOverlayVisible;
        _configService?.UpdateOverlayVisibility(_isOverlayVisible);
        OverlayVisibilityChanged?.Invoke(this, new OverlayVisibilityChangedEventArgs(_isOverlayVisible));
    }

    public void SetBackgroundTransparency(int transparency)
    {
        transparency = Math.Clamp(transparency, 0, 255);
        if (_backgroundTransparency != transparency)
        {
            _backgroundTransparency = transparency;
            _configService?.UpdateBackgroundTransparency(transparency);
            TransparencyChanged?.Invoke(this, new TransparencyChangedEventArgs());
        }
    }

    public void SetKeyTransparency(int transparency)
    {
        transparency = Math.Clamp(transparency, 0, 255);
        if (_keyTransparency != transparency)
        {
            _keyTransparency = transparency;
            _configService?.UpdateKeyTransparency(transparency);
            TransparencyChanged?.Invoke(this, new TransparencyChangedEventArgs());
        }
    }

    public void HandleKeyPressed(Keys key)
    {
        if (_pressedKeys.Add(key))
        {
            KeyHighlightChanged?.Invoke(this, new KeyHighlightChangedEventArgs());
        }
    }

    public void HandleKeyReleased(Keys key)
    {
        if (_pressedKeys.Remove(key))
        {
            KeyHighlightChanged?.Invoke(this, new KeyHighlightChangedEventArgs());
        }
    }

    private void InitializeLayouts()
    {
        _layouts[LayoutType.Qwerty] = new KeyboardLayout
        {
            Name = "QWERTY",
            Description = "Standard QWERTY layout",
            Color = Color.FromArgb(59, 130, 246),
            Keys = CreateQwertyLayout()
        };

        _layouts[LayoutType.Custom] = new KeyboardLayout
        {
            Name = "Custom Split",
            Description = "Custom ergonomic split keyboard",
            Color = Color.FromArgb(255, 107, 53),
            Keys = CreateCustomLayout()
        };
    }

    private List<KeyInfo> CreateQwertyLayout()
    {
        return new List<KeyInfo>
        {
            // Top number row
            new("~", "`", 0, 0, 1.0f), new("!", "1", 1, 0, 1.0f), new("@", "2", 2, 0, 1.0f), new("#", "3", 3, 0, 1.0f),
            new("$", "4", 4, 0, 1.0f), new("%", "5", 5, 0, 1.0f), new("^", "6", 6, 0, 1.0f), new("&", "7", 7, 0, 1.0f),
            new("*", "8", 8, 0, 1.0f), new("(", "9", 9, 0, 1.0f), new(")", "0", 10, 0, 1.0f), new("_", "-", 11, 0, 1.0f),
            new("+", "=", 12, 0, 1.0f), new("BKSP", "⌫", 13, 0, 2.0f),
            
            // QWERTY row
            new("TAB", "⇥", 0, 1, 1.5f), new("Q", "q", 1.5f, 1, 1.0f), new("W", "w", 2.5f, 1, 1.0f), new("E", "e", 3.5f, 1, 1.0f),
            new("R", "r", 4.5f, 1, 1.0f), new("T", "t", 5.5f, 1, 1.0f), new("Y", "y", 6.5f, 1, 1.0f), new("U", "u", 7.5f, 1, 1.0f),
            new("I", "i", 8.5f, 1, 1.0f), new("O", "o", 9.5f, 1, 1.0f), new("P", "p", 10.5f, 1, 1.0f), new("{", "[", 11.5f, 1, 1.0f),
            new("}", "]", 12.5f, 1, 1.0f), new("|", "\\", 13.5f, 1, 1.5f),
            
            // Home row
            new("CAPS", "⇪", 0, 2, 1.75f), new("A", "a", 1.75f, 2, 1.0f), new("S", "s", 2.75f, 2, 1.0f), new("D", "d", 3.75f, 2, 1.0f),
            new("F", "f", 4.75f, 2, 1.0f), new("G", "g", 5.75f, 2, 1.0f), new("H", "h", 6.75f, 2, 1.0f), new("J", "j", 7.75f, 2, 1.0f),
            new("K", "k", 8.75f, 2, 1.0f), new("L", "l", 9.75f, 2, 1.0f), new(":", ";", 10.75f, 2, 1.0f), new("\"", "'", 11.75f, 2, 1.0f),
            new("ENTER", "⏎", 12.75f, 2, 2.25f),
            
            // Bottom row
            new("SHIFT", "⇧", 0, 3, 2.25f), new("Z", "z", 2.25f, 3, 1.0f), new("X", "x", 3.25f, 3, 1.0f), new("C", "c", 4.25f, 3, 1.0f),
            new("V", "v", 5.25f, 3, 1.0f), new("B", "b", 6.25f, 3, 1.0f), new("N", "n", 7.25f, 3, 1.0f), new("M", "m", 8.25f, 3, 1.0f),
            new("<", ",", 9.25f, 3, 1.0f), new(">", ".", 10.25f, 3, 1.0f), new("?", "/", 11.25f, 3, 1.0f), new("SHIFT", "⇧", 12.25f, 3, 2.75f),
            
            // Space row
            new("CTRL", "ctrl", 0, 4, 1.25f), new("WIN", "⊞", 1.25f, 4, 1.25f), new("ALT", "alt", 2.5f, 4, 1.25f),
            new("SPACE", "␣", 3.75f, 4, 6.25f), new("ALT", "alt", 10, 4, 1.25f), new("WIN", "⊞", 11.25f, 4, 1.25f),
            new("MENU", "☰", 12.5f, 4, 1.25f), new("CTRL", "ctrl", 13.75f, 4, 1.25f)
        };
    }


    private List<KeyInfo> CreateCustomLayout()
    {
        return new List<KeyInfo>
        {
            // Top row left side (Y offset ~1)
            new("", "Esc", 0.5f, 1, 1.0f),      // Esc at leftmost
            new("Q", "q", 1.5f, 1, 1.0f),       // Q
            new("W", "w", 2.5f, 0.8f, 1.0f),    // W slightly higher
            new("E", "e", 3.5f, 0.6f, 1.0f),    // E highest
            new("R", "r", 4.5f, 0.8f, 1.0f),    // R down slightly
            new("T", "t", 5.5f, 1.0f, 1.0f),    // T down more

            // Center navigation
            new("↑", "up", 6.5f, 1.4f, 1.0f),   // Up arrow
            new("←", "left", 8.5f, 1.4f, 1.0f), // Left arrow

            // Top row right side 
            new("Y", "y", 9.5f, 1.0f, 1.0f),    // Y
            new("U", "u", 10.5f, 0.8f, 1.0f),   // U
            new("I", "i", 11.5f, 0.6f, 1.0f),   // I
            new("O", "o", 12.5f, 0.8f, 1.0f),   // O
            new("P", "p", 13.5f, 1, 1.0f),      // P
            new("", "BSpace", 14.5f, 1, 1.0f),  // Backspace

            // Middle row left side (Y offset ~2.2)
            new("", "Tab", 0.5f, 2.2f, 1.0f),   // Tab
            new("A", "a", 1.5f, 2.2f, 1.0f),    // A
            new("S", "s", 2.5f, 2.0f, 1.0f),    // S slightly higher
            new("D", "d", 3.5f, 1.8f, 1.0f),    // D higher
            new("F", "f", 4.5f, 2.0f, 1.0f),    // F down slightly
            new("G", "g", 5.5f, 2.2f, 1.0f),    // G down more

            // Center navigation
            new("↓", "down", 6.5f, 2.6f, 1.0f), // Down arrow
            new("→", "right", 8.5f, 2.6f, 1.0f), // Right arrow

            // Middle row right side
            new("H", "h", 9.5f, 2.2f, 1.0f),    // H
            new("J", "j", 10.5f, 2.0f, 1.0f),   // J
            new("K", "k", 11.5f, 1.8f, 1.0f),   // K
            new("L", "l", 12.5f, 2.0f, 1.0f),   // L
            new(":", ";", 13.5f, 2.2f, 1.0f),   // Semicolon
            new("", "Caps", 14.5f, 2.2f, 1.0f), // Caps

            // Bottom row left side (Y offset ~3.4)
            new("", "ctrl", 0.5f, 3.4f, 1.0f),  // Ctrl
            new("Z", "z", 1.5f, 3.4f, 1.0f),    // Z
            new("X", "x", 2.5f, 3.2f, 1.0f),    // X
            new("C", "c", 3.5f, 3.0f, 1.0f),    // C
            new("V", "v", 4.5f, 3.2f, 1.0f),    // V
            new("B", "b", 5.5f, 3.4f, 1.0f),    // B

            // Bottom row right side
            new("N", "n", 9.5f, 3.4f, 1.0f),    // N
            new("M", "m", 10.5f, 3.2f, 1.0f),   // M
            new("<", ",", 11.5f, 3.0f, 1.0f),   // Comma
            new(">", ".", 12.5f, 3.2f, 1.0f),   // Period
            new("?", "/", 13.5f, 3.4f, 1.0f),   // Slash
            new("", "Del", 14.5f, 3.4f, 1.0f),  // Delete

            // Thumb cluster left (Y offset ~4.5, angled)
            new("", "⇧", 4.0f, 4.5f, 1.0f),     // Shift
            new("", "⊞", 5.1f, 4.7f, 1.0f),     // Win (rotated 15°)
            new("", "␣", 6.3f, 5.0f, 1.2f),     // Space (bigger, rotated 30°)

            // Thumb cluster right (Y offset ~4.5, angled) 
            new("", "⏎", 8.7f, 5.0f, 1.2f),     // Enter (bigger, rotated -30°)
            new("", "Fn", 9.9f, 4.7f, 1.0f),    // Fn (rotated -15°)
            new("", "alt", 11.0f, 4.5f, 1.0f)   // Alt
        };
    }

    private void InitializeKeyMapping()
    {
        // Clear existing mappings
        _keyMapping.Clear();

        // Get the current active layout for mapping
        var layout = GetActiveLayout();

        // Map common keys that are consistent across layouts
        // Numbers row
        _keyMapping[Keys.Oemtilde] = layout.Keys.FirstOrDefault(k => k.BaseChar == "`") ?? layout.Keys.First();
        _keyMapping[Keys.D1] = layout.Keys.FirstOrDefault(k => k.BaseChar == "1") ?? layout.Keys.First();
        _keyMapping[Keys.D2] = layout.Keys.FirstOrDefault(k => k.BaseChar == "2") ?? layout.Keys.First();
        _keyMapping[Keys.D3] = layout.Keys.FirstOrDefault(k => k.BaseChar == "3") ?? layout.Keys.First();
        _keyMapping[Keys.D4] = layout.Keys.FirstOrDefault(k => k.BaseChar == "4") ?? layout.Keys.First();
        _keyMapping[Keys.D5] = layout.Keys.FirstOrDefault(k => k.BaseChar == "5") ?? layout.Keys.First();
        _keyMapping[Keys.D6] = layout.Keys.FirstOrDefault(k => k.BaseChar == "6") ?? layout.Keys.First();
        _keyMapping[Keys.D7] = layout.Keys.FirstOrDefault(k => k.BaseChar == "7") ?? layout.Keys.First();
        _keyMapping[Keys.D8] = layout.Keys.FirstOrDefault(k => k.BaseChar == "8") ?? layout.Keys.First();
        _keyMapping[Keys.D9] = layout.Keys.FirstOrDefault(k => k.BaseChar == "9") ?? layout.Keys.First();
        _keyMapping[Keys.D0] = layout.Keys.FirstOrDefault(k => k.BaseChar == "0") ?? layout.Keys.First();
        _keyMapping[Keys.OemMinus] = layout.Keys.FirstOrDefault(k => k.BaseChar == "-") ?? layout.Keys.First();
        _keyMapping[Keys.Oemplus] = layout.Keys.FirstOrDefault(k => k.BaseChar == "=") ?? layout.Keys.First();

        // Letter keys - these depend on the layout
        _keyMapping[Keys.A] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "a") ?? layout.Keys.First();
        _keyMapping[Keys.B] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "b") ?? layout.Keys.First();
        _keyMapping[Keys.C] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "c") ?? layout.Keys.First();
        _keyMapping[Keys.D] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "d") ?? layout.Keys.First();
        _keyMapping[Keys.E] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "e") ?? layout.Keys.First();
        _keyMapping[Keys.F] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "f") ?? layout.Keys.First();
        _keyMapping[Keys.G] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "g") ?? layout.Keys.First();
        _keyMapping[Keys.H] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "h") ?? layout.Keys.First();
        _keyMapping[Keys.I] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "i") ?? layout.Keys.First();
        _keyMapping[Keys.J] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "j") ?? layout.Keys.First();
        _keyMapping[Keys.K] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "k") ?? layout.Keys.First();
        _keyMapping[Keys.L] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "l") ?? layout.Keys.First();
        _keyMapping[Keys.M] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "m") ?? layout.Keys.First();
        _keyMapping[Keys.N] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "n") ?? layout.Keys.First();
        _keyMapping[Keys.O] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "o") ?? layout.Keys.First();
        _keyMapping[Keys.P] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "p") ?? layout.Keys.First();
        _keyMapping[Keys.Q] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "q") ?? layout.Keys.First();
        _keyMapping[Keys.R] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "r") ?? layout.Keys.First();
        _keyMapping[Keys.S] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "s") ?? layout.Keys.First();
        _keyMapping[Keys.T] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "t") ?? layout.Keys.First();
        _keyMapping[Keys.U] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "u") ?? layout.Keys.First();
        _keyMapping[Keys.V] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "v") ?? layout.Keys.First();
        _keyMapping[Keys.W] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "w") ?? layout.Keys.First();
        _keyMapping[Keys.X] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "x") ?? layout.Keys.First();
        _keyMapping[Keys.Y] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "y") ?? layout.Keys.First();
        _keyMapping[Keys.Z] = layout.Keys.FirstOrDefault(k => k.BaseChar.ToLower() == "z") ?? layout.Keys.First();

        // Common keys
        _keyMapping[Keys.Space] = layout.Keys.FirstOrDefault(k => k.BaseChar == "␣") ?? layout.Keys.First();
        _keyMapping[Keys.Tab] = layout.Keys.FirstOrDefault(k => k.BaseChar == "⇥") ?? layout.Keys.First();
        _keyMapping[Keys.Enter] = layout.Keys.FirstOrDefault(k => k.BaseChar == "⏎") ?? layout.Keys.First();
        _keyMapping[Keys.Back] = layout.Keys.FirstOrDefault(k => k.BaseChar == "⌫") ?? layout.Keys.First();
        _keyMapping[Keys.LShiftKey] = layout.Keys.FirstOrDefault(k => k.BaseChar == "⇧") ?? layout.Keys.First();
        _keyMapping[Keys.RShiftKey] = layout.Keys.FirstOrDefault(k => k.BaseChar == "⇧") ?? layout.Keys.First();
        _keyMapping[Keys.LControlKey] = layout.Keys.FirstOrDefault(k => k.BaseChar == "ctrl") ?? layout.Keys.First();
        _keyMapping[Keys.RControlKey] = layout.Keys.FirstOrDefault(k => k.BaseChar == "ctrl") ?? layout.Keys.First();
        _keyMapping[Keys.LMenu] = layout.Keys.FirstOrDefault(k => k.BaseChar == "alt") ?? layout.Keys.First();
        _keyMapping[Keys.RMenu] = layout.Keys.FirstOrDefault(k => k.BaseChar == "alt") ?? layout.Keys.First();

        // Punctuation
        _keyMapping[Keys.Oemcomma] = layout.Keys.FirstOrDefault(k => k.BaseChar == ",") ?? layout.Keys.First();
        _keyMapping[Keys.OemPeriod] = layout.Keys.FirstOrDefault(k => k.BaseChar == ".") ?? layout.Keys.First();
        _keyMapping[Keys.OemQuestion] = layout.Keys.FirstOrDefault(k => k.BaseChar == "/") ?? layout.Keys.First();
        _keyMapping[Keys.OemSemicolon] = layout.Keys.FirstOrDefault(k => k.BaseChar == ";") ?? layout.Keys.First();
        _keyMapping[Keys.OemQuotes] = layout.Keys.FirstOrDefault(k => k.BaseChar == "'") ?? layout.Keys.First();
        _keyMapping[Keys.OemOpenBrackets] = layout.Keys.FirstOrDefault(k => k.BaseChar == "[") ?? layout.Keys.First();
        _keyMapping[Keys.OemCloseBrackets] = layout.Keys.FirstOrDefault(k => k.BaseChar == "]") ?? layout.Keys.First();
        _keyMapping[Keys.OemPipe] = layout.Keys.FirstOrDefault(k => k.BaseChar == "\\") ?? layout.Keys.First();
    }
}

public class LayoutChangedEventArgs : EventArgs
{
    public LayoutType LayoutType { get; }
    public KeyboardLayout Layout { get; }

    public LayoutChangedEventArgs(LayoutType layoutType, KeyboardLayout layout)
    {
        LayoutType = layoutType;
        Layout = layout;
    }
}

public class OverlayVisibilityChangedEventArgs : EventArgs
{
    public bool IsVisible { get; }

    public OverlayVisibilityChangedEventArgs(bool isVisible)
    {
        IsVisible = isVisible;
    }
}

public class TransparencyChangedEventArgs : EventArgs
{
    public TransparencyChangedEventArgs()
    {
    }
}

public class KeyHighlightChangedEventArgs : EventArgs
{
    public KeyHighlightChangedEventArgs()
    {
    }
}