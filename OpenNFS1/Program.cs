using System;
using System.IO;

namespace OpenNFS1
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            try
            {
                using (var game = new Game1())
                    game.Run();
            }
            catch (Exception ex)
            {
                WriteCrashLog(ex);
                throw;
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteCrashLog(e.ExceptionObject);
        }

        private static void WriteCrashLog(object error)
        {
            try
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "exception.txt"), error.ToString());
            }
            catch { /* never hide the original exception if the output folder is read-only */ }
        }
    }
}
