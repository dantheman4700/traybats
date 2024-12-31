using Microsoft.Win32;
using System;
using System.IO;

namespace LGSTrayUI;

public class UserSettingsWrapper
{
    private const string AutoStartRegKeyValue = "LGSTrayBattery";
    private bool _autoStart;

    public UserSettingsWrapper()
    {
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
}
