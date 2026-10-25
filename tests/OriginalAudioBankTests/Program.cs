using System;
using System.IO;
using System.Linq;
using OpenNFS1.Parsers.Audio;

namespace OpenNFS1
{
    internal static class GameConfig
    {
        public static string CdDataPath { get; set; }
    }
}

internal static class Program
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void WriteSample(BinaryWriter writer, int slot,
        int headerOffset, int dataOffset, byte bytesPerSample, byte channels,
        int frames, int loopStart, int loopLength, byte[] pcm)
    {
        writer.BaseStream.Position = slot * 4;
        writer.Write(headerOffset);
        writer.BaseStream.Position = headerOffset + 0x28;
        writer.Write(new [] { (byte)'E', (byte)'A', (byte)'C', (byte)'S' });
        writer.Write(16000);
        writer.Write(bytesPerSample);
        writer.Write(channels);
        writer.Write((byte)0);
        writer.Write((byte)255);
        writer.Write(frames);
        writer.Write(loopStart);
        writer.Write(loopLength);
        writer.Write(dataOffset);
        writer.Write(0);
        writer.BaseStream.Position = dataOffset;
        writer.Write(pcm);
    }

    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "NfsAudioTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(root, "Simdata", "Soundbnk", "COLL_SW.BNK");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = File.Create(path))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new byte[4096]);
                WriteSample(writer, 33, 512, 1400, 2, 1, 8, 0, 0,
                    new byte[16] {1,0,2,0,3,0,4,0,5,0,6,0,7,0,8,0});
                WriteSample(writer, 42, 584, 1700, 2, 1, 8, 2, 4,
                    new byte[16] {11,0,12,0,13,0,14,0,15,0,16,0,17,0,18,0});
                WriteSample(writer, 43, 656, 4092, 2, 1, 8, 2, 4, new byte[4]);
                WriteSample(writer, 1, 728, 2200, 1, 2, 4, 1, 2,
                    new byte[8] {255,0,1,128,127,129,0,127});
            }
            OpenNFS1.GameConfig.CdDataPath = root;
            var bank = new BnkFile("COLL_SW.BNK");

            var tire = bank.GetBySlot(42);
            Check(tire != null && tire.Slot == 42, "Lost canonical original tire sample slot.");
            Check(tire.PCMData.Length == 8, "Loop region did not use four 16-bit frames.");
            Check(tire.FullPCMData.Length == 16, "Missing original whole-sound attack.");
            Check(BitConverter.ToInt16(tire.PCMData, 0) == 13,
                "Loop sample did not begin at original frame 2.");
            Check(BitConverter.ToInt16(tire.FullPCMData, 0) == 11,
                "Whole sound should begin at original first frame.");
            Check(bank.GetBySlot(33) != null && bank.GetBySlot(33).FullPCMData.Length == 16,
                "Lost one-shot collision sound.");
            Check(bank.GetBySlot(43) == null, "Malformed alias must be rejected individually.");
            Check(bank.Samples.Count == 3, "Malformed entry discarded the entire sound bank.");
            var engine = bank.GetBySlot(1);
            Check(engine != null && engine.NbrChannels == 2, "Original stereo was not preserved.");
            Check(engine.FullPCMData.Length == 16 && engine.PCMData.Length == 8,
                "Stereo 8-bit conversion incorrectly sized.");
            Check(BitConverter.ToInt16(engine.FullPCMData, 0) == -256,
                "Signed original 8-bit sample conversion failed.");

            Console.WriteLine("PASS: original BNK source slots, skid slot 42 loop, full attack, stereo conversion and bad-alias recovery.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
