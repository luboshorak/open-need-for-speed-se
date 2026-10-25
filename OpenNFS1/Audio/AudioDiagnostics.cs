using System;
using System.IO;

namespace OpenNFS1.Audio
{
    internal static class AudioDiagnostics
    {
        public static void Record(Exception ex)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenNFS1");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "audio.log"),
                    DateTime.Now.ToString("O") + " " + ex + Environment.NewLine);
            }
            catch { /* audio errors are nonfatal */ }
        }
    }
}
