using System;
using System.IO;

namespace TheFactory.Snappy {

    public class VarInt {
        public const int MaxVarIntLen16 = 3;
        public const int MaxVarIntLen32 = 5;
        public const int MaxVarIntLen64 = 10;

        public class UvarIntRet {
            public ulong Value;
            public int VarIntLength;

            public UvarIntRet(ulong val, int valLen) {
                this.Value = val;
                this.VarIntLength = valLen;
            }
        }

        public static UvarIntRet UvarInt(byte[] src, int srcOff) {
            ulong x = 0;
            int s = 0;
            for (int i = 0; i < src.Length - srcOff; i++) {
                byte b = src[srcOff + i];
                if (b < 0x80) {
                    if (i > 9 || i == 9 && b > 1) {
                        return new UvarIntRet(0, -(i + 1));
                    }
                    return new UvarIntRet(x | ((ulong)b) << s, i + 1);
                }
                x |= (ulong)(b & 0x7f) << s;
                s += 7;
            }

            return new UvarIntRet(0, 0);
        }

        public static int PutUvarInt(byte[] buf, int offset, ulong x) {
            var i = 0;
            while (x >= 0x80) {
                buf[i + offset] = (byte)(x | 0x80);
                x >>= 7;
                i++;
            }
            buf[i + offset] = (byte)x;
            return i + 1;
        }
    }
}

