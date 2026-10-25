using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Security.Cryptography;
using OpenNFS1.Audio;
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

    private static int Main(string[] args)
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
            // A shared read-only handle rejects a parser that requests write access.
            using var readOnly = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
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

            Check(tire.LoopStart == 2 && tire.LoopLength == 4, "Native loop metadata lost.");
            var forward = DrivingAudioState.Compute(60, true, 2, .7f, .2f, 0, true, false, false, 0);
            var reverse = DrivingAudioState.Compute(-60, true, 2, .7f, .2f, 0, true, false, false, 0);
            Check(forward == reverse && reverse.OffRoad > 0, "Reverse surface audio is silent.");
            Check(DrivingAudioState.Compute(80, false, 4, 1, 1, 1, false, true, true, 1) == default,
                "Airborne car makes tire/road noise.");
            Check(DrivingAudioState.Compute(0, true, 4, 1, 1, 1, false, true, false, 1) == default,
                "Stationary car makes surface noise.");
            Check(DrivingAudioState.Compute(80, true, 4, 1, 1, 1, false, true, true, 1).Skid == 0,
                "Asphalt squeal on four off-road tires.");
            Check(DrivingAudioState.Compute(80, true, 0, 0, 0, 1, true, false, false, 0).Skid == 0,
                "ABS braking falsely creates locked-wheel squeal.");
            Check(DrivingAudioState.Compute(80, true, 0, 0, 0, 1, false, false, false, 0).Skid > 0,
                "Locked-wheel braking is silent.");
            Check(DrivingAudioState.Compute(0, true, 0, 0, 0, 0, false, false, true, 0).Skid > 0,
                "Stationary burnout is silent.");
            readOnly.Dispose();
            if (args.Length > 0)
            {
                string originals = Path.Combine(root, "Original", "Simdata", "Soundbnk");
                Directory.CreateDirectory(originals);
                using var archive = ZipFile.OpenRead(args[0]);
                int banks = 0, samples = 0;
                foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(".bnk", StringComparison.OrdinalIgnoreCase)))
                {
                    string dest = Path.Combine(originals, entry.Name);
                    entry.ExtractToFile(dest);
                    OpenNFS1.GameConfig.CdDataPath = Path.Combine(root, "Original");
                    using var locked = new FileStream(dest, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var original = new BnkFile(entry.Name);
                    Check(original.Samples.Count > 0, "No samples decoded: " + entry.Name);
                    foreach (var sample in original.Samples)
                    {
                        Check(sample.FullPCMData.Length % (2 * sample.NbrChannels) == 0, "Unaligned PCM.");
                        Check(sample.LoopStart >= 0 && sample.LoopLength > 0 &&
                            (sample.LoopStart + sample.LoopLength) * sample.NbrChannels * 2 <= sample.FullPCMData.Length,
                            "Loop outside decoded audio: " + entry.Name);
                    }
                    banks++; samples += original.Samples.Count;
                }
                Console.WriteLine($"PASS: {banks} original banks, {samples} slots, read-only access and loop bounds.");
            }
            Check(OriginalAudioRules.CarImpact(20) == 33 && OriginalAudioRules.CarImpact(80) == 34 &&
                OriginalAudioRules.CarImpact(120) == 36, "Impact severity mapping is wrong.");
            Check(OriginalAudioRules.Gear == 32 && OriginalAudioRules.Landing == 37 && OriginalAudioRules.Wall == 35,
                "Gear/horn/landing routing regressed.");
            Check(OriginalAudioRules.Pitch(64,12) == 0 && OriginalAudioRules.Pitch(0,12) == -1,
                "BNK pitch reach is not converted to octaves.");
            Check(OriginalAudioRules.LapSpeech(false,3,3,false) == "FINALLAP.EAS" &&
                OriginalAudioRules.LapSpeech(false,3,3,true) == "BESTLAST.EAS" &&
                OriginalAudioRules.LapSpeech(false,2,3,true) == "BESTTIME.EAS" &&
                OriginalAudioRules.LapSpeech(true,2,3,true) == null &&
                OriginalAudioRules.LapSpeech(false,4,3,true) == null, "Speech race-event routing is wrong.");
            Check(OriginalAudioRules.RadarPeriod(8,false) < OriginalAudioRules.RadarPeriod(40,false) &&
                float.IsPositiveInfinity(OriginalAudioRules.RadarPeriod(80,false)), "Radar range/interval regressed.");
            Check(OriginalAudioRules.CollisionBank("tr7.tri") == "COLLSWMT.BNK" &&
                OriginalAudioRules.CollisionBank("al1.tri") == "COLLSWWT.BNK", "Track bank selection regressed.");
            if (args.Length > 2)
            {
                foreach (string line in File.ReadAllLines(args[2]))
                {
                    string[] fields = line.Split(' ');
                    var speech = EacsFile.Read(Path.Combine(args[1], fields[0]));
                    string hash = Convert.ToHexString(SHA256.HashData(speech.FullPCMData));
                    Check(hash == fields[1], "Independent IMA decoder mismatch for " + fields[0]);
                    Console.WriteLine("PASS: independent speech PCM reference " + fields[0]);
                }
            }
            bool invalidSpeech = false;
            try { EacsFile.Decode(new byte[32]); } catch (InvalidDataException) { invalidSpeech = true; }
            Check(invalidSpeech, "Invalid speech header accepted.");
            Console.WriteLine("PASS: impact, gear, road-bank, radar and lap-speech event rules.");
            Console.WriteLine("PASS: original BNK source slots, skid slot 42 loop, full attack, stereo conversion and bad-alias recovery, read-only sources and driving mix states.");
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
