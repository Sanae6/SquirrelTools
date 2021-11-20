using System;
using System.Collections.Generic;
using System.Linq;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Graphing {
    public class ControlFlowGraph {
        public ControlFlowGraph? Parent;
        public FunctionPrototype Prototype;
        public Block Root;
        public List<Block> UniqueBlocks = new List<Block>();
        public Dictionary<int, Block> Blocks = new Dictionary<int, Block>();
        public List<(int, ControlFlowGraph)> Closures = new List<(int, ControlFlowGraph)>();

        public ControlFlowGraph(FunctionPrototype prototype, ControlFlowGraph parent = null!) {
            Parent = parent;
            Prototype = prototype;
            Root = NewBlock(0, 0);
        }

        public Block NewBlock(int first, int last = -1) {
            Block block = Blocks[first] = new Block(this, first, last);
            UniqueBlocks.Add(block);
            return block;
        }

        public bool HasBlock(int ip) => Blocks.ContainsKey(ip);
        public Block GetBlock(int ip) => Blocks[ip];
        // public bool HasBlock(int ip) => Blocks.Count(kvp => kvp.Value.FirstIndex <= ip && ip <= kvp.Value.LastIndex) == 1;
        public Block GetBlockIncluded(int ip) => Blocks.FirstOrDefault(kvp => kvp.Value.FirstIndex <= ip && ip <= kvp.Value.LastIndex).Value;

        public Block GetBlock(int ip, int current) {
            if (HasBlock(ip)) return GetBlock(ip);
            if (ip <= current) {
                Block splitBlock = null!;
                foreach ((int address, Block block) in Blocks) {
                    if (address < ip && (splitBlock == null! || address > splitBlock.FirstIndex)) splitBlock = block;
                }

                Block retBlock = NewBlock(ip, splitBlock.LastIndex);
                retBlock.Next = splitBlock.Next;
                retBlock.Branch = splitBlock.Branch;
                splitBlock.LastIndex = ip - 1;
                splitBlock.Next = retBlock;
                splitBlock.Branch = null;
                foreach ((int address, var subGraph) in splitBlock.Closures) {
                    if (address < ip) continue;
                    splitBlock.Closures.Remove(address, out _);
                    retBlock.Closures.Add(address, subGraph);
                }
                return retBlock;
            }

            return NewBlock(ip);
        }

        public override string ToString() {
            return Prototype.Name.ToString();
        }
    }

    public class Block {
        public Block(ControlFlowGraph owner, int first, int last) {
            Owner = owner;
            FirstIndex = first;
            LastIndex = last;
        }

        public readonly ControlFlowGraph Owner;
        public FunctionPrototype Prototype => Owner.Prototype;
        public Dictionary<int, ControlFlowGraph> Closures = new Dictionary<int, ControlFlowGraph>();
        public HashSet<Block> Parents = new HashSet<Block>();
        public FunctionPrototype.Instruction[] Instructions => Prototype.Instructions[FirstIndex..(LastIndex + 1)];
        public int? NextIndex => next?.FirstIndex;
        public int? BranchIndex => branch?.FirstIndex;
        private Block? next;
        private Block? branch;
        public Block? Next {
            get => next;
            set {
                if (value == null) next?.Parents.Remove(this);
                else {
                    next?.Parents.Remove(this);
                    value.Parents.Add(this);
                }
                next = value;
            }
        }
        public Block? Branch {
            get => branch;
            set {
                if (value == null) branch?.Parents.Remove(this);
                else {
                    branch?.Parents.Remove(this);
                    value.Parents.Add(this);
                }
                branch = value;
            }
        }
        public int FirstIndex;
        public int LastIndex;
        public FunctionPrototype.Instruction First => Prototype.Instructions[FirstIndex];
        public FunctionPrototype.Instruction Last => Prototype.Instructions[LastIndex];
    }
}