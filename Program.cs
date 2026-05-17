using System;
using System.Threading;
using System.Windows.Forms;

namespace ScreenGlow
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = "ScreenGlow_SingleInstance";
        private static Mutex _singleInstanceMutex;

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out createdNew);
            if (!createdNew)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using (var context = new TrayAppContext())
                {
                    Application.Run(context);
                }
            }
            finally
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
            }
        }
    }
}
