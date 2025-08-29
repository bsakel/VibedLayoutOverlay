using System.Text.Json;
using System.Text.Json.Serialization;

namespace LayoutOverlay.Windows.Models;

public class AppConfiguration
{
    [JsonPropertyName("isOverlayVisible")]
    public bool IsOverlayVisible { get; set; } = true;

    [JsonPropertyName("backgroundTransparency")]
    public int BackgroundTransparency { get; set; } = 204; // 80% opacity

    [JsonPropertyName("keyTransparency")]
    public int KeyTransparency { get; set; } = 204; // 80% opacity

    [JsonPropertyName("activeLayout")]
    public string ActiveLayout { get; set; } = "Qwerty";

    [JsonPropertyName("enableKeyHighlighting")]
    public bool EnableKeyHighlighting { get; set; } = true;

    [JsonPropertyName("showOverlayBackground")]
    public bool ShowOverlayBackground { get; set; } = false;

    [JsonPropertyName("showKeyBackground")]
    public bool ShowKeyBackground { get; set; } = false;

    [JsonPropertyName("keyBorderColor")]
    public string KeyBorderColor { get; set; } = "#FF6A00"; // Orange

    [JsonPropertyName("keyBorderColorPressed")]
    public string KeyBorderColorPressed { get; set; } = "#FFFFFF"; // White

    [JsonPropertyName("keyFontColor")]
    public string KeyFontColor { get; set; } = "#FFFFFF"; // White

    [JsonPropertyName("keyFontColorPressed")]
    public string KeyFontColorPressed { get; set; } = "#FFFF00"; // Yellow

    [JsonPropertyName("shiftFontColor")]
    public string ShiftFontColor { get; set; } = "#FFFFFF"; // White

    /// <summary>
    /// Validates and clamps configuration values to acceptable ranges
    /// </summary>
    public void Validate()
    {
        BackgroundTransparency = Math.Clamp(BackgroundTransparency, 50, 255);
        KeyTransparency = Math.Clamp(KeyTransparency, 50, 255);
        
        if (ActiveLayout != "Qwerty" && ActiveLayout != "Custom")
        {
            ActiveLayout = "Qwerty";
        }

        // Validate color strings (ensure they're valid hex colors)
        KeyBorderColor = ValidateHexColor(KeyBorderColor, "#FF6A00");
        KeyBorderColorPressed = ValidateHexColor(KeyBorderColorPressed, "#FFFFFF");
        KeyFontColor = ValidateHexColor(KeyFontColor, "#FFFFFF");
        KeyFontColorPressed = ValidateHexColor(KeyFontColorPressed, "#FFFF00");
        ShiftFontColor = ValidateHexColor(ShiftFontColor, "#FFFFFF");
    }

    private static string ValidateHexColor(string color, string defaultColor)
    {
        if (string.IsNullOrEmpty(color) || !color.StartsWith("#") || color.Length != 7)
            return defaultColor;

        try
        {
            Convert.ToInt32(color[1..], 16);
            return color;
        }
        catch
        {
            return defaultColor;
        }
    }
}