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
        Assert(championship.StageCount == 6, "Original championship has six A/B/C venues.");
        Assert(championship.CurrentCourse == initial, "Start at the selected circuit.");
        Assert(!championship.TryAdvance(), "Do not advance without a race result.");

        int expectedTotal = 0;
        int expectedSeconds = 0;
        int openRoadCourses = 0;
        for (int i = 0; i < championship.StageCount; i++)
        {
            var current = championship.CurrentCourse;
            Assert(!current.HideFromMenu && !current.Name.Contains("hidden track"),
                "Do not include hidden/unplayable race courses.");
            if (current.IsOpenRoad)
            {
                openRoadCourses++;
                championship.RecordRoadSegment(current, TimeSpan.FromSeconds(25));
                championship.RecordRoadSegment(current, TimeSpan.FromSeconds(999)); // duplicate ignored
                var second = TrackDescription.Descriptions.First(t => t.Name == current.Name + " 2");
                championship.RecordRoadSegment(second, TimeSpan.FromSeconds(25));
                expectedSeconds += 50;
                Assert(championship.CompletedStages == i, "Road stage prematurely completed venue.");
            }
            expectedSeconds += 100;
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
        Assert(openRoadCourses == 3, "Missing original Alpine, Coastal and City road courses.");
        Assert(championship.TotalTime == TimeSpan.FromSeconds(expectedSeconds),
            "Overall race time is wrong.");
        Console.WriteLine("PASS: tournament circuit rotation, point scoring, and results progression");
    }
}
