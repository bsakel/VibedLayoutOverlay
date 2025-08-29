using System.Drawing;

namespace LayoutOverlay.Windows.Models;

public class KeyboardLayout
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<KeyInfo> Keys { get; set; } = new();
    public Color Color { get; set; } = Color.Blue;
}

public record KeyInfo(string ShiftedChar, string BaseChar, float X, float Y, float Width = 1.0f);

public enum LayoutType
{
    Qwerty,
    Custom
}