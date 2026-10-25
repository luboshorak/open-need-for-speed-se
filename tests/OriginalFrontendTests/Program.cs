using System;
using System.IO;
using OpenNFS1;
using OpenNFS1.Parsers;
using OpenNFS1.Vehicles;
using OpenNFS1.Vehicles.AI;

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

        // Hidden AL2/AL3, CL2/CL3 and CY2/CY3 must reopen their own
        // visible course, not default to the first Alpine menu entry.
        foreach (string stage in new[] { "2", "3" })
        {
            foreach (string family in new[] { "AL", "CL", "CY" })
            {
                Assert(OpenRoadStageSelector.IsVisibleCourseForSelection(
                        @"SIMDATA\MISC\" + family + "1.TRI", true,
                        @"SIMDATA\MISC\" + family + stage + ".TRI", true),
                    "Hidden " + family + stage + " did not resolve to visible " + family + "1.");
            }
        }
        Assert(!OpenRoadStageSelector.IsVisibleCourseForSelection(
                @"SIMDATA\MISC\AL1.TRI", true, @"SIMDATA\MISC\CL2.TRI", true),
            "Alpine incorrectly matched a different road's hidden Coastal segment.");
        Assert(OpenRoadStageSelector.IsVisibleCourseForSelection(
                @"SIMDATA\MISC\TR2.TRI", false, @"simdata\misc\tr2.tri", false),
            "Identical circuit files should resolve case-insensitively.");


        foreach (float yaw in new[] { 0f, .5f, 1.5f })
        {
            var side = new System.Numerics.Vector3(MathF.Cos(yaw),0,MathF.Sin(yaw));
            var forward = new System.Numerics.Vector3(-side.Z,0,side.X);
            var left = -side*20; var right = side*20;
            var original = forward*17 + side*2;
            var pulled = PoliceStopPlacement.ToRight(original,left,right,6);
            Assert(Math.Abs(System.Numerics.Vector3.Dot(pulled,forward)-17)<.001,
                "Police stop must preserve longitudinal progress on rotated roads");
            Assert(Math.Abs(System.Numerics.Vector3.Dot(pulled,side)-17)<.001,
                "Police stop must respect half car width at verge");
        }
        Console.WriteLine("PASS: roadside placement preserves exact along-road progress");
        var warning = new PolicePursuitLogic();
        for (int i = 0; i < 100; i++) warning.Update(.03f, 180, 0, 0, 70, false);
        Assert(!warning.IsPursuing, "Speeding without an actual patrol must not dispatch one");
        for (int i = 0; i < 4; i++) warning.Update(.03f, 70, 50, 0, 70, true);
        Assert(!warning.IsPursuing, "Limit equality is legal");
        for (int i = 0; i < 4; i++) warning.Update(.03f, 90, 50, 5, 70, true);
        Assert(!warning.IsPursuing, "Exactly five nodes is outside detection");
        for (int i = 0; i < 4; i++) warning.Update(.03f, 90, 50, 4, 70, true);
        Assert(warning.IsPursuing, "Nearby speeding starts the original chase");
        for (int i = 0; i < 4; i++) warning.Update(.03f, 90, 50, 4, 70, true);
        Assert(!warning.Caught && !warning.HasJoinedPursuit, "Patrol ahead cannot catch player merely passing it");
        for (int i = 0; i < 4; i++) warning.Update(.03f, 90, 50, -1, 70, true);
        Assert(warning.HasJoinedPursuit, "Patrol behind player must join actual forward pursuit");
        for (int i = 0; i < 4; i++) warning.Update(.03f, 0, 20, 0, 70, true);
        Assert(!warning.WarningIssued, "Stopped player alone is not a completed stop");
        for (int i = 0; i < 4; i++) warning.Update(.03f, 0, 0, -1, 70, true);
        Assert(!warning.WarningIssued, "Police behind the player have not caught it");
        for (int i = 0; i < 4; i++) warning.Update(.03f, 0, 0, 0, 70, true);
        Assert(warning.WarningIssued && warning.WarningCount == 1 && warning.TicketCount == 0,
            "Short chase and both stopped gives a warning");
        warning.ClearEvents();
        Assert(!warning.WarningIssued, "Warning event acknowledgement");
        var officer = new PolicePursuitLogic();
        for (int citation = 1; citation <= 2; citation++)
        {
            for (int i = 0; i < 4; i++) officer.Update(.03f, 100, 100, -1, 70, true);
            Assert(officer.IsPursuing, "Repeat real patrol encounter starts");
            for (int i = 0; i < 400; i++) officer.Update(.03f, 100, 100, -1, 70, true);
            Assert(officer.ChaseTicks == 400, "400 original ticks = 12 seconds");
            for (int i = 0; i < 4; i++) officer.Update(.03f, 0, 0, 0, 70, true);
            Assert(officer.TicketIssued && officer.TicketCount == citation,
                "Long chase gives a ticket, not a warning");
            officer.ClearEvents();
        }
        Assert(officer.IsArrested, "Second ticket arrests");
        for (int i = 0; i < 4; i++) officer.Update(.03f, 200, 100, 0, 70, true);
        Assert(!officer.IsPursuing, "Arrest prevents another pursuit");
        foreach (float frame in new[] { 1f/30, 1f/60, 1f/144 })
        {
            var timed = new PolicePursuitLogic();
            for (int i = 0; i < (int)Math.Round(1/frame); i++) timed.Update(frame, 120, 120, -1, 70, true);
            Assert(timed.ChaseTicks == 28, "Police tick cadence must not depend on frame rate");
        }
        Assert(!PolicePursuitLogic.SpawnAllowed(0, 0), "Unmarked nodes never spawn police");
        Assert(PolicePursuitLogic.SpawnAllowed(1, 32) && !PolicePursuitLogic.SpawnAllowed(1, 33), "Original inclusive odds threshold");
        Assert(PolicePursuitLogic.SpawnAllowed(0x19, 127), "Highest category always passes odds");
        var lost = new PolicePursuitLogic();
        for (int i = 0; i < 4; i++) lost.Update(.03f, 120, 100, 0, 70, true);
        for (int i = 0; i < 4; i++) lost.Update(.03f, 120, 0, 0, 70, false);
        Assert(!lost.IsPursuing && !lost.Caught, "Disappearing patrol clears chase/caught state");
        Assert(Math.Abs(PolicePursuitLogic.PenaltySeconds - 1.8f) < .001, "Original sixty-step hold");

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
        Assert(pursuitNear.TargetSpeed > 0 && pursuitNear.TargetSpeed <= 30,
            "Patrol must slow for a stopped suspect instead of ramming the car.");
        var blocked = PoliceRoadPlanner.Plan(150, 10, 3, true, 60, true, true);
        Assert(blocked.TargetLane == 4,
            "Patrol should choose an available passing lane around slower traffic.");
        var trapped = PoliceRoadPlanner.Plan(150, 10, 3, true, 60, false, false);
        Assert(trapped.TargetSpeed <= 60,
            "Patrol unable to change lanes must brake for traffic.");
        Assert(PoliceRoadPlanner.Plan(80, 8, 2, true, 40, true, false).TargetLane == 2,
            "Patrol must not leave the right-hand carriageway into oncoming lane.");

        Assert(PoliceRoadPlanner.Plan(140, 2, 3, false, 0, true, true).TargetSpeed > 140,
            "Patrol must be able to pass a moving suspect");
        Assert(PoliceRoadPlanner.YawStep(-1, 100, .02f) < 0, "Right turn has negative yaw in this coordinate system");
        float slow = 1, fast = 1;
        for (int i = 0; i < 60; i++) slow -= PoliceRoadPlanner.YawStep(slow, 100, 1f/30);
        for (int i = 0; i < 120; i++) fast -= PoliceRoadPlanner.YawStep(fast, 100, 1f/60);
        Assert(slow > 0 && slow < .03f && Math.Abs(slow-fast) < .01f, "Bounded steering converges without frame-dependent weaving");
        var alert = new PolicePursuitLogic();
        for(int i=0;i<8;i++) alert.Update(.03f,120,50,3,70,true,true,false);
        Assert(alert.IsPursuing && !alert.Caught, "Oncoming alert must not become caught");
        float[] offsets={-20,-10,0,10,20};
        var oncoming=new[]{new RacerTrafficPlanner.Obstacle(150,10,7,600,10)};
        var avoid=RacerTrafficPlanner.Plan(3,2,offsets,oncoming);
        Assert(avoid.Lane!=3 && avoid.Brake>0, "Oncoming hazard must be handled far before contact");
        var adjacent=new[]{new RacerTrafficPlanner.Obstacle(100,20,6,100,10)};
        Assert(RacerTrafficPlanner.Plan(3,2,offsets,adjacent).Lane==3, "Adjacent traffic is not an obstacle in our lane");
        var packedTraffic=new[]{new RacerTrafficPlanner.Obstacle(100,10,7,200,10),
            new RacerTrafficPlanner.Obstacle(0,0,7,0,10),new RacerTrafficPlanner.Obstacle(0,20,7,0,10)};
        var stop=RacerTrafficPlanner.Plan(3,2,offsets,packedTraffic);
        Assert(stop.Lane==3 && stop.Brake>0, "Blocked adjacent lanes require braking instead of ramming");
        var edge=RacerTrafficPlanner.Plan(2,2,offsets,new[]{new RacerTrafficPlanner.Obstacle(80,0,7,200,10)});
        Assert(edge.Lane>=2, "Never evade into oncoming carriageway");
        Console.WriteLine("PASS: alert vs joined pursuit; predictive oncoming avoidance, adjacent clearance and emergency braking");
        Console.WriteLine("PASS: original patrol detection, cadence, catch, warning/ticket, second-ticket arrest, spawn odds and stable steering");
        Console.WriteLine("PASS: Huffman QFS, player name, AL/CL/CY segments, reverse gear, police gap steering and tickets");
    }
}