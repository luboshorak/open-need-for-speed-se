using System;
using OpenNFS1;

static class Program
{
    static void Check(bool condition, string description)
    {
        if (!condition)
            throw new Exception(description);
    }

    static void Main()
    {
        var circuit = new RaceGateTracker(1, 300, false);
        Check(!circuit.Update(120), "No finish before midpoint");
        Check(!circuit.Update(158), "Passing midpoint is not the finish");
        Check(circuit.Update(303), "Crossing finish between frames must register");
        Check(!circuit.Update(305), "Finish must not count twice");
        Check(!circuit.Update(1), "Circuit wrap must not count as finish");
        Check(!circuit.Update(151), "Second lap midpoint");
        Check(circuit.Update(304), "Second lap finish");
        Check(!circuit.Update(1), "Second wrap");
        Check(!circuit.Update(170), "Third midpoint");
        Check(circuit.Update(301), "Third lap finish");

        // Test the real circuit boundary: TR2 (Autumn Valley) has 1024
        // road nodes, and its finish is at index 1022. At high speed we
        // may jump from 1021 straight to 1 without observing 1022 or 1023.
        var skippedWrap = new RaceGateTracker(1, 1022, false);
        Check(!skippedWrap.Update(600), "Halfway is not the finish");
        Check(!skippedWrap.Update(1021), "Approaching the finish");
        Check(skippedWrap.Update(1), "Crossing the unobserved wrap must count");
        Check(!skippedWrap.Update(2), "No double count after wrap");

        var backwards = new RaceGateTracker(1, 1022, false);
        Check(!backwards.Update(700), "Midpoint reached");
        Check(!backwards.Update(600), "Reversing inside circuit must not count");

        var openRoad = new RaceGateTracker(1, 572, true);
        Check(!openRoad.Update(450), "End of open road not reached");
        Check(openRoad.Update(575), "Crossing open-road finish between frames");
        Check(!openRoad.Update(575), "Repeated finish should not count");

        var reverse = new RaceGateTracker(1, 250, false);
        Check(!reverse.Update(110), "No halfway yet");
        Check(!reverse.Update(5), "Reverse across start must not finish");
        Check(!reverse.Update(240), "Midpoint reached but not finish");
        Check(reverse.Update(252), "Finish after passing midpoint");

        Console.WriteLine("PASS: three circuit laps, skipped nodes and wrapping, open-road finish, and reverse handling");
    }
}