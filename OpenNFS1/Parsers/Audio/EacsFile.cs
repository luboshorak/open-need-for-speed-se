using System;
using System.IO;
using System.Text;

namespace OpenNFS1.Parsers.Audio
{
    internal static class EacsFile
    {
        private static readonly int[] Steps = {
            7,8,9,10,11,12,13,14,16,17,19,21,23,25,28,31,34,37,41,45,50,55,60,66,
            73,80,88,97,107,118,130,143,157,173,190,209,230,253,279,307,337,371,408,
            449,494,544,598,658,724,796,876,963,1060,1166,1282,1411,1552,1707,1878,
            2066,2272,2499,2749,3024,3327,3660,4026,4428,4871,5358,5894,6484,7132,
            7845,8630,9493,10442,11487,12635,13899,15289,16818,18500,20350,22385,
            24623,27086,29794,32767 };
        private static readonly int[] IndexDelta = { -1,-1,-1,-1,2,4,6,8 };
        public static BnkSample Read(string path) => Decode(File.ReadAllBytes(path));
        public static BnkSample Decode(byte[] data)
        {
            if (data.Length < 32 || Encoding.ASCII.GetString(data, 0, 4) != "EACS")
                throw new InvalidDataException("Missing standalone EACS header.");
            int rate = BitConverter.ToInt32(data, 4), bytes = data[8], channels = data[9], compression = data[10];
            int frames = BitConverter.ToInt32(data, 12), offset = BitConverter.ToInt32(data, 24);
            if (rate < 8000 || rate > 48000 || channels < 1 || channels > 2 ||
                bytes < 1 || bytes > 2 || frames <= 0 || frames > rate * 120 || offset < 32 ||
                (compression != 0 && compression != 2))
                throw new InvalidDataException("Unsupported EACS speech format.");
            int count = checked(frames * channels);
            long required = compression == 2 ? (count + 1L) / 2 : (long)count * bytes;
            if (offset + required > data.Length) throw new InvalidDataException("Truncated EACS speech.");
            var pcm = new byte[count * 2];
            int[] predictor = new int[channels], index = new int[channels];
            for (int i = 0; i < count; i++)
            {
                int value;
                if (compression == 0)
                    value = bytes == 2 ? BitConverter.ToInt16(data, offset + i * 2) : unchecked((sbyte)data[offset + i]) << 8;
                else
                {
                    // Standalone EA IMA starts from zero; high nibble precedes low.
                    int nibble = (data[offset + i / 2] >> (i % 2 == 0 ? 4 : 0)) & 15;
                    int ch = i % channels, step = Steps[index[ch]];
                    int delta = (step >> 3) + ((nibble & 4) != 0 ? step : 0) +
                        ((nibble & 2) != 0 ? step >> 1 : 0) + ((nibble & 1) != 0 ? step >> 2 : 0);
                    predictor[ch] = Math.Clamp(predictor[ch] + ((nibble & 8) == 0 ? delta : -delta), -32768, 32767);
                    index[ch] = Math.Clamp(index[ch] + IndexDelta[nibble & 7], 0, 88);
                    value = predictor[ch];
                }
                pcm[i * 2] = (byte)value; pcm[i * 2 + 1] = (byte)(value >> 8);
            }
            return new BnkSample { SourceId = "EACS", SampleRate = rate, NbrChannels = (short)channels,
                FullPCMData = pcm, PCMData = pcm, NbrSamples = frames, LoopLength = frames, Gain = 1f };
        }
    }
}
