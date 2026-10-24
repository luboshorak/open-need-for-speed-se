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
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "audio.log"),
                    DateTime.Now.ToString("O") + " " + ex + Environment.NewLine);
            }
            catch { /* audio errors are nonfatal */ }
        }
    }
}
