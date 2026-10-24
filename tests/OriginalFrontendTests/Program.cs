using System;
using System.IO;
using OpenNFS1;
using OpenNFS1.Parsers;

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

        Console.WriteLine("PASS: original EA 32FB Huffman assets, decoder errors, player name normalization");
    }
}