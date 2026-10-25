using System;
using System.IO;

namespace OpenNFS1.Audio
{
    internal static class OriginalAudioRules
    {
        public const int Gear = 32, Landing = 37, Wall = 35, Road = 41;
        public const int Skid = 42, OffRoad = 38, Gravel = 80;
        public const int Radar = 97, Siren = 98, SirenEcho = 100;
        public static int CarImpact(float speed) => speed > 110 ? 36 : speed > 60 ? 34 : 33;
        public static bool Tunnel(byte property) => property is 4 or 7 or 9 or 12 or 13;
        public static bool Rough(byte property) => property is 5 or 18;
        public static int AmbienceSide(byte property) => property is 14 or 17 ? -1 : property is 15 or 18 ? 1 : 0;
        public static string CollisionBank(string track) =>
            Path.GetFileNameWithoutExtension(track ?? "").Equals("tr7", StringComparison.OrdinalIgnoreCase)
                ? "COLLSWMT.BNK" : "COLLSWWT.BNK";
        public static int AmbienceSlot(string bank) => bank == "COLLSWMT.BNK" ? 61 : 62;
        public static float Pitch(int bend, int reach, int cents = 0) =>
            Math.Clamp((cents + (Math.Clamp(bend, 0, 127) - 64) * reach * 100f / 64f) / 1200f, -1f, 1f);
        public static float RadarPeriod(int gap, bool behind)
        {
            int level = Math.Clamp((64 - Math.Abs(gap)) / 8, 0, 6);
            if (behind) level /= 2;
            return level == 0 ? float.PositiveInfinity : new[] { 1, 20, 15, 10, 5, 3, 1 }[level] / 30f;
        }
        public static string LapSpeech(bool openRoad, int lap, int laps, bool best) =>
            openRoad || lap <= 1 || lap > laps ? null :
            best ? (lap == laps ? "BESTLAST.EAS" : "BESTTIME.EAS") :
            lap == laps ? "FINALLAP.EAS" : null;
        public static string OpponentBank(string carBank) => carBank.ToUpperInvariant() switch
        {
            "911_SW.BNK" => "O911MB.BNK", "SUPRA_SW.BNK" => "OSUPMB.BNK",
            "RX7_SW.BNK" => "ORX7MB.BNK", "NSX_SW.BNK" => "ONSXMB.BNK",
            "ZR1_SW.BNK" => "OZR1MB.BNK", "VIPERSW.BNK" => "OVIPMB.BNK",
            "TR512_SW.BNK" => "O512MB.BNK", "DIABLOSW.BNK" => "ODIAMB.BNK",
            _ => "TRAFFCO.BNK"
        };
    }
}
