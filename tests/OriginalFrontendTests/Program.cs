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

        var officer = new PolicePursuitLogic();
        bool dispatched = false;
        for (int i = 0; i < 40; i++)
            dispatched |= officer.Update(.1f, 145, float.MaxValue);
        Assert(dispatched && officer.IsPursuing, "Sustained speeding did not trigger pursuit.");

        // Only a real pull-over near the cop results in a ticket.
        for (int i = 0; i < 30; i++)
            officer.Update(.1f, 0, 2);
        Assert(officer.TicketIssued, "Stopping close to pursuing police did not issue a ticket.");
        officer.ClearTicket();
        Assert(!officer.TicketIssued, "Acknowledging police ticket did not clear the event.");
        bool immediateRepeat = false;
        for (int i = 0; i < 50; i++)
            immediateRepeat |= officer.Update(.1f, 200, float.MaxValue);
        Assert(!immediateRepeat, "Police pursuit restarted during mandatory cooldown.");

        Console.WriteLine("PASS: Huffman QFS, player name, AL/CL/CY segments, police chase and ticket cooldown");
    }
}