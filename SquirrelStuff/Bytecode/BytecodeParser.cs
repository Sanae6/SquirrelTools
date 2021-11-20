using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SquirrelStuff.Bytecode {
    public static class BytecodeParser {
        public const int Sqir = 0x53514952;
        public static FunctionPrototype Parse(BinaryReader reader) {
            if (reader.ReadUInt16() != 0xFAFA) {
                throw new Exception("Data provided is not a valid compiled Squirrel nut");
            }

            reader.AssertTag(Sqir, "SQIR tag");
            uint unused = reader.ReadUInt32(); // character size
            uint unused1 = reader.ReadUInt32(); // integer size
            uint unused2 = reader.ReadUInt32(); // float size
            return FunctionPrototype.ReadFunction(reader);
        }
    }

    public static class ParserExtensions {
        public static void AssertTag(this BinaryReader reader, int tag, string message = "") {
            Debug.Assert(reader.ReadInt32() == tag, message);
        }
        public static SquirrelObject ReadObject(this BinaryReader reader) {
            ObjectType type = (ObjectType) reader.ReadUInt32();
            return type switch {
                ObjectType.OtString => new SquirrelObject(type, Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()))),
                ObjectType.OtInteger => new SquirrelObject(type, reader.ReadInt32()),
                ObjectType.OtFloat => new SquirrelObject(type, reader.ReadSingle()),
                ObjectType.OtBool => new SquirrelObject(type, reader.ReadUInt32() != 0),
                ObjectType.OtNull => new SquirrelObject(type, null),
                _ => throw new Exception($"Invalid object type {type:X}")
            };
        }
    }
}