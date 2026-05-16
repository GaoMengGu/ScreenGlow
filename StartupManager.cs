using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenGlow
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ScreenGlow";
        private const string ShortcutName = "ScreenGlow.lnk";

        public static bool IsEnabled()
        {
            return !string.IsNullOrWhiteSpace(GetRegisteredCommand()) || File.Exists(GetStartupShortcutPath());
        }

        public static bool IsEnabledForCurrentExecutable()
        {
            return IsRegistryEnabledForCurrentExecutable() || IsShortcutEnabledForCurrentExecutable();
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
                    CreateStartupShortcut();
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                    DeleteStartupShortcut();
                }
            }
        }

        private static bool IsRegistryEnabledForCurrentExecutable()
        {
            var registeredPath = GetRegisteredExecutablePath();
            return registeredPath.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsShortcutEnabledForCurrentExecutable()
        {
            var shortcutPath = GetStartupShortcutPath();
            if (!File.Exists(shortcutPath))
            {
                return false;
            }

            return GetShortcutTarget(shortcutPath).Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
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

        private static string GetStartupShortcutPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ShortcutName);
        }

        private static void CreateStartupShortcut()
        {
            var shortcutPath = GetStartupShortcutPath();
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath));

            var shellLink = (IShellLinkW)new CShellLink();
            shellLink.SetPath(Application.ExecutablePath);
            shellLink.SetWorkingDirectory(AppDomain.CurrentDomain.BaseDirectory);
            shellLink.SetDescription("ScreenGlow tray light controller");
            shellLink.SetIconLocation(Application.ExecutablePath, 0);

            var persistFile = (IPersistFile)shellLink;
            persistFile.Save(shortcutPath, true);
        }

        private static void DeleteStartupShortcut()
        {
            var shortcutPath = GetStartupShortcutPath();
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }

        private static string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                var shellLink = (IShellLinkW)new CShellLink();
                var persistFile = (IPersistFile)shellLink;
                persistFile.Load(shortcutPath, 0);

                var path = new StringBuilder(260);
                shellLink.GetPath(path, path.Capacity, IntPtr.Zero, 0);
                return path.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Trim('"') + "\"";
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class CShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] string pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] string pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] string pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }
    }
}
