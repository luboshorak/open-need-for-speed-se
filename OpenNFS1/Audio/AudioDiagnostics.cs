using System;
using System.IO;

namespace OpenNFS1.Audio
{
    internal static class AudioDiagnostics
    {
        private static void Append(string message)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OpenNFS1");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "audio.log"),
                    DateTime.Now.ToString("O") + " " + message + Environment.NewLine);
            }
            catch { /* Sound diagnostics must never interrupt a race. */ }
        }

        public static void Record(Exception ex) => Append("ERROR " + ex);
        public static void Note(string message) => Append("INFO " + message);
    }
}
