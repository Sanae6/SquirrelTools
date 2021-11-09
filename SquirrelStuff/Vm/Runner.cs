using System.Collections.Generic;
using SquirrelStuff.Vm.Objects;

namespace SquirrelStuff.Vm {
    public class Runner {
        public Stack<VmObject> VmObjects = new Stack<VmObject>(1024);
        
        public VmObject? Target = null;
        public VmTable RootTable = new VmTable();

        public int StackResolve(int location) => location < 0 ? VmObjects.Count - location : location - 1;

        public void Get(VmObject obj, VmObject key) {
            
        }

        public void Execute(VmClosure closure) {
            // closure.Instruction
            // closure.Instructions[]
        }
    }
}