using System;
using System.Collections.Generic;
using System.Linq;
using SquirrelStuff.Bytecode;

// most code is taken from https://github.com/krzys-h/UndertaleModTool/blob/master/UndertaleModLib/Decompiler/Decompiler.cs
// rewritten to work with squirrel bytecode

namespace SquirrelStuff.Graphing {
    public class GraphGenerator {
        // evil SQFuncState.AddInstruction()'s optimization code be like i make your code less optimal
        //todo actually bother to do this, need to do stack allocation just for this so not until i got proper decomp :)
        private static void DeoptimizeInstructions(FunctionPrototype prototype) {
            FunctionPrototype.Instruction[] instructions = prototype.Instructions;
            List<FunctionPrototype.Instruction> newInstructions = new List<FunctionPrototype.Instruction>();
            foreach (FunctionPrototype.Instruction inst in instructions) {
                switch (inst.Opcode) {
                    case Opcodes.JCmp:
                        newInstructions.Add(new FunctionPrototype.Instruction {
                            Opcode = Opcodes.Cmp,
                            Argument3 = inst.Argument3,
                            Argument2 = inst.Argument2,
                            Argument1 = inst.Argument0,
                        });
                        break;
                }
            }

            prototype.Instructions = newInstructions.ToArray();
        }

        public static ControlFlowGraph BuildControlFlowGraph(FunctionPrototype prototype) {
            ControlFlowGraph graph = new ControlFlowGraph(prototype);
            Block root = graph.Root;
            Block? current = root;
            for (int ip = 0; ip < prototype.Instructions.Length; ip++) {
                // Instruction? prev = ip > 0 ? prototype.Instructions[ip - 1] : null;
                FunctionPrototype.Instruction inst = prototype.Instructions[ip];
                FunctionPrototype.Instruction? next = ip < prototype.Instructions.Length - 1 ? prototype.Instructions[ip + 1] : null;

                if (graph.HasBlock(ip)) {
                    Block block = graph.GetBlock(ip);
                    if (current != null) {
                        current.Next = block;
                        current.Branch = null;
                    }

                    current = block;
                }

                if (current is null) current = graph.NewBlock(ip);
                current.LastIndex = ip;
                // Console.WriteLine($"{ip} - {current.FirstIndex}/{current.LastIndex}");

                // int arg1() => inst.Argument1

                switch (inst.Opcode) {
                    case Opcodes.Jmp: {
                        int addr = ip + inst.Argument1 + 1;
                        Block block = graph.GetBlock(addr, ip);
                        current.Next = block;
                        current.Branch = null;
                        // current.LastIndex = ip;
                        current = null;
                        break;
                    }
                    case Opcodes.JCmp:
                    case Opcodes.Jz: {
                        int addr = ip + inst.Argument1 + 1;
                        //next?.Opcode == Opcodes.Jmp ? addr + 1 : 
                        Block nextBlock = graph.GetBlock(ip + 1, ip);
                        Block branchBlock = graph.GetBlock(addr, ip);
                        current.Next = nextBlock;
                        current.Branch = branchBlock;
                        current = null;
                        break;
                    }
                    case Opcodes.Closure: // to see closure branches
                        ControlFlowGraph flowGraph = BuildControlFlowGraph(prototype.Functions[inst.Argument1]);
                        current.Closures.Add(ip, flowGraph);
                        graph.Closures.Add((ip, flowGraph));
                        break;
                }
            }

            //eliminate accidental circular block references
            foreach (Block block in graph.Blocks.Values.Distinct()) {
                if (block.Next == block) block.Next = null;
                if (block.Branch == block) block.Branch = null;
            }

            bool NotLastBlock(int last) =>/* last < prototype.Instructions.Length - 1 && */graph.HasBlock(last + 1);
            foreach (Block block in graph.Blocks.Values.Distinct()) {
                if (block.Next == null && NotLastBlock(block.LastIndex)) block.Next = graph.GetBlockIncluded(block.LastIndex + 1);
            }

            // foreach (Block block in graph.Blocks.Values.Distinct()) {
            //     Console.WriteLine($"[{block.FirstIndex}/{block.LastIndex}]");
            // }
            //
            // Console.WriteLine();
            // foreach (Block block in graph.Blocks.Values.Distinct()) {
            //     if (block.Next is { } next) Console.WriteLine($"[{block.FirstIndex}/{block.LastIndex}] n-> [{next.FirstIndex}/{next.LastIndex}]");
            //     if (block.Branch is { } branch) Console.WriteLine($"[{block.FirstIndex}/{block.LastIndex}] b-> [{branch.FirstIndex}/{branch.LastIndex}]");
            // }

            return graph;
        }

        public static Graph GenerateGraph(ControlFlowGraph graph) {
            Graph g = new Graph(graph.Prototype.Name.ToString());

            // Dictionary<Block, int> blockIndex = new Dictionary<Block, int>();
            HashSet<Block> AllBlocks = new HashSet<Block>();
            Dictionary<ControlFlowGraph, int> bases = new Dictionary<ControlFlowGraph, int>();

            int vertBase = 0;

            void SetBases((int Address, ControlFlowGraph Graph) flowGraph) {
                bases[flowGraph.Graph] = vertBase;
                vertBase += flowGraph.Graph.Prototype.Instructions.Length;
                foreach ((int, ControlFlowGraph) closure in flowGraph.Graph.Closures) SetBases(closure);
            }

            int GetIndex(Block block) => bases[block.Owner] + block.FirstIndex;

            SetBases((vertBase, graph));

            void AddBlockClosure(ControlFlowGraph curGraph) {
                // Console.WriteLine(curGraph.UniqueBlocks.Count);
                foreach (Block block in curGraph.UniqueBlocks.Where(block => AllBlocks.Add(block))) {
                    g.AddVertex(GetIndex(block), $"{(block.FirstIndex == 0 ? block.Prototype.Name.ToString(true) + "\\n" : "")}{block.FirstIndex}-{block.LastIndex}");
                }

                foreach ((int _, var closureGraph) in curGraph.Closures) {
                    AddBlockClosure(closureGraph);
                }
            }

            AddBlockClosure(graph);

            foreach (Block block in AllBlocks) {
                if (block.Next is not null) {
                    // Console.WriteLine($"{block.Owner} {GetIndex(block)}->{GetIndex(block.Next)}");
                    g.AddEdge(GetIndex(block), GetIndex(block.Next), $"{block.FirstIndex}->{block.Next.FirstIndex}");
                }

                if (block.Branch is not null) {
                    // Console.WriteLine($"{block.Owner} {GetIndex(block)}-b>{GetIndex(block.Branch)}");
                    g.AddEdge(GetIndex(block), GetIndex(block.Branch), $"Branch\\n{block.FirstIndex}->{block.Branch.FirstIndex}");
                }
            }

            void AddClosureEdge(ControlFlowGraph curGraph) {
                foreach ((int address, ControlFlowGraph flowGraph) in curGraph.Closures) {
                    Block parent = curGraph.GetBlockIncluded(address);
                    Block child = flowGraph.Root;
                    g.AddEdge(GetIndex(parent), GetIndex(child), $"{curGraph.Prototype.Name}({parent.FirstIndex}-{parent.LastIndex})->" +
                                                                 $"{flowGraph.Prototype.Name}({child.FirstIndex}-{child.LastIndex})");
                    // g.AddEdge(blockIndex[parent], blockIndex[child], false, label: $"{curGraph.Prototype.Name}[{parent.FirstIndex}-{parent.LastIndex}]->" +
                    //                                                                $"{graphClosure.Graph.Prototype.Name}[{child.FirstIndex}-{child.LastIndex}]");
                    AddClosureEdge(flowGraph);
                }
            }

            AddClosureEdge(graph);

            return g;
        }
    }
}