using System.Collections.Generic;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Analysis {
    public class DecompObject : SquirrelObject {
        public DecompObject(ObjectType type, object? value) : base(type, value) { }

        public static DecompObject From(SquirrelObject obj) => obj as DecompObject ?? new DecompObject(obj.Type, obj.Value);

        public static DecompObject Null() => new DecompObject(ObjectType.OtNull, null);
        public static DecompObject String(string value) => new DecompObject(ObjectType.OtString, value);
        public static DecompObject Bool(bool value) => new DecompObject(ObjectType.OtBool, value);
        public static DecompObject Int(int value) => new DecompObject(ObjectType.OtInteger, value);
        public static DecompObject Float(float value) => new DecompObject(ObjectType.OtFloat, value);
        public static DecompObject Table(Dictionary<DecompObject, DecompObject> value) => new DecompObject(ObjectType.OtTable, value);
        public static DecompObject Closure(FunctionPrototype value) => new DecompObject(ObjectType.OtClosure, value); 
        public static DecompObject RootTable { get; } = new RootTable();
    }

    public class RootTable : DecompObject {
        public RootTable() : base(ObjectType.OtTable, null) {}
        public override string ToString(bool raw) {
            return "::"; // root table prefix
        }
    }
}