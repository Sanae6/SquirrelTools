using System;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Vm.Objects {
    public abstract class VmObject {
        public abstract ObjectType Type { get; }

        public abstract VmObject? Cast(ObjectType newType);
        public static implicit operator VmObject(string value) => new VmString(value);
        public static implicit operator VmObject(VmNativeClosure.NativeClosure value) => new VmNativeClosure(value);
    }
}