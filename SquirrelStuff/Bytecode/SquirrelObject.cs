using System.Globalization;

namespace SquirrelStuff.Bytecode {
    public class SquirrelObject {
        public ObjectType Type;
        public object? Value;

        public SquirrelObject(ObjectType type, object? value) {
            Type = type;
            Value = value;
        }

        public override string ToString() {
            return ToString(true);
        }

        public virtual string ToString(bool raw) {
            return Type switch {
                ObjectType.OtString => raw ? (string) Value! : $"\"{(string) Value!}\"",
                ObjectType.OtInteger => ((int) Value!).ToString(),
                ObjectType.OtBool => (bool) Value! ? "true" : "false",
                ObjectType.OtFloat => ((float) Value!).ToString("F"),
                ObjectType.OtNull => "Null",
                _ => "UNKNOWN_OBJECT_TYPE"
            };
        }
    }
}