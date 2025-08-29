using System.Drawing;
using System.Runtime.InteropServices;
using LayoutOverlay.Windows.Services;
using LayoutOverlay.Windows.Models;

namespace LayoutOverlay.Windows.Forms;

public partial class OverlayForm : Form
{
    private readonly LayoutService _layoutService;
    private const int KeySize = 54; // Increased by 20% (45 * 1.2)
    private const int KeySpacing = 2;

    // Win32 API for click-through overlay
    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    const int GWL_EXSTYLE = -20;
    const uint WS_EX_LAYERED = 0x80000;
    const uint WS_EX_TRANSPARENT = 0x20;
    const uint WS_EX_TOPMOST = 0x8;

    public OverlayForm(LayoutService layoutService)
    {
        _layoutService = layoutService;
        _layoutService.TransparencyChanged += OnTransparencyChanged;
        _layoutService.KeyHighlightChanged += OnKeyHighlightChanged;
        InitializeComponent();
        SetupOverlayWindow();
    }

    private void InitializeComponent()
    {
        this.Text = "";  // Remove title
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.Magenta;  // Use magenta as transparency key (more unique)
        this.TransparencyKey = Color.Magenta;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.ShowIcon = false;
        this.WindowState = FormWindowState.Normal;
        this.Size = new Size(Screen.PrimaryScreen?.Bounds.Width ?? 1920, Screen.PrimaryScreen?.Bounds.Height ?? 1080);
        this.Location = new Point(0, 0);
        this.AllowTransparency = true;  // Enable transparency
        
        // Enable double buffering to reduce flicker
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.DoubleBuffer | 
                     ControlStyles.SupportsTransparentBackColor, true);
    }

    private void SetupOverlayWindow()
    {
        // Make window always on top with layered transparency
        if (this.Handle != IntPtr.Zero)
        {
            uint extendedStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, 
                extendedStyle | WS_EX_LAYERED | WS_EX_TOPMOST);
            // Note: WS_EX_TRANSPARENT removed to allow overlay content to be visible
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        SetupOverlayWindow();  // Ensure window setup after handle creation
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Clear with transparency key color first
        e.Graphics.Clear(Color.Magenta);
        
        if (!_layoutService.IsOverlayVisible)
            return;

        DrawKeyboardOverlay(e.Graphics);
    }

    private void DrawKeyboardOverlay(Graphics g)
    {
        var layout = _layoutService.GetActiveLayout();
        
        // Calculate overlay position (bottom half of screen, centered horizontally)
        var screenBounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        var overlayWidth = 15 * (KeySize + KeySpacing);
        var overlayHeight = 5 * (KeySize + KeySpacing) + 100; // Extra space for header
        
        var startX = (screenBounds.Width - overlayWidth) / 2;
        var startY = screenBounds.Height / 2 + 50; // Bottom half, with some margin from center

        // Draw background with configurable transparency (if enabled)
        if (_layoutService.ShowOverlayBackground)
        {
            using var backgroundBrush = new SolidBrush(Color.FromArgb(_layoutService.BackgroundTransparency, 30, 30, 30));
            g.FillRoundedRectangle(backgroundBrush, startX - 24, startY - 72, overlayWidth + 48, overlayHeight + 24, 12); // 20% bigger padding
        }

        // Draw header (if background is enabled)
        if (_layoutService.ShowOverlayBackground)
        {
            DrawHeader(g, layout, startX, startY - 50, overlayWidth);
        }

        // Draw keyboard keys
        foreach (var key in layout.Keys)
        {
            DrawKey(g, key, startX, startY, layout.Color);
        }
    }

    private void DrawHeader(Graphics g, KeyboardLayout layout, int x, int y, int width)
    {
        using var headerFont = new Font("Segoe UI", 19, FontStyle.Bold); // 20% bigger (16 * 1.2)
        using var descFont = new Font("Segoe UI", 12); // 20% bigger (10 * 1.2)
        using var textBrush = new SolidBrush(Color.White);
        using var colorBrush = new SolidBrush(layout.Color);

        // Draw layout indicator dot (20% bigger)
        g.FillEllipse(colorBrush, x, y + 6, 24, 24);

        // Draw layout name
        g.DrawString(layout.Name, headerFont, textBrush, x + 36, y);

        // Draw description
        g.DrawString(layout.Description, descFont, textBrush, x + 36, y + 30);
    }

    private void DrawKey(Graphics g, KeyInfo key, int startX, int startY, Color themeColor)
    {
        // For Custom layout, center the keys by subtracting offset
        var layout = _layoutService.GetActiveLayout();
        var xOffset = layout.Name == "Custom Split" ? -2.0f : 0.0f;
        
        var keyX = startX + ((key.X + xOffset) * (KeySize + KeySpacing));
        var keyY = startY + (key.Y * (KeySize + KeySpacing));
        var keyWidth = (int)(key.Width * KeySize + (key.Width - 1) * KeySpacing);
        var keyHeight = KeySize;

        // Check if key is pressed for highlighting
        bool isPressed = _layoutService.IsKeyPressed(key);
        
        // Key background with configurable transparency and highlighting (if enabled)
        if (_layoutService.ShowKeyBackground)
        {
            Color keyBackgroundColor = isPressed 
                ? Color.FromArgb(_layoutService.KeyTransparency, Math.Min(255, themeColor.R + 40), Math.Min(255, themeColor.G + 40), Math.Min(255, themeColor.B + 40))
                : Color.FromArgb(_layoutService.KeyTransparency, 60, 60, 60);

            using var keyBrush = new SolidBrush(keyBackgroundColor);
            g.FillRoundedRectangle(keyBrush, keyX, keyY, keyWidth, keyHeight, 7); // Slightly bigger radius
        }

        // Configurable border colors
        var borderColor = isPressed ? HexToColor(_layoutService.KeyBorderColorPressed) : HexToColor(_layoutService.KeyBorderColor);
        using var borderPen = new Pen(borderColor, isPressed ? 3 : 2);
        g.DrawRoundedRectangle(borderPen, keyX, keyY, keyWidth, keyHeight, 7);

        // Key text (20% bigger fonts) with configurable colors
        using var keyFont = new Font("Segoe UI", 12, isPressed ? FontStyle.Bold : FontStyle.Bold); 
        using var shiftFont = new Font("Segoe UI", 10, isPressed ? FontStyle.Bold : FontStyle.Regular);
        
        var fontColor = isPressed ? HexToColor(_layoutService.KeyFontColorPressed) : HexToColor(_layoutService.KeyFontColor);
        var shiftColor = HexToColor(_layoutService.ShiftFontColor);
        
        using var textBrush = new SolidBrush(fontColor);
        using var shiftBrush = new SolidBrush(shiftColor);

        // Draw shifted character (top)
        var shiftRect = new RectangleF(keyX + 2, keyY + 2, keyWidth - 4, keyHeight / 2 - 2);
        g.DrawString(key.ShiftedChar, shiftFont, shiftBrush, shiftRect, 
            new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near });

        // Draw base character (bottom)
        var baseRect = new RectangleF(keyX + 2, keyY + keyHeight / 2, keyWidth - 4, keyHeight / 2 - 2);
        g.DrawString(key.BaseChar, keyFont, textBrush, baseRect, 
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }

    public void RefreshLayout()
    {
        this.Invalidate();
    }

    private void OnTransparencyChanged(object? sender, TransparencyChangedEventArgs e)
    {
        this.Invalidate();
    }

    private void OnKeyHighlightChanged(object? sender, KeyHighlightChangedEventArgs e)
    {
        this.Invalidate();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _layoutService.TransparencyChanged -= OnTransparencyChanged;
        _layoutService.KeyHighlightChanged -= OnKeyHighlightChanged;
        base.OnFormClosed(e);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x80000; // WS_EX_LAYERED only - removed WS_EX_TRANSPARENT to allow drawing
            return cp;
        }
    }

    private static Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length != 7)
            return Color.White; // Default fallback

        try
        {
            return ColorTranslator.FromHtml(hex);
        }
        catch
        {
            return Color.White; // Default fallback
        }
    }
}

// Extension method for rounded rectangles
public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    {
        using var path = CreateRoundedRectPath(x, y, width, height, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, float x, float y, float width, float height, float radius)
    {
        using var path = CreateRoundedRectPath(x, y, width, height, radius);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(float x, float y, float width, float height, float radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        var rect = new RectangleF(x, y, width, height);
        
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        var arc = new RectangleF(x, y, diameter, diameter);
        path.AddArc(arc, 180, 90);

        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);

        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}