using System;
using NUnit.Framework;

namespace TheFactory.Snappy.Tests {

    [TestFixture()]
    public class TestVarInt {
        void roundtrip(ulong x) {
            // Use some padding bytes to check nonzero offsets.
            var pad = 5;

            var buf = new byte[VarInt.MaxVarIntLen64 + pad];
            int n = VarInt.PutUvarInt(buf, pad, x);

            var ret = VarInt.UvarInt(buf, pad);
            Assert.AreEqual(n, ret.VarIntLength);
            Assert.AreEqual(x, ret.Value);
        }

        [Test()]
        public void TestUvarInt() {
            ulong[] tests = new ulong[] {
                0,
                1,
                2,
                10,
                20,
                63,
                64,
                65,
                127,
                128,
                129,
                255,
                256,
                257,
                1 << 63 - 1,
            };

            for (int i = 0; i < tests.Length; i++) {
                roundtrip(tests[i]);
            }
        }
    }
}

