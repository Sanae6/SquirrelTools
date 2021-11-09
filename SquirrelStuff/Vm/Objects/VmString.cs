using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Vm.Objects {
    public class VmString : VmObject {
        public string Value { get; }

        public VmString(string value) {
            Value = value;
        }

        public override ObjectType Type => ObjectType.OtString;

        public override VmObject? Cast(ObjectType newType) {
            throw new System.NotImplementedException();
        }

        public static implicit operator string(VmString obj) => obj.Value;
    }
}