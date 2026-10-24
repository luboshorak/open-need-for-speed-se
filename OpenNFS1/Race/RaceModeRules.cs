using System;
using System.Collections.Generic;
using System.Linq;
using OpenNFS1.Vehicles;

namespace OpenNFS1
{
    /// <summary>
    /// Original single-player opponent rules from the Need for Speed SE manual.
    /// Single Race: seven different cars (The Pack), or seven of one model.
    /// Head to Head: exactly one selected rival. Time Trial: none.
    /// Tournament: seven opponents selected from the track's car class.
    /// </summary>
    internal static class RaceModeRules
    {
        public const int FullGridOpponents = 7;

        public static int OpponentCount(RaceMode mode) =>
            mode == RaceMode.TimeTrial ? 0 :
            mode == RaceMode.HeadToHead ? 1 : FullGridOpponents;

        public static bool UsesSelectedCar(RaceMode mode, bool pack) =>
            mode == RaceMode.HeadToHead || (mode == RaceMode.SingleRace && !pack);

        public static int TournamentClass(TrackDescription track)
        {
            if (track == null) return 0;
            string name = track.Name;
            if (name == "City" || name == "Rusty Springs") return 3; // C
            if (name == "Coastal" || name == "Autumn Valley") return 2; // B
            if (name == "Alpine" || name == "Vertigo Ridge") return 1; // A
            return 0; // Bonus/SE tracks: no documented class
        }

        private static int VehicleClass(VehicleDescription car)
        {
            switch (car.Name)
            {
                case "RX7":
                case "NSX":
                case "Supra": return 3;
                case "911":
                case "Viper":
                case "ZR1": return 2;
                case "Diablo":
                case "F512": return 1;
                default: return 0;
            }
        }

        public static bool IsVehicleAllowed(RaceMode mode, TrackDescription track, VehicleDescription vehicle)
        {
            return mode != RaceMode.Tournament || TournamentClass(track) == 0 ||
                VehicleClass(vehicle) == TournamentClass(track);
        }

        public static List<VehicleDescription> BuildOpponents(RaceMode mode, bool pack,
            TrackDescription track, VehicleDescription player, VehicleDescription chosen,
            IReadOnlyList<VehicleDescription> available)
        {
            var result = new List<VehicleDescription>();
            int count = OpponentCount(mode);
            if (count == 0 || available == null || available.Count == 0) return result;

            if (mode == RaceMode.HeadToHead || (mode == RaceMode.SingleRace && !pack))
            {
                var rival = chosen ?? available.First(c => c.Name != "Warrior");
                for (int i = 0; i < count; i++) result.Add(rival);
                return result;
            }

            List<VehicleDescription> pool;
            if (mode == RaceMode.Tournament && TournamentClass(track) != 0)
                pool = available.Where(c => VehicleClass(c) == TournamentClass(track)).ToList();
            else
                pool = available.Where(c => c.Name != "Warrior" && c != player).ToList();

            if (pool.Count == 0) pool = available.Where(c => c.Name != "Warrior").ToList();
            if (pool.Count == 0) pool = available.ToList();
            for (int i = 0; i < count; i++) result.Add(pool[i % pool.Count]);
            return result;
        }
    }
}
