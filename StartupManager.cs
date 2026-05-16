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
        private const string LegacyStartupScriptName = "ScreenGlow Startup.cmd";
        private const string LegacyShortcutName = "ScreenGlow.lnk";

        public static bool IsEnabled()
        {
            return !string.IsNullOrWhiteSpace(GetRegisteredCommand()) || HasLegacyStartupFile();
        }

        public static bool IsEnabledForCurrentExecutable()
        {
            return PathsEqual(GetRegisteredExecutablePath(), Application.ExecutablePath);
        }

        public static bool NeedsPathRepair()
        {
            return IsEnabled() && (!IsEnabledForCurrentExecutable() || HasLegacyStartupFile());
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
                    DeleteLegacyStartupFiles();
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                    DeleteLegacyStartupFiles();
                }
            }
        }

        private static string GetRegisteredExecutablePath()
        {
            var command = GetRegisteredCommand().Trim();
            if (command.Length == 0)
            {
                return string.Empty;
            }

            command = Environment.ExpandEnvironmentVariables(command);

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

        private static bool HasLegacyStartupFile()
        {
            return File.Exists(GetLegacyStartupScriptPath()) || File.Exists(GetLegacyShortcutPath());
        }

        private static void DeleteLegacyStartupFiles()
        {
            DeleteFile(GetLegacyStartupScriptPath());
            DeleteFile(GetLegacyShortcutPath());
        }

        private static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string GetLegacyStartupScriptPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), LegacyStartupScriptName);
        }

        private static string GetLegacyShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), LegacyShortcutName);
        }

        private static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            try
            {
                return Path.GetFullPath(left).Equals(Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }
    }
}
