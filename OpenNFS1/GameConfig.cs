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
        public static int TextureQuality { get; set; } = 2; // Point, Linear, Anisotropic
        public static bool HighCarDetail { get; set; } = true;
        public static bool HorizonEnabled { get; set; } = true;
        public static SamplerState WrapSampler => TextureQuality switch
        {
            0 => SamplerState.PointWrap,
            1 => SamplerState.LinearWrap,
            _ => SamplerState.AnisotropicWrap
        };
        public static SamplerState ClampSampler => TextureQuality switch
        {
            0 => SamplerState.PointClamp,
            1 => SamplerState.LinearClamp,
            _ => SamplerState.AnisotropicClamp
        };

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
        public static bool MenuMusicEnabled { get; set; } = true;
        public static bool SoundEffectsEnabled { get; set; } = true;
        public static bool RockMusic { get; set; } = true;
        public static float MusicVolume { get; set; } = 0.45f;
        public static int CircuitLaps { get; set; } = 6;
        public static RaceMode CurrentRaceMode { get; set; } = RaceMode.SingleRace;
        public static bool TimeTrial { get { return CurrentRaceMode == RaceMode.TimeTrial; } }
        public static TournamentSeries Tournament { get; set; }

        public static bool FullScreen { get; set; }
        // Output only: gameplay and all original QFS coordinates stay 640x480.
        // 0 = smooth, 1 = sharp nearest, 2 = pixel-perfect integer scaling.
        public static int ScalingMode { get; set; } = 1;
        public static int WindowWidth { get; set; } = 1280;
        public static int WindowHeight { get; set; } = 960;
        public static bool OriginalColors { get; set; } = true;
        public static float Gamma { get; set; } = 1.0f;
        public static float Brightness { get; set; } = 0.0f;
        public static float Contrast { get; set; } = 1.0f;
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

            // User preferences live outside the install directory and never
            // overwrite the bundled gameconfig.json or any original CD files.
            if (File.Exists(UserSettingsPath))
            {
                using var preferences = JsonDocument.Parse(File.ReadAllText(UserSettingsPath));
                var cfg = preferences.RootElement;
                if (cfg.TryGetProperty("fullScreen", out var value)) FullScreen = value.GetBoolean();
                if (cfg.TryGetProperty("windowWidth", out value)) WindowWidth = Math.Clamp(value.GetInt32(), 640, 3840);
                if (cfg.TryGetProperty("windowHeight", out value)) WindowHeight = Math.Clamp(value.GetInt32(), 480, 2160);
                if (cfg.TryGetProperty("scalingMode", out value)) ScalingMode = Math.Clamp(value.GetInt32(), 0, 2);
                if (cfg.TryGetProperty("originalColors", out value)) OriginalColors = value.GetBoolean();
                if (cfg.TryGetProperty("gamma", out value)) Gamma = Math.Clamp(value.GetSingle(), 0.6f, 1.6f);
                if (cfg.TryGetProperty("brightness", out value)) Brightness = Math.Clamp(value.GetSingle(), -0.3f, 0.3f);
                if (cfg.TryGetProperty("contrast", out value)) Contrast = Math.Clamp(value.GetSingle(), 0.6f, 1.6f);
                if (cfg.TryGetProperty("musicEnabled", out value)) MusicEnabled = value.GetBoolean();
                if (cfg.TryGetProperty("menuMusicEnabled", out value)) MenuMusicEnabled = value.GetBoolean();
                if (cfg.TryGetProperty("soundEffectsEnabled", out value)) SoundEffectsEnabled = value.GetBoolean();
                if (cfg.TryGetProperty("rockMusic", out value)) RockMusic = value.GetBoolean();
                if (cfg.TryGetProperty("musicVolume", out value)) MusicVolume = Math.Clamp(value.GetSingle(), 0, 1);
                if (cfg.TryGetProperty("circuitLaps", out value))
                    CircuitLaps = Math.Clamp(value.GetInt32(), 1, 12);
                if (cfg.TryGetProperty("manualGearbox", out value)) ManualGearbox = value.GetBoolean();
                if (cfg.TryGetProperty("absEnabled", out value)) AbsEnabled = value.GetBoolean();
                if (cfg.TryGetProperty("tractionControlEnabled", out value)) TractionControlEnabled = value.GetBoolean();
                if (cfg.TryGetProperty("drawDistance", out value)) DrawDistance = value.GetInt32();
                    if (cfg.TryGetProperty("textureQuality", out value)) TextureQuality = Math.Clamp(value.GetInt32(), 0, 2);
                    if (cfg.TryGetProperty("highCarDetail", out value)) HighCarDetail = value.GetBoolean();
                    if (cfg.TryGetProperty("horizonEnabled", out value)) HorizonEnabled = value.GetBoolean();
                if (cfg.TryGetProperty("useOpponentPack", out value)) UseOpponentPack = value.GetBoolean();
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

        private static string UserSettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenNFS1", "settings.json");

        public static void SaveUserSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(UserSettingsPath));
                var values = new
                {
                    fullScreen = FullScreen,
                    windowWidth = WindowWidth,
                    windowHeight = WindowHeight,
                    scalingMode = ScalingMode,
                    originalColors = OriginalColors,
                    gamma = Gamma,
                    brightness = Brightness,
                    contrast = Contrast,
                    musicEnabled = MusicEnabled,
                    menuMusicEnabled = MenuMusicEnabled,
                    soundEffectsEnabled = SoundEffectsEnabled,
                    rockMusic = RockMusic,
                    musicVolume = MusicVolume,
                    circuitLaps = CircuitLaps,
                    manualGearbox = ManualGearbox,
                    absEnabled = AbsEnabled,
                    tractionControlEnabled = TractionControlEnabled,
                    drawDistance = DrawDistance,
                    textureQuality = TextureQuality,
                    highCarDetail = HighCarDetail,
                    horizonEnabled = HorizonEnabled,
                    useOpponentPack = UseOpponentPack
                };
                File.WriteAllText(UserSettingsPath,
                    JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (IOException) { /* read-only profile must not crash the game */ }
            catch (UnauthorizedAccessException) { }
        }

        public static bool HasOriginalGameData()
        {
            // Avoid entering the main menu on an empty or incorrectly nested CD directory.
            return File.Exists(Path.Combine(CdDataPath, "FRONTEND", "ART", "CONTROL", "CENTRAL.QFS")) &&
                   File.Exists(Path.Combine(CdDataPath, "SIMDATA", "MISC", "AL1.TRI"));
        }
    }
}
