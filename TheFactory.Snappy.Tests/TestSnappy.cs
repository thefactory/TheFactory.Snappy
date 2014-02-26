using NUnit.Framework;
using System;
using System.IO;

namespace TheFactory.Snappy.Tests {

    [TestFixture()]
    public class TestSnappy {
        string TestFile(string relPath) {
            var here = Path.GetDirectoryName(this.GetType().Assembly.Location);
            return Path.Combine(Path.Combine(here, "../../test-data"), relPath);
        }

        [Test()]
        public void TestEmpty() {
            byte[] data = new byte[] { };

            var comp = SnappyEncoder.Encode(data);
            AssertArraysEqual(new byte[] { 0x00 }, comp);
        }

        [Test()]
        public void TestEmptyRoundtrip() {
            Roundtrip(new byte[]{ });
        }

        [Test()]
        public void TestGoldEncodes() {
            var tests = new string[][] {
                new string[]{"upstream/alice29.txt", "gold/alice29.txt.gold"},
                new string[]{"upstream/asyoulik.txt", "gold/asyoulik.txt.gold"},
                new string[]{"upstream/fireworks.jpeg", "gold/fireworks.jpeg.gold"},
                new string[]{"upstream/geo.protodata", "gold/geo.protodata.gold"},
                new string[]{"upstream/html", "gold/html.gold"},
                new string[]{"upstream/html_x_4", "gold/html_x_4.gold"},
                new string[]{"upstream/kppkn.gtb", "gold/kppkn.gtb.gold"},
                new string[]{"upstream/lcet10.txt", "gold/lcet10.txt.gold"},
                new string[]{"upstream/paper-100k.pdf", "gold/paper-100k.pdf.gold"},
                new string[]{"upstream/plrabn12.txt", "gold/plrabn12.txt.gold"},
                new string[]{"upstream/urls.10K", "gold/urls.10K.gold"},
            };

            foreach (var test in tests) {
                var data = ReadAll(TestFile(test[0]));
                var gold = ReadAll(TestFile(test[1]));

                var comp = SnappyEncoder.Encode(data);

                Assert.AreEqual(gold.Length, comp.Length);
                AssertArraysEqual(gold, comp);

                Roundtrip(data);
            }
        }

        [Test()]
        public void TestSmallRand() {
            var rand = new Random(27354294);
            for (int n = 1; n < 20000; n += 23) {
                var b = new byte[n];
                rand.NextBytes(b);
                Roundtrip(b);
            }
        }

        [Test()]
        public void TestSmallRegular() {
            for (int n = 1; n < 20000; n += 23) {
                var b = new byte[n];
                for (int i = 0; i < b.Length; i++) {
                    b[i] = (byte)(i % 10 + 'a');
                }

                Roundtrip(b);
            }
        }

        void Roundtrip(byte[] data) {
            var comp = SnappyEncoder.Encode(data);
            var decomp = SnappyDecoder.Decode(comp);

            Assert.AreEqual(data.Length, decomp.Length);
        }

        [Test()]
        public void TestSimple() {
            byte[] data = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            };

            var comp = SnappyEncoder.Encode(data);
            AssertArraysEqual(new byte[] {
                0x12, 0x20, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x15, 0x09
            }, comp);

            Roundtrip(data);
        }

        void AssertArraysEqual(byte[] a, byte[] b) {
            // Intentionally don't check for length equality here, so we can get
            // the differing index for debugging.
            int len = a.Length < b.Length ? a.Length : b.Length;
            for (int i=0; i<len; i++) {
                if (a[i] != b[i]) {
                    Assert.AreEqual(a[i], b[i], String.Format("Arrays differ at {0}", i));
                }
            }
        }

        byte[] ReadAll(string file) {
            var r = new FileStream(file, FileMode.Open);
            var buf = new byte[r.Length];
            r.Read(buf, 0, buf.Length);
            return buf;
        }
    }
}

