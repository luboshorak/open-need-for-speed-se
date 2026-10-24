using System;
using System.Collections.Generic;
using System.IO;

namespace OpenNFS1.Parsers
{
    /// <summary>
    /// EA 30FB/32FB/34FB canonical Huffman streams used in original NFS SE
    /// *BST.QFS leaderboard artwork. Unlike RefPack (10FB), these streams
    /// are bit-oriented; 32FB adds a byte-wise delta filter on output.
    /// </summary>
    internal static class EaHuffmanDecoder
    {
        private sealed class BitReader
        {
            private readonly byte[] _input;
            private int _bit;
            public BitReader(byte[] input) { _input = input; }
            public int Read(int count)
            {
                if (count < 0 || count > 24 || (long)_bit + count > (long)_input.Length * 8)
                    throw new InvalidDataException("Truncated EA Huffman bitstream.");
                int result = 0;
                for (int i = 0; i < count; i++, _bit++)
                    result = (result << 1) |
                        ((_input[_bit >> 3] >> (7 - (_bit & 7))) & 1);
                return result;
            }
            public int ReadVariable()
            {
                int width = 2;
                while (Read(1) == 0)
                {
                    width++;
                    if (width > 16)
                        throw new InvalidDataException("Invalid EA Huffman variable code.");
                }
                return Read(width) + (1 << width) - 4;
            }
        }

        public static bool CanDecode(byte[] data) =>
            data != null && data.Length >= 8 && data[1] == 0xFB &&
            (data[0] == 0x30 || data[0] == 0x32 || data[0] == 0x34 ||
             data[0] == 0x31 || data[0] == 0x33 || data[0] == 0x35);

        public static byte[] Decode(byte[] input)
        {
            if (!CanDecode(input))
                throw new InvalidDataException("Unsupported EA Huffman compression header.");

            var bits = new BitReader(input);
            int method = bits.Read(16);
            if ((method & 0x100) != 0) bits.Read(24);
            int outputSize = bits.Read(24);
            if (outputSize <= 0 || outputSize > 32 * 1024 * 1024)
                throw new InvalidDataException("Invalid EA Huffman output length.");

            int escape = bits.Read(8);
            var counts = new List<int>();
            int codeSum = 0;
            while (true)
            {
                if (counts.Count >= 16)
                    throw new InvalidDataException("Invalid Huffman code lengths.");
                int count = bits.ReadVariable();
                counts.Add(count);
                codeSum = codeSum * 2 + count;
                if (codeSum > (1 << counts.Count))
                    throw new InvalidDataException("Oversubscribed Huffman dictionary.");
                if (codeSum == (1 << counts.Count)) break;
            }

            int total = 0;
            foreach (int count in counts) total += count;
            if (total < 1 || total > 256)
                throw new InvalidDataException("Invalid number of Huffman symbols.");

            // EA codes describe a permutation of 0..255 via distances
            // to the next unassigned symbol, wrapping at byte 255.
            var symbols = new byte[total];
            var assigned = new bool[256];
            int cursor = 255;
            for (int i = 0; i < total; i++)
            {
                int remaining = bits.ReadVariable() + 1;
                while (remaining > 0)
                {
                    cursor = (cursor + 1) & 255;
                    if (!assigned[cursor]) remaining--;
                }
                assigned[cursor] = true;
                symbols[i] = (byte)cursor;
            }

            // Canonical Huffman keys: length in high 16 bits, prefix below.
            var dictionary = new Dictionary<int, byte>();
            int code = 0, symbolIndex = 0;
            for (int width = 1; width <= counts.Count; width++)
            {
                for (int i = 0; i < counts[width - 1]; i++)
                {
                    dictionary[(width << 16) | (code >> (16 - width))] =
                        symbols[symbolIndex++];
                    code += 1 << (16 - width);
                }
            }

            byte[] result = new byte[outputSize];
            int output = 0;
            while (output < outputSize)
            {
                int prefix = 0, value = -1;
                for (int width = 1; width <= counts.Count; width++)
                {
                    prefix = (prefix << 1) | bits.Read(1);
                    if (dictionary.TryGetValue((width << 16) | prefix, out byte symbol))
                    {
                        value = symbol;
                        break;
                    }
                }
                if (value < 0)
                    throw new InvalidDataException("Unknown EA Huffman code.");

                if (value != escape)
                {
                    result[output++] = (byte)value;
                }
                else
                {
                    int length = bits.ReadVariable();
                    if (length == 0)
                    {
                        if (bits.Read(1) == 1)
                            throw new InvalidDataException("Premature Huffman end marker.");
                        result[output++] = (byte)bits.Read(8);
                    }
                    else
                    {
                        if (output == 0 || length > outputSize - output)
                            throw new InvalidDataException("Invalid Huffman run length.");
                        byte previous = result[output - 1];
                        for (int i = 0; i < length; i++)
                            result[output++] = previous;
                    }
                }
            }

            if ((method & 0xFEFF) == 0x32FB)
            {
                byte accumulator = 0;
                for (int i = 0; i < result.Length; i++)
                {
                    unchecked { accumulator += result[i]; }
                    result[i] = accumulator;
                }
            }
            else if ((method & 0xFEFF) == 0x34FB)
            {
                byte first = 0, second = 0;
                for (int i = 0; i < result.Length; i++)
                {
                    unchecked { first += result[i]; second += first; }
                    result[i] = second;
                }
            }
            return result;
        }
    }
}
