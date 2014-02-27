using System;
using System.IO;

namespace TheFactory.Snappy {

    public class SnappyEncoder {
        // Limit how far copy back-references can go, the same as the C++ code.
        const int MaxOffset = 1 << 15;

        public static int MaxEncodedLen(int srcLen) {
            return 32 + srcLen + srcLen / 6;
        }

        public static byte[] Encode(byte[] src) {
            return Encode(src, 0, src.Length);
        }

        public static byte[] Encode(byte[] src, int srcOff, int srcLen) {
            byte[] buf = new byte[MaxEncodedLen(srcLen)];
            var len = Encode(src, srcOff, srcLen, buf, 0, buf.Length);

            byte[] ret = new byte[len];
            Array.Copy(buf, ret, len);
            return ret;
        }

        public static int Encode(byte[] src, int srcOff, int srcLen, byte[] dst, int dstOff, int dstLen) {
            if (dstLen < MaxEncodedLen(srcLen)) {
                // Destination array doesn't have enough room for the encoded data.
                throw new IndexOutOfRangeException("snappy: destination array too short");
            }

            var d = new MemoryStream(dst, dstOff, dstLen);

            // The block starts with the varint-encoded length of the decompressed bytes.
            byte[] buf = new byte[VarInt.MaxVarIntLen32];
            int len = VarInt.PutUvarInt(buf, 0, (ulong)srcLen);
            d.Write(buf, 0, len);

            // Return early if src is short.
            if (srcLen <= 4) {
                if (srcLen != 0) {
                    EmitLiteral(d, src, srcOff, srcLen);
                    return (int)d.Position;
                }
            }

            // Initialize the hash table. Its size ranges from 1<<8 to 1<<14 inclusive.
            int maxTableSize = 1 << 14;
            int shift = 32-8;
            uint tableSize = 1<<8;
            while (tableSize < maxTableSize && tableSize < srcLen) {
                shift--;
                tableSize *= 2;
            }
            var table = new int[maxTableSize];

            // Iterate over the source bytes.
            int s = srcOff;
            int t = 0;
            int lit = srcOff;

            while (s + 3 < srcLen) {
                // Update the hash table.
                byte b0 = src[s];
                byte b1 = src[s + 1];
                byte b2 = src[s + 2];
                byte b3 = src[s + 3];
                uint h = (uint)b0 | ((uint)b1)<<8 | ((uint)b2)<<16 | ((uint)b3)<<24;
                var p = (h*0x1e35a7bd)>>shift;
                // We need to to store values in [-1, inf) in table. To save
                // some initialization time, (re)use the table's zero value
                // and shift the values against this zero: add 1 on writes,
                // subtract 1 on reads.
                t = table[p] - 1;
                table[p] = s + 1;

                // If t is invalid or src[s:s+4] differs from src[t:t+4],
                // accumulate a literal byte.
                if (t < 0 || s - t >= MaxOffset || b0 != src[t] || b1 != src[t + 1] || b2 != src[t + 2] || b3 != src[t + 3]) {
                    s++;
                    continue;
                }

                // Otherwise, we have a match. First, emit any pending literal bytes.
                if (lit != s) {
                    EmitLiteral(d, src, lit, s - lit);
                }

                // Extend the match to be as long as possible.
                int s0 = s;
                s = s + 4;
                t = t + 4;
                while (s < srcLen && src[s] == src[t]) {
                    s++;
                    t++;
                }

                // Emit the copied bytes.
                EmitCopy(d, s-t, s-s0+srcOff);
                lit = s;
            }

            // Emit any final pending literal bytes and return.
            if (lit != srcLen) {
                EmitLiteral(d, src, lit, srcLen - lit);
            }

            return (int)d.Position;
        }

        static int EmitLiteral(Stream dst, byte[] lit, int litOff, int litLen) {
            int i = 0;
            uint n = (uint)(litLen - 1);

            if (n < 60) {
                dst.WriteByte((byte)(n << 2 | SnappyTag.Literal));
                i = 1;
            } else if (n < (1 << 8)) {
                dst.WriteByte(60 << 2 | SnappyTag.Literal);
                dst.WriteByte((byte)n);
                i = 2;
            } else if (n < (1 << 16)) {
                dst.WriteByte(61 << 2 | SnappyTag.Literal);
                dst.WriteByte((byte)n);
                dst.WriteByte((byte)(n >> 8));
                i = 3;
            } else if (n < (1 << 24)) {
                dst.WriteByte(62 << 2 | SnappyTag.Literal);
                dst.WriteByte((byte)n);
                dst.WriteByte((byte)(n >> 8));
                dst.WriteByte((byte)(n >> 16));
                i = 4;
            } else if ((Int64)n < 1 << 32) {
                dst.WriteByte(63 << 2 | SnappyTag.Literal);
                dst.WriteByte((byte)n);
                dst.WriteByte((byte)(n >> 8));
                dst.WriteByte((byte)(n >> 16));
                dst.WriteByte((byte)(n >> 24));
                i = 5;
            } else {
                throw new IndexOutOfRangeException("snappy: source buffer is too long");
            }
            dst.Write(lit, litOff, litLen);
            return i + litLen;
        }

        static int EmitCopy(Stream dst, int offset, int length) {
            int i = 0;
            while (length > 0) {
                int x = length - 4;
                if ((0 <= x) && (x < 1<<3) && (offset < 1<<11)) {
                    dst.WriteByte((byte)((((offset >> 8) & 0x07) << 5) | (byte)(x << 2) | SnappyTag.Copy1));
                    dst.WriteByte((byte)offset);
                    i += 2;
                    break;
                }

                x = length;
                if (x > 1<<6) {
                    x = 1 << 6;
                }

                dst.WriteByte((byte)((byte)(x-1) << 2 | SnappyTag.Copy2));
                dst.WriteByte((byte)offset);
                dst.WriteByte((byte)(offset >> 8));
                i += 3;
                length -= x;
            }
            return i;
        }
    }
}

