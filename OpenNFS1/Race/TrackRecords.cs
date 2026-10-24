using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OpenNFS1
{
    /// <summary>
    /// Real, local race records. Does not manufacture the original game's
    /// preinstalled score table: only completed races produce entries.
    /// </summary>
    internal static class TrackRecords
    {
        private static readonly string PathToScores = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenNFS1", "track-records.json");
        private static readonly List<Entry> Entries = new List<Entry>();
        private static bool _loaded;

        internal sealed class Entry
        {
            public string TrackFile { get; set; }
            public string Player { get; set; }
            public string Car { get; set; }
            public int RaceMode { get; set; }
            public double Milliseconds { get; set; }
            public float TopSpeed { get; set; }
            public DateTime AtUtc { get; set; }
        }

        public static IReadOnlyList<Entry> Get(string trackFile, int limit = 10)
        {
            Load();
            return Entries.Where(e => string.Equals(e.TrackFile, trackFile,
                    StringComparison.OrdinalIgnoreCase) && e.Milliseconds > 0)
                .OrderBy(e => e.Milliseconds).Take(Math.Clamp(limit, 1, 50)).ToList();
        }

        public static Entry FastestSpeed(string trackFile)
        {
            Load();
            return Entries.Where(e => string.Equals(e.TrackFile, trackFile,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.TopSpeed).FirstOrDefault();
        }

        public static void Record(string trackFile, string car,
            RaceMode mode, TimeSpan time, float topSpeed)
        {
            if (string.IsNullOrWhiteSpace(trackFile) || !PlayerProfile.IsNamed ||
                time <= TimeSpan.Zero || time.TotalHours > 24)
                return;

            Load();
            Entries.Add(new Entry
            {
                TrackFile = trackFile,
                Player = PlayerProfile.Name,
                Car = car ?? "UNKNOWN",
                RaceMode = (int)mode,
                Milliseconds = time.TotalMilliseconds,
                TopSpeed = Math.Clamp(topSpeed, 0, 1000),
                AtUtc = DateTime.UtcNow
            });

            // Keep enough old runs for speed records and multiple players, but
            // bound growth and malformed/oversized user data on future loads.
            if (Entries.Count > 2000)
                Entries.RemoveRange(0, Entries.Count - 2000);
            Save();
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!File.Exists(PathToScores) ||
                    new FileInfo(PathToScores).Length > 1024 * 1024) return;
                var items = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(PathToScores));
                if (items == null) return;
                Entries.AddRange(items.Where(e => e != null &&
                    e.Milliseconds > 0 && e.Milliseconds <= 24 * 3600 * 1000 &&
                    !string.IsNullOrWhiteSpace(e.TrackFile) &&
                    !string.IsNullOrWhiteSpace(e.Player)).Take(2000));
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException ||
                                       ex is UnauthorizedAccessException)
            {
                // Corrupt records must not block launching a race.
            }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PathToScores));
                string temp = PathToScores + ".tmp";
                try
                {
                    File.WriteAllText(temp, JsonSerializer.Serialize(Entries));
                    File.Move(temp, PathToScores, true);
                }
                finally
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
