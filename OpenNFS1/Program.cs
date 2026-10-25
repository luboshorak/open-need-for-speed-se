using System;
using System.IO;

namespace OpenNFS1
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 4 && args[1] == "--audio-audit")
            {
                Environment.ExitCode = Audio.AudioAudit.Run(args[2], args[3]);
                return;
            }
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            try
            {
                using (var game = new Game1())
                {
                    WriteLaunchIdentity();
                    game.Run();
                }
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

        // Diagnostic evidence of WHICH compiled game binary is actually running.
        // A successful compilation alone cannot prove that the user launched
        // the newly published EXE instead of an older desktop shortcut.
        private static void WriteLaunchIdentity()
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OpenNFS1");
                Directory.CreateDirectory(folder);
                string provenanceFile = Path.Combine(AppContext.BaseDirectory, "build-provenance.txt");
                string provenance = File.Exists(provenanceFile)
                    ? File.ReadAllText(provenanceFile).Trim()
                    : "Source commit not stamped (developer/older build).";
                string identity = "Started: " + DateTimeOffset.Now.ToString("O") + Environment.NewLine
                    + "EXE: " + (Environment.ProcessPath ?? "(unknown)") + Environment.NewLine
                    + "Directory: " + AppContext.BaseDirectory + Environment.NewLine
                    + "Assembly version: " + typeof(Program).Assembly.GetName().Version + Environment.NewLine
                    + "Original CD data: " + GameConfig.CdDataPath + Environment.NewLine
                    + "Source: " + provenance + Environment.NewLine;
                File.WriteAllText(Path.Combine(folder, "last-launch.txt"), identity);
            }
            catch (Exception)
            {
                // Read-only profiles must not prevent playing.
            }
        }

        private static void WriteCrashLog(object error)
        {
            try
            {
                var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenNFS1");
                Directory.CreateDirectory(logDir);
                File.WriteAllText(Path.Combine(logDir, "exception.txt"),
                    DateTime.Now.ToString("O") + Environment.NewLine + error);
            }
            catch { /* never hide the original exception if the output folder is read-only */ }
        }
    }
}
