using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OpenNFS1
{
    /// <summary>
    /// Persistent player identity, separate from licensed original CD assets.
    /// On the first run, PlayerNameScreen asks for this name before the main
    /// menu; records use the name as it existed when a race was completed.
    /// </summary>
    internal static class PlayerProfile
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenNFS1", "player.json");

        public static string Name { get; private set; }
        public static bool IsNamed => !string.IsNullOrWhiteSpace(Name);

        public static void Load()
        {
            Name = null;
            if (!File.Exists(FilePath)) return;
            try
            {
                if (new FileInfo(FilePath).Length > 4096) return;
                var data = JsonSerializer.Deserialize<ProfileData>(File.ReadAllText(FilePath));
                if (data?.Version == 1 && !string.IsNullOrWhiteSpace(data.Name))
                    Name = Normalize(data.Name);
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException ||
                                       ex is UnauthorizedAccessException)
            {
                // A corrupt profile must never prevent the game from starting.
            }
        }

        public static string Normalize(string entered)
        {
            string normalized = new string((entered ?? "").Trim().ToUpperInvariant()
                .Where(c => c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' ||
                            c == ' ' || c == '-' || c == '_').Take(16).ToArray());
            return normalized.Trim();
        }

        public static void Save(string name)
        {
            string normalized = Normalize(name);
            if (normalized.Length == 0)
                throw new ArgumentException("Enter a player name.", nameof(name));

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temp = FilePath + ".tmp";
            try
            {
                File.WriteAllText(temp,
                    JsonSerializer.Serialize(new ProfileData { Version = 1, Name = normalized }));
                File.Move(temp, FilePath, true);
                Name = normalized;
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        private sealed class ProfileData
        {
            public int Version { get; set; }
            public string Name { get; set; }
        }
    }
}
