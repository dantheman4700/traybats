using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.IO;
using System.Collections.Generic;

namespace LGSTrayUI;

public class UserSettingsWrapper
{
    private const string AutoStartRegKeyValue = "LGSTrayBattery";
    private bool _autoStart;
    private readonly IConfiguration _configuration;

    public UserSettingsWrapper(IConfiguration configuration)
    {
        _configuration = configuration;
        using var registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
        _autoStart = registryKey?.GetValue(AutoStartRegKeyValue) != null;
    }

    public bool AutoStart
    {
        get => _autoStart;
        set
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (value)
            {
                registryKey?.SetValue(AutoStartRegKeyValue, Path.Combine(AppContext.BaseDirectory, Environment.ProcessPath!));
            }
            else
            {
                registryKey?.DeleteValue(AutoStartRegKeyValue, false);
            }
            _autoStart = value;
        }
    }

    public bool NumericDisplay
    {
        get => _configuration.GetValue<bool>("UI:numericDisplay");
        set
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.toml");
            var lines = File.ReadAllLines(filePath);
            var found = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("numericDisplay"))
                {
                    lines[i] = $"numericDisplay = {value.ToString().ToLower()}";
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                var uiSection = Array.FindIndex(lines, l => l.Trim() == "[UI]");
                if (uiSection >= 0)
                {
                    var newLines = new List<string>(lines);
                    newLines.Insert(uiSection + 1, $"numericDisplay = {value.ToString().ToLower()}");
                    lines = newLines.ToArray();
                }
            }
            File.WriteAllLines(filePath, lines);
        }
    }

    public List<string> SelectedDevices
    {
        get => _configuration.GetSection("UserSettings:selectedDevices").Get<List<string>>() ?? new List<string>();
        set
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.toml");
            var lines = File.ReadAllLines(filePath);
            var found = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("selectedDevices"))
                {
                    lines[i] = $"selectedDevices = [{string.Join(", ", value.Select(d => $"\"{d}\""))}]";
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                var section = Array.FindIndex(lines, l => l.Trim() == "[UserSettings]");
                if (section >= 0)
                {
                    var newLines = new List<string>(lines);
                    newLines.Insert(section + 1, $"selectedDevices = [{string.Join(", ", value.Select(d => $"\"{d}\""))}]");
                    lines = newLines.ToArray();
                }
            }
            File.WriteAllLines(filePath, lines);
        }
    }
}
