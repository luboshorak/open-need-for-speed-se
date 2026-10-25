using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace OpenNFS1.Parsers.Audio
{
	class BnkSample
	{
        public string SourceId;
        public int Slot;
        public byte[] FullPCMData;
		public int NbrSamples;
		public Int16 NbrChannels;
		public byte[] PCMData;
		public int SampleRate;
		public int LoopStart;
	}

	// Bnk files (in SOUNDBNK folder) contain audio samples
	class BnkFile
	{
		string _filename;
		public bool ForceMono = false;
		public List<BnkSample> Samples { get; private set; }
        public BnkSample GetBySlot(int slot) => Samples.Find(s => s.Slot == slot);

		public BnkFile(string filename)
		{
			_filename = filename;
			Samples = new List<BnkSample>();
			filename = Path.Combine(GameConfig.CdDataPath, @"Simdata\Soundbnk\" + filename);
			BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open));
			Parse(reader);
			reader.Close();
		}

		private void Parse(BinaryReader reader)
		{
			List<(int Slot, int Offset)> sampleOffsets = new List<(int Slot, int Offset)>();
			for (int i = 0; i < 128; i++)
			{
				int sampleOffset = reader.ReadInt32();
				if (sampleOffset != 0)
					sampleOffsets.Add((i, sampleOffset));
			}

			int count = 0;
			foreach (var sampleRef in sampleOffsets)
			{
                try
                {
                    reader.BaseStream.Position = sampleRef.Offset;
                    ReadSample(reader, sampleRef.Slot);
                }
                catch (InvalidDataException exception)
                {
                    // Original CD banks contain a few malformed alias entries
                    // with an incorrect sample-byte depth (e.g. COLL_SB slots
                    // 43/45/56/58). Keep the other real sounds playable.
                    Trace.WriteLine($"Invalid original BNK slot {_filename}:{sampleRef.Slot}: {exception.Message}");
                }
			}
		}

		private void ReadSample(BinaryReader reader, int sourceSlot)
		{
			BnkSample sample = new BnkSample();
            sample.Slot = sourceSlot;
			reader.BaseStream.Position += 0x28;
			string id = new string(reader.ReadChars(4));
            sample.SourceId = id;
			sample.SampleRate = reader.ReadInt32();
			byte bytesPerSample = reader.ReadByte();
			sample.NbrChannels = reader.ReadByte();
			byte compression = reader.ReadByte();
			byte type = reader.ReadByte();

			int totalLength = reader.ReadInt32();
			sample.LoopStart = reader.ReadInt32();
			int loopLength = reader.ReadInt32();
			int dataOffset = reader.ReadInt32();
			int unk3 = reader.ReadInt32();

			// HACKS to avoid popping sounds.. Not sure why we need to do this.. maybe NFS finds values near
			// loop start/end which are the best match
			//if (_filename == "SUPRA_SW.bnk" && sampleIndex == 0)
			//	loopLength += 10;

			//Debug.WriteLine(String.Format("{0}: sampleRate:{5} bps: {1} loopStart: {2} loopLen: {3} totalLen: {4}, offset: {6}, channels: {7}",
			//	headerIndex, bytesPerSample, sample.LoopStart, loopLength, sample.NbrSamples, sample.SampleRate, dataOffset, sample.NbrChannels));

			            // BNK contains a full source sound AND a loop segment. Event
            // samples (shifts, collision, horn) must use the entire sound;
            // looped motors / rolling noise use the provided loop region.
            // The old parser discarded the attack of EVERY sound and even
            // read out of bounds if a source encoded a bad loop length.
            int channelCount = sample.NbrChannels;
            long fullBytes = (long)totalLength * bytesPerSample * channelCount;
            if (totalLength <= 0 || fullBytes <= 0 || fullBytes > 60 * 1024 * 1024 ||
                dataOffset < 0 || dataOffset + fullBytes > reader.BaseStream.Length)
                throw new InvalidDataException($"Invalid BNK sample at slot {sourceSlot}.");
            reader.BaseStream.Position = dataOffset;
            byte[] fullData = reader.ReadBytes((int)fullBytes);

            int safeLoopStart = Math.Clamp(sample.LoopStart, 0, totalLength);
            int safeLoopLength = loopLength <= 0 ? totalLength : Math.Min(loopLength,
                Math.Max(0, totalLength - safeLoopStart));
            if (safeLoopLength <= 0)
            {
                safeLoopStart = 0;
                safeLoopLength = totalLength;
            }
            int startBytes = safeLoopStart * channelCount * bytesPerSample;
            int countBytes = safeLoopLength * channelCount * bytesPerSample;
            byte[] loopData = new byte[countBytes];
            Buffer.BlockCopy(fullData, startBytes, loopData, 0, countBytes);
            sample.NbrSamples = safeLoopLength;

            byte[] ToMono(byte[] input)
            {
                if (!ForceMono || channelCount != 2)
                    return input;
                byte[] mono = new byte[input.Length / 2];
                int frameBytes = bytesPerSample * 2;
                for (int frame = 0; frame < mono.Length / bytesPerSample; frame++)
                    Buffer.BlockCopy(input, frame * frameBytes, mono,
                        frame * bytesPerSample, bytesPerSample);
                return mono;
            }

            byte[] ConvertPcm(byte[] input)
            {
                if (bytesPerSample == 2) return input;
                if (bytesPerSample != 1)
                    throw new InvalidDataException("Unsupported BNK sample bit depth");
                byte[] pcm16 = new byte[input.Length * 2];
                for (int i = 0; i < input.Length; i++)
                {
                    short value = (short)(unchecked((sbyte)input[i]) << 8);
                    pcm16[i * 2] = (byte)value;
                    pcm16[i * 2 + 1] = (byte)(value >> 8);
                }
                return pcm16;
            }

            sample.FullPCMData = ConvertPcm(ToMono(fullData));
            sample.PCMData = ConvertPcm(ToMono(loopData));
            if (ForceMono && channelCount == 2)
                sample.NbrChannels = 1;
			Samples.Add(sample);
			
			//WavWriter.Write(String.Format("c:\\temp\\test{0}.wav", headerIndex),
			//	nbrSamples, channels, soundData);
			
		}
	}
}
