using System;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Vm.Objects {
    public class VmClosure : VmObject {
        public FunctionPrototype Prototype;
        internal int InstructionPointer;
        internal VmTable LocalTable = new VmTable();
        public FunctionPrototype.Instruction[] Instructions => Prototype.Instructions;
        
        public override ObjectType Type => ObjectType.OtClosure;

        public override VmObject? Cast(ObjectType newType) {
            throw new NotImplementedException();
        }
    }
}