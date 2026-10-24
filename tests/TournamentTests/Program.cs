using System;
using System.Linq;
using OpenNFS1;

static class Program
{
    static void Assert(bool ok, string msg)
    {
        if (!ok) throw new Exception(msg);
    }

    static void Main()
    {
        var initial = TrackDescription.Descriptions.First(
            t => t.Name == "Autumn Valley");
        var championship = new TournamentSeries(initial, TrackDescription.Descriptions);
        Assert(championship.StageCount >= 5, "Expected playable championship circuits.");
        Assert(championship.CurrentCourse == initial, "Start at the selected circuit.");
        Assert(!championship.TryAdvance(), "Do not advance without a race result.");

        int expectedTotal = 0;
        for (int i = 0; i < championship.StageCount; i++)
        {
            var current = championship.CurrentCourse;
            Assert(!current.IsOpenRoad, "Only closed racing circuits are allowed.");
            Assert(!current.HideFromMenu && !current.Name.Contains("hidden track"),
                "Do not include hidden/unplayable race courses.");
            championship.RecordResult(i, TimeSpan.FromSeconds(100));
            int awarded = Math.Max(1, 10 - i);
            expectedTotal += awarded;
            championship.RecordResult(0, TimeSpan.FromSeconds(999));
            Assert(championship.Points == expectedTotal, "Result counted twice.");
            Assert(championship.CompletedStages == i + 1, "Wrong stage count.");
            Assert(championship.TryAdvance() == (i < championship.StageCount - 1),
                "Incorrect next-stage availability.");
        }
        Assert(championship.IsComplete, "Championship never completes.");
        Assert(championship.TotalTime == TimeSpan.FromSeconds(championship.StageCount * 100),
            "Overall race time is wrong.");
        Console.WriteLine("PASS: tournament circuit rotation, point scoring, and results progression");
    }
}
