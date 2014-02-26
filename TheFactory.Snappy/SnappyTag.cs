using System;

namespace TheFactory.Snappy {
    // This is a line-for-line port of the snappy-go project.

    public class SnappyTag {
        public const byte Literal = 0x00;
        public const byte Copy1 = 0x01;
        public const byte Copy2 = 0x02;
        public const byte Copy4 = 0x03;
    }

}

