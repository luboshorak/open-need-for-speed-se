using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenNFS1.Vehicles;

namespace OpenNFS1
{
    /// <summary>
    /// Three manual slots and one autosave in the user's profile. Saving
    /// doesn't change the live race, and a corrupt save cannot alter GameConfig.
    /// </summary>
    internal static class TournamentSaveManager
    {
        private static string Folder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenNFS1", "TournamentSaves");

        private static string FileFor(int slot)
        {
            if (slot < 0 || slot > 3)
                throw new ArgumentOutOfRangeException(nameof(slot), "Choose slot 0–3.");
            return Path.Combine(Folder, slot == 0 ? "autosave.json" : "slot" + slot + ".json");
        }

        public static TournamentSaveData Read(int slot)
        {
            string path = FileFor(slot);
            var info = new FileInfo(path);
            if (!info.Exists) return null;
            if (info.Length < 2 || info.Length > 128 * 1024)
                throw new InvalidDataException("Invalid championship save size.");

            var result = JsonSerializer.Deserialize<TournamentSaveData>(File.ReadAllText(path));
            if (result == null || result.Version != 1)
                throw new InvalidDataException("Unsupported championship save format.");
            return result;
        }

        public static string Describe(int slot)
        {
            try
            {
                TournamentSaveData save = Read(slot);
                if (save == null) return "EMPTY";
                return "Race " + (save.StageIndex + 1) + " of 6 | " +
                    save.Points + " pts | " + (save.SavedAtUtc == default
                        ? "date unknown" : save.SavedAtUtc.ToLocalTime().ToString("g"));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is JsonException || ex is InvalidDataException)
            {
                return "UNREADABLE SAVE";
            }
        }

        public static bool Exists(int slot) => File.Exists(FileFor(slot));

        public static void Delete(int slot)
        {
            string path = FileFor(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>Snapshot a restartable NEXT race rather than the results UI.</summary>
        public static void Save(int slot, TrackDescription upcomingTrack)
        {
            TournamentSeries tournament = GameConfig.Tournament ??
                throw new InvalidOperationException("There is no active tournament to save.");

            TournamentSaveData data = tournament.CreateCheckpoint(upcomingTrack);
            data.PlayerCarName = GameConfig.SelectedVehicle?.Name ??
                throw new InvalidOperationException("No player vehicle was selected.");
            data.OpponentCarName = GameConfig.SelectedOpponent?.Name;
            data.UseOpponentPack = GameConfig.UseOpponentPack;
            data.ManualGearbox = GameConfig.ManualGearbox;
            data.AlternativeTimeOfDay = GameConfig.AlternativeTimeOfDay;
            data.CircuitLaps = GameConfig.CircuitLaps;

            Directory.CreateDirectory(Folder);
            string target = FileFor(slot);
            string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp,
                    JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temp, target, overwrite: true);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        public static TrackDescription SuggestedNextTrack()
        {
            if (GameConfig.Tournament == null) return null;
            TrackDescription track = GameConfig.SelectedTrackDescription;
            if (track == null) return null;

            // The result screen has already recorded the just-finished stage.
            if (GameConfig.Tournament.NextVenueAfterResult != null &&
                GameConfig.Tournament.CompletedStages >
                GameConfig.Tournament.StageIndex)
                return GameConfig.Tournament.NextVenueAfterResult;

            return track;
        }

        public static TrackDescription NextAfterResults()
        {
            if (GameConfig.Tournament == null) return null;
            var current = GameConfig.SelectedTrackDescription;
            if (current?.IsOpenRoad == true)
            {
                var nextSegment = TrackDescription.GetNextOpenRoadStage(current);
                if (nextSegment != null) return nextSegment;
            }
            return GameConfig.Tournament.NextVenueAfterResult;
        }

        /// <summary>Load and validate every dependency BEFORE mutating live state.</summary>
        public static void Restore(int slot)
        {
            TournamentSaveData save = Read(slot) ??
                throw new FileNotFoundException("The selected tournament save is empty.");

            TournamentSeries series = TournamentSeries.Restore(save, TrackDescription.Descriptions);
            TrackDescription track = TrackDescription.Descriptions.Find(t =>
                string.Equals(t.FileName, save.UpcomingTrackFile, StringComparison.OrdinalIgnoreCase));
            VehicleDescription vehicle = VehicleDescription.Descriptions.Find(c =>
                string.Equals(c.Name, save.PlayerCarName, StringComparison.OrdinalIgnoreCase));
            VehicleDescription opponent = VehicleDescription.Descriptions.Find(c =>
                string.Equals(c.Name, save.OpponentCarName, StringComparison.OrdinalIgnoreCase));

            if (track == null || vehicle == null ||
                !RaceModeRules.IsVehicleAllowed(RaceMode.Tournament, series.CurrentCourse, vehicle) ||
                save.CircuitLaps < 1 || save.CircuitLaps > 12)
                throw new InvalidDataException("The save references an unavailable race, vehicle or settings.");

            GameConfig.SelectedVehicle = vehicle;
            GameConfig.SelectedOpponent = opponent;
            GameConfig.SelectedTrackDescription = track;
            GameConfig.CurrentRaceMode = RaceMode.Tournament;
            GameConfig.Tournament = series;
            GameConfig.UseOpponentPack = save.UseOpponentPack;
            GameConfig.ManualGearbox = save.ManualGearbox;
            GameConfig.AlternativeTimeOfDay = save.AlternativeTimeOfDay;
            GameConfig.CircuitLaps = save.CircuitLaps;
        }
    }
}
