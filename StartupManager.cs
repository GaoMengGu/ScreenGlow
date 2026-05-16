using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenGlow
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ScreenGlow";
        private const string StartupScriptName = "ScreenGlow Startup.cmd";
        private const string LegacyShortcutName = "ScreenGlow.lnk";

        public static bool IsEnabled()
        {
            return !string.IsNullOrWhiteSpace(GetRegisteredCommand()) ||
                   File.Exists(GetStartupScriptPath()) ||
                   File.Exists(GetLegacyShortcutPath());
        }

        public static bool IsEnabledForCurrentExecutable()
        {
            return IsRegistryEnabledForCurrentExecutable() || IsStartupScriptEnabledForCurrentExecutable();
        }

        public static bool NeedsPathRepair()
        {
            return IsEnabled() && !IsEnabledForCurrentExecutable();
        }

        public static string GetRegisteredCommand()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                return key == null ? string.Empty : Convert.ToString(key.GetValue(ValueName));
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("无法打开 Windows 启动项注册表。");
                }

                if (enabled)
                {
                    key.SetValue(ValueName, BuildCurrentCommand(), RegistryValueKind.String);
                    CreateStartupScript();
                    DeleteLegacyShortcut();
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                    DeleteStartupScript();
                    DeleteLegacyShortcut();
                }
            }
        }

        private static bool IsRegistryEnabledForCurrentExecutable()
        {
            var registeredPath = GetRegisteredExecutablePath();
            return registeredPath.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStartupScriptEnabledForCurrentExecutable()
        {
            var scriptPath = GetStartupScriptPath();
            if (!File.Exists(scriptPath))
            {
                return false;
            }

            var script = File.ReadAllText(scriptPath);
            return script.IndexOf(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetRegisteredExecutablePath()
        {
            var command = GetRegisteredCommand().Trim();
            if (command.Length == 0)
            {
                return string.Empty;
            }

            if (command[0] == '"')
            {
                var closeQuote = command.IndexOf('"', 1);
                return closeQuote > 1 ? command.Substring(1, closeQuote - 1) : string.Empty;
            }

            var firstSpace = command.IndexOf(' ');
            return firstSpace > 0 ? command.Substring(0, firstSpace) : command;
        }

        private static string BuildCurrentCommand()
        {
            return Quote(Application.ExecutablePath);
        }

        private static string GetStartupScriptPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupScriptName);
        }

        private static void CreateStartupScript()
        {
            var scriptPath = GetStartupScriptPath();
            Directory.CreateDirectory(Path.GetDirectoryName(scriptPath));

            var lines = new[]
            {
                "@echo off",
                "start \"\" /D " + Quote(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')) + " " + Quote(Application.ExecutablePath)
            };

            File.WriteAllLines(scriptPath, lines);
        }

        private static void DeleteStartupScript()
        {
            var scriptPath = GetStartupScriptPath();
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }
        }

        private static void DeleteLegacyShortcut()
        {
            var shortcutPath = GetLegacyShortcutPath();
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }

        private static string GetLegacyShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), LegacyShortcutName);
        }

        private static string Quote(string value)
        {
            return "\"" + value.Trim('"') + "\"";
        }
    }
}
