using System.Text.Json;
using LayoutOverlay.Windows.Models;

namespace LayoutOverlay.Windows.Services;

public class ConfigurationService
{
    private const string ConfigFileName = "layout-overlay-config.json";
    private readonly string _configFilePath;
    private AppConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService()
    {
        // Store config file in the same directory as the executable
        var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) 
                          ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configFilePath = Path.Combine(appDirectory, ConfigFileName);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _configuration = LoadConfiguration();
    }

    public AppConfiguration Configuration => _configuration;

    public AppConfiguration LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
                if (config != null)
                {
                    config.Validate();
                    Console.WriteLine($"Configuration loaded from: {_configFilePath}");
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
        }

        // Return default configuration and save it
        var defaultConfig = new AppConfiguration();
        defaultConfig.Validate();
        SaveConfiguration(defaultConfig);
        Console.WriteLine($"Default configuration created at: {_configFilePath}");
        return defaultConfig;
    }

    public void SaveConfiguration(AppConfiguration configuration)
    {
        try
        {
            configuration.Validate();
            var json = JsonSerializer.Serialize(configuration, _jsonOptions);
            File.WriteAllText(_configFilePath, json);
            _configuration = configuration;
            Console.WriteLine($"Configuration saved to: {_configFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }

    public void UpdateBackgroundTransparency(int transparency)
    {
        _configuration.BackgroundTransparency = Math.Clamp(transparency, 50, 255);
        SaveConfiguration(_configuration);
    }

    public void UpdateKeyTransparency(int transparency)
    {
        _configuration.KeyTransparency = Math.Clamp(transparency, 50, 255);
        SaveConfiguration(_configuration);
    }

    public void UpdateActiveLayout(string layoutName)
    {
        if (layoutName == "Qwerty" || layoutName == "Custom")
        {
            _configuration.ActiveLayout = layoutName;
            SaveConfiguration(_configuration);
        }
    }

    public void UpdateOverlayVisibility(bool isVisible)
    {
        _configuration.IsOverlayVisible = isVisible;
        SaveConfiguration(_configuration);
    }

    public void UpdateKeyHighlighting(bool enabled)
    {
        _configuration.EnableKeyHighlighting = enabled;
        SaveConfiguration(_configuration);
    }
}