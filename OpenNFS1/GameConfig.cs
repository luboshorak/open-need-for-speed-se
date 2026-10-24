using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenNFS1.Vehicles;
using OpenNFS1.Parsers.Track;

namespace OpenNFS1
{
    static class GameConfig
    {
        public const float MeshScale = 0.040f;
        public const float TerrainScale = 0.000080f;
        public static readonly float FOV = MathHelper.ToRadians(65);
        public const float MaxSegmentRenderCount = 50;
        public static readonly SamplerState WrapSampler = SamplerState.AnisotropicWrap;

        public static VehicleDescription SelectedVehicle;
        public static VehicleDescription SelectedOpponent;
        public static TrackDescription SelectedTrackDescription;
        public static Track CurrentTrack;
        public static bool ManualGearbox { get; set; }
        public static bool AlternativeTimeOfDay { get; set; }
        public static bool UseOpponentPack { get; set; } = true;
        public static bool NoMercy { get; set; }
        public static bool AbsEnabled { get; set; } = true;
        public static bool TractionControlEnabled { get; set; } = true;
        public static bool MusicEnabled { get; set; } = true;
        public static bool RockMusic { get; set; } = true;
        public static float MusicVolume { get; set; } = 0.45f;
        public static int CircuitLaps { get; set; } = 3;
        public static RaceMode CurrentRaceMode { get; set; } = RaceMode.SingleRace;
        public static bool TimeTrial { get { return CurrentRaceMode == RaceMode.TimeTrial; } }
        public static TournamentSeries Tournament { get; set; }

        public static bool FullScreen { get; set; }
        public static string CdDataPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "CD_Data");
        public static bool Render2dScenery = true, Render3dScenery = true;
        public static bool RenderOnlyPhysicalTrack = true;
        public static int DrawDistance { get; set; } = 5000;
        public static bool RespectOpenRoadCheckpoints { get; set; } = true;
        public static bool DrawDebugInfo { get; set; }

        public static void Load(string[] args = null)
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "gameconfig.json");
            if (File.Exists(configPath))
            {
                using (var doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                {
                    var obj = doc.RootElement;
                    if (obj.TryGetProperty("fullScreen", out var value)) FullScreen = value.GetBoolean();
                    if (obj.TryGetProperty("musicEnabled", out value)) MusicEnabled = value.GetBoolean();
                    if (obj.TryGetProperty("rockMusic", out value)) RockMusic = value.GetBoolean();
                    if (obj.TryGetProperty("musicVolume", out value)) MusicVolume = Math.Clamp(value.GetSingle(), 0, 1);
                    if (obj.TryGetProperty("absEnabled", out value)) AbsEnabled = value.GetBoolean();
                    if (obj.TryGetProperty("tractionControlEnabled", out value)) TractionControlEnabled = value.GetBoolean();
                    if (obj.TryGetProperty("useOpponentPack", out value)) UseOpponentPack = value.GetBoolean();
                    if (obj.TryGetProperty("cdDataPath", out value)) CdDataPath = value.GetString() ?? "CD_Data";
                    if (obj.TryGetProperty("drawDistance", out value)) DrawDistance = value.GetInt32();
                    if (obj.TryGetProperty("respectOpenRoadCheckpoints", out value)) RespectOpenRoadCheckpoints = value.GetBoolean();
                    if (obj.TryGetProperty("drawDebugInfo", out value)) DrawDebugInfo = value.GetBoolean();
                }
            }

            if (!Path.IsPathRooted(CdDataPath))
                CdDataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, CdDataPath));

            if (args == null) return;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--data", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    CdDataPath = Path.GetFullPath(args[++i]);
                else if (string.Equals(args[i], "--fullscreen", StringComparison.OrdinalIgnoreCase))
                    FullScreen = true;
                else if (string.Equals(args[i], "--windowed", StringComparison.OrdinalIgnoreCase))
                    FullScreen = false;
            }
        }

        public static bool HasOriginalGameData()
        {
            // Avoid entering the main menu on an empty or incorrectly nested CD directory.
            return File.Exists(Path.Combine(CdDataPath, "FRONTEND", "ART", "CONTROL", "CENTRAL.QFS")) &&
                   File.Exists(Path.Combine(CdDataPath, "SIMDATA", "MISC", "AL1.TRI"));
        }
    }
}
