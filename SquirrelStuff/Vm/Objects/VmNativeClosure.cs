using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Vm.Objects {
    public class VmNativeClosure : VmObject {
        public VmNativeClosure(NativeClosure value) {
            Value = value;
        }

        public NativeClosure Value { get; }

        public delegate VmObject NativeClosure(Runner runner);
        public override ObjectType Type => ObjectType.OtNativeclosure;
        public override VmObject? Cast(ObjectType newType) {
            return null;
        }
    }
}