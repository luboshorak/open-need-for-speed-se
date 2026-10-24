using System;
using System.Linq;
using OpenNFS1;
using OpenNFS1.Vehicles;

static class Program
{
    static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
    }

    static void Main()
    {
        var cars = VehicleDescription.Descriptions;
        var courses = TrackDescription.Descriptions;
        var player = cars.First(x => x.Name == "Supra");
        var rival = cars.First(x => x.Name == "Diablo");
        var city = courses.First(x => x.Name == "City");
        var alpine = courses.First(x => x.Name == "Alpine");
        var coastal = courses.First(x => x.Name == "Coastal");

        Check(RaceModeRules.OpponentCount(RaceMode.HeadToHead) == 1, "Head to Head should be 1v1");
        Check(RaceModeRules.OpponentCount(RaceMode.SingleRace) == 7, "Single Race should have seven rivals");
        Check(RaceModeRules.OpponentCount(RaceMode.Tournament) == 7, "Tournament should have seven rivals");
        Check(RaceModeRules.OpponentCount(RaceMode.TimeTrial) == 0, "Time Trial must be solo");

        var head = RaceModeRules.BuildOpponents(RaceMode.HeadToHead, false,
            city, player, rival, cars);
        Check(head.Count == 1 && head[0] == rival, "Wrong head-to-head rival");

        var single = RaceModeRules.BuildOpponents(RaceMode.SingleRace, false,
            city, player, rival, cars);
        Check(single.Count == 7 && single.All(x => x == rival),
            "Choosing a car should create 7 identical rivals");

        var pack = RaceModeRules.BuildOpponents(RaceMode.SingleRace, true,
            city, player, rival, cars);
        Check(pack.Count == 7 && pack.Distinct().Count() == 7 &&
            !pack.Contains(player) && pack.All(x => x.Name != "Warrior"),
            "The Pack must contain seven different standard opponents");

        var trial = RaceModeRules.BuildOpponents(RaceMode.TimeTrial, true,
            city, player, rival, cars);
        Check(trial.Count == 0, "Time Trial contains an opponent");

        foreach (var check in new[] { (city, 3), (coastal, 2), (alpine, 1) })
        {
            Check(RaceModeRules.TournamentClass(check.Item1) == check.Item2,
                "Wrong car performance class");
            var opponents = RaceModeRules.BuildOpponents(
                RaceMode.Tournament, true, check.Item1, player, rival, cars);
            Check(opponents.Count == 7 &&
                opponents.All(x => RaceModeRules.IsVehicleAllowed(
                    RaceMode.Tournament, check.Item1, x)),
                "Tournament rival outside its class");
        }

        Console.WriteLine("PASS: 1v1 Head to Head, 7-car pack/single race, class tournament, solo time trial");
    }
}