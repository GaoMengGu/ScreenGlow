using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenGlow
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ScreenGlow";

        public static bool IsEnabled()
        {
            return !string.IsNullOrWhiteSpace(GetRegisteredCommand());
        }

        public static bool IsEnabledForCurrentExecutable()
        {
            var registeredPath = GetRegisteredExecutablePath();
            return registeredPath.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
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
                }
                else
                {
                    key.DeleteValue(ValueName, false);
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

        private static string Quote(string value)
        {
            return "\"" + value.Trim('"') + "\"";
        }
    }
}
