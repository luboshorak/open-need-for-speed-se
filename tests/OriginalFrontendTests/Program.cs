using System;
using System.IO;
using OpenNFS1;
using OpenNFS1.Parsers;
using OpenNFS1.Vehicles;

static class Program
{
    private static void Assert(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    static void Main()
    {
        // Synthetic EA 32FB Huffman file; all output is generated for this
        // test, not copied from any original NFS game data.
        // Four canonical 2-bit codes (0..3), decoded bytes 0,1,2,3,
        // followed by the 32FB cumulative-delta filter: 0,1,3,6.
        byte[] packed = Convert.FromHexString("32FB000004FF889241B0");
        Assert(EaHuffmanDecoder.CanDecode(packed), "Huffman method not recognized.");
        byte[] result = EaHuffmanDecoder.Decode(packed);
        Assert(result.Length == 4 && result[0] == 0 && result[1] == 1 &&
            result[2] == 3 && result[3] == 6, "EA Huffman decode/filter regression.");

        Assert(!EaHuffmanDecoder.CanDecode(new byte[] { 0x10,0xFB,0,0,0,0,0,0 }),
            "RefPack streams must keep using the existing decoder.");
        bool rejected = false;
        try { EaHuffmanDecoder.Decode(new byte[] { 0x32, 0xFB, 0, 0, 4, 255 }); }
        catch (InvalidDataException) { rejected = true; }
        Assert(rejected, "Truncated QFS must be rejected safely.");

        Assert(PlayerProfile.Normalize("  My Driver-42 !!") == "MY DRIVER-42",
            "Original-style profile name normalization failed.");
        Assert(PlayerProfile.Normalize("  ") == "", "Empty name unexpectedly accepted.");
        Assert(PlayerProfile.Normalize("abcdefghijklmnopqr") == "ABCDEFGHIJKLMNOP",
            "Original 16-character name maximum must be enforced.");

        Assert(OpenRoadStageSelector.SameFamily(
            @"SIMDATA\MISC\AL1.TRI", true, @"SIMDATA\MISC\AL3.TRI", true),
            "Changing an Alpine segment must preserve the Alpine road group.");
        Assert(OpenRoadStageSelector.SameFamily(
            @"SIMDATA\MISC\CL2.TRI", true, @"SIMDATA\MISC\CL3.TRI", true),
            "Changing a Coastal segment must preserve the Coastal road group.");
        Assert(!OpenRoadStageSelector.SameFamily(
            @"SIMDATA\MISC\AL1.TRI", true, @"SIMDATA\MISC\CY1.TRI", true),
            "Stage navigation crossed into an unrelated road.");
        Assert(OpenRoadStageSelector.Family(@"SIMDATA\MISC\TR1.TRI", false) == null,
            "Closed circuits may not be treated as multistage roads.");

        var warning = new PolicePursuitLogic();
        bool dispatched = false;
        for (int i = 0; i < 40; i++)
            dispatched |= warning.Update(.1f, 145, float.MaxValue);
        Assert(dispatched && warning.IsPursuing,
            "Sustained speeding must trigger a patrol.");
        warning.Update(.1f, 0, 2);
        Assert(warning.WarningIssued && !warning.TicketIssued &&
            !warning.IsPursuing,
            "Immediately pulling over after the siren must produce a warning, not a ticket.");
        warning.ClearWarning();

        var officer = new PolicePursuitLogic();
        for (int citation = 1; citation <= 3; citation++)
        {
            // Wait for the prior citation's cooldown to expire.
            for (int i = 0; i < 260; i++)
                officer.Update(.1f, 0, float.MaxValue);
            bool started = false;
            for (int i = 0; i < 40; i++)
                started |= officer.Update(.1f, 145, float.MaxValue);
            Assert(started, "Failed to start a repeat police pursuit.");
            // A genuine ongoing chase, not an immediately accepted warning.
            for (int i = 0; i < 60; i++)
                officer.Update(.1f, 80, 7);
            for (int i = 0; i < 30; i++)
                officer.Update(.1f, 0, 2);
            Assert(officer.TicketIssued && officer.TicketCount == citation,
                "Police did not record the correct citation for a caught driver.");
            officer.ClearTicket();
            Assert(!officer.TicketIssued, "Ticket event was not acknowledged.");
            if (citation < 3)
            {
                bool repeatDuringCooldown = false;
                for (int i = 0; i < 50; i++)
                    repeatDuringCooldown |= officer.Update(.1f, 200, float.MaxValue);
                Assert(!repeatDuringCooldown,
                    "Police pursuit restarted during mandatory cooldown.");
            }
        }
        Assert(officer.IsArrested, "Third ticket must end the stage with arrest.");
        Assert(!officer.Update(.1f, 200, float.MaxValue),
            "Arrested driver cannot trigger another pursuit.");

        // Automatic Down: brake while moving forward, then shift 1st -> N
        // -> reverse and accelerate backwards with the same Down input.
        var brakingForward = ReverseDriveControl.Compute(false, 1, 45, 0, 1, false, false);
        Assert(brakingForward.Brake == 1 && brakingForward.Throttle == 0 &&
            !brakingForward.ShiftDown, "Down during forward motion must BRAKE.");
        var shiftFirst = ReverseDriveControl.Compute(false, 1, 0, 0, 1, false, false);
        Assert(shiftFirst.ShiftDown && shiftFirst.Throttle == 0,
            "Stopped automatic should select neutral before reverse.");
        var shiftNeutral = ReverseDriveControl.Compute(false, 0, 0, 0, 1, false, false);
        Assert(shiftNeutral.ShiftDown, "Stopped neutral must shift into reverse.");
        var reversing = ReverseDriveControl.Compute(false, -1, -10, 0, 1, false, false);
        Assert(reversing.Throttle == 1 && reversing.Brake == 0,
            "Down should accelerate once reverse gear is engaged.");
        var stopReverse = ReverseDriveControl.Compute(false, -1, -10, 1, 0, false, false);
        Assert(stopReverse.Brake == 1 && !stopReverse.ShiftUp,
            "Up must brake reverse motion before shifting into first.");
        var leaveReverse = ReverseDriveControl.Compute(false, -1, 0, 1, 0, false, false);
        Assert(leaveReverse.ShiftUp, "At rest, Up must leave reverse and return to forward.");
        var manualReverse = ReverseDriveControl.Compute(true, -1, -8, 0, 1, false, false);
        Assert(manualReverse.Throttle == 1 && manualReverse.Brake == 0,
            "Manual transmission must allow throttle in explicitly selected reverse.");
        var manualShift = ReverseDriveControl.Compute(true, 0, 0, 0, 0, false, true);
        Assert(manualShift.ShiftDown, "Manual Z must engage reverse from neutral.");

        var pursuitFar = PoliceRoadPlanner.Plan(120, 15, 3, false, 0, true, true);
        Assert(pursuitFar.TargetSpeed > 120 && pursuitFar.TargetLane == 3,
            "Patrol must accelerate towards distant suspect without weaving.");
        var pursuitNear = PoliceRoadPlanner.Plan(0, 1, 3, false, 0, true, true);
        Assert(pursuitNear.TargetSpeed <= 30,
            "Patrol must slow for a stopped suspect instead of ramming the car.");
        var blocked = PoliceRoadPlanner.Plan(150, 10, 3, true, 60, true, true);
        Assert(blocked.TargetLane == 4,
            "Patrol should choose an available passing lane around slower traffic.");
        var trapped = PoliceRoadPlanner.Plan(150, 10, 3, true, 60, false, false);
        Assert(trapped.TargetSpeed <= 60,
            "Patrol unable to change lanes must brake for traffic.");
        Assert(PoliceRoadPlanner.Plan(80, 8, 2, true, 40, true, false).TargetLane == 2,
            "Patrol must not leave the right-hand carriageway into oncoming lane.");

        Console.WriteLine("PASS: Huffman QFS, player name, AL/CL/CY segments, reverse gear, police gap steering and tickets");
    }
}