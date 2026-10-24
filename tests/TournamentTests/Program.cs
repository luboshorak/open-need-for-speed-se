using System;
using System.Linq;
using System.Text.Json;
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
        // Snapshot 1: resume a three-segment road venue after its first segment.
        var alpine = TrackDescription.Descriptions.First(t => t.Name == "Alpine");
        var alpine2 = TrackDescription.GetNextOpenRoadStage(alpine);
        var alpine3 = TrackDescription.GetNextOpenRoadStage(alpine2);
        var road = new TournamentSeries(alpine, TrackDescription.Descriptions);
        road.RecordRoadSegment(alpine, TimeSpan.FromSeconds(75));
        var partial = road.CreateCheckpoint(alpine2);
        Assert(partial.StageIndex == 0 && partial.CompletedStages == 0,
            "Incomplete road venue must not increment tournament round.");
        partial = JsonSerializer.Deserialize<TournamentSaveData>(
            JsonSerializer.Serialize(partial));
        var restored = TournamentSeries.Restore(partial, TrackDescription.Descriptions);
        restored.RecordRoadSegment(alpine2, TimeSpan.FromSeconds(80));
        restored.RecordResult(1, TimeSpan.FromSeconds(85));
        Assert(restored.CompletedStages == 1 && restored.Points == 9,
            "Resumed open-road stage lost the tournament standing.");
        Assert(restored.TotalTime == TimeSpan.FromSeconds(240),
            "The first road segment was missing after resume.");

        // Snapshot 2: result screen projects forward to the NEXT venue without
        // mutating current live championship, so repeated saves cannot double-score.
        var next = restored.NextVenueAfterResult;
        var projected = restored.CreateCheckpoint(next);
        Assert(projected.CompletedStages == 1 && projected.StageIndex == 1,
            "Checkpoint incorrectly restarts already completed venue.");
        var resumed = TournamentSeries.Restore(projected, TrackDescription.Descriptions);
        Assert(resumed.CurrentCourse == next && resumed.Points == 9 &&
            resumed.CompletedStages == 1, "Restored round/points mismatch.");
        Assert(restored.StageIndex == 0 && restored.CompletedStages == 1,
            "Saving changed live results and could double-score.");

        // Corrupted saves are rejected rather than leaving invalid live state.
        projected.Points = -1;
        bool refused = false;
        try { TournamentSeries.Restore(projected, TrackDescription.Descriptions); }
        catch (InvalidOperationException) { refused = true; }
        Assert(refused, "Invalid or tampered scores were accepted.");

        Console.WriteLine("PASS: tournament venues, scoring, checkpoint serialization, road resume and tamper rejection");
    }
}
