using System;
using System.Windows.Forms;

namespace ScreenGlow
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var context = new TrayAppContext())
            {
                Application.Run(context);
            }
        }
    }
}
