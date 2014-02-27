using System;

namespace TheFactory.Snappy {

    public class SnappyDecoder {
        public static byte[] Decode(byte[] src) {
            return Decode(src, 0, src.Length);
        }

        public static byte[] Decode(byte[] src, int srcOff, int srcLen) {
            var len = VarInt.UvarInt(src, srcOff);
            byte[] ret = new byte[len.Value];

            Decode(src, srcOff, srcLen, ret, 0, ret.Length);
            return ret;
        }

        public static int Decode(byte[] src, int srcOff, int srcLen, byte[] dst, int dstOff, int dstLen) {
            var len = VarInt.UvarInt(src, srcOff);
            if (dstLen < (int)len.Value) {
                // Destination array doesn't have enough room for the decoded data.
                throw new IndexOutOfRangeException("snappy: destination array too short");
            }

            int s = srcOff + len.VarIntLength;
            int d = dstOff;
            int offset = 0;
            int length = 0;

            while (s < srcOff + srcLen) {
                byte tag = (byte)(src[s] & 0x03);
                if (tag == SnappyTag.Literal) {
                    uint x = (uint)(src[s] >> 2);
                    if (x < 60) {
                        s += 1;
                    } else if (x == 60) {
                        s += 2;
                        if (s > srcLen) {
                            throw new IndexOutOfRangeException("snappy: corrupt input");
                        }
                        x = (uint)src[s - 1];
                    } else if (x == 61) {
                        s += 3;
                        if (s > srcLen) {
                            throw new IndexOutOfRangeException("snappy: corrupt input");
                        }
                        x = (uint)src[s - 2] | (uint)src[s - 1] << 8;
                    } else if (x == 62) {
                        s += 4;
                        if (s > srcLen) {
                            throw new IndexOutOfRangeException("snappy: corrupt input");
                        }
                        x = (uint)src[s - 3] | (uint)src[s - 2] << 8 | (uint)src[s - 1] << 16;
                    } else if (x == 63) {
                        s += 5;
                        if (s > srcLen) {
                            throw new IndexOutOfRangeException("snappy: corrupt input");
                        }
                        x = (uint)src[s - 4] | (uint)src[s - 3] << 8 | (uint)src[s - 2] << 16 | (uint)src[s - 1] << 24;
                    }
                    length = (int)(x + 1);
                    if (length <= 0) {
                        throw new IndexOutOfRangeException("snappy: unsupported literal length");
                    }
                    if (length > dstLen - d || length > srcOff + srcLen - s) {
                        throw new IndexOutOfRangeException("snappy: corrupt input");
                    }
                    Array.Copy(src, s, dst, d, length);
                    d += length;
                    s += length;
                    continue;
                } else if (tag == SnappyTag.Copy1) {
                    s += 2;
                    if (s > srcOff + srcLen) {
                        throw new IndexOutOfRangeException("snappy: corrupt input");
                    }
                    length = 4 + (((int)src[s - 2] >> 2) & 0x7);
                    offset = ((int)src[s - 2] & 0xe0) << 3 | (int)src[s - 1];
                } else if (tag == SnappyTag.Copy2) {
                    s += 3;
                    if (s > srcOff + srcLen) {
                        throw new IndexOutOfRangeException("snappy: corrupt input");
                    }
                    length = 1 + ((int)src[s - 3] >> 2);
                    offset = (int)src[s - 2] | (int)src[s - 1] << 8;
                } else if (tag == SnappyTag.Copy4) {
                    throw new NotSupportedException("snappy: unsupported COPY_4 tag");
                }

                int end = d + length;
                if (offset > d || end > dstLen) {
                    throw new IndexOutOfRangeException("snappy: corrupt input");
                }

                for (; d < end; d++) {
                    dst[d] = dst[d - offset];
                }
            }

            return d;
        }
    }
}

