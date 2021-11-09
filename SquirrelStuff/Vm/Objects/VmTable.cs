using System.Collections.Generic;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Vm.Objects {
    public class VmTable : VmObject {
        public Dictionary<VmObject, VmObject> Data = new Dictionary<VmObject, VmObject>();

        public override ObjectType Type => ObjectType.OtTable;

        public override VmObject? Cast(ObjectType newType) {
            return null;
        }
    }
}