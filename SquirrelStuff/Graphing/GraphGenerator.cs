using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SquirrelStuff.Analysis;
using SquirrelStuff.Bytecode;

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
            for (int ip = 0; ip < prototype.Instructions.Length;) {
                FunctionPrototype.Instruction inst = prototype.Instructions[ip];

                if (graph.HasBlock(ip)) {
                    Block block = graph.GetBlock(ip);
                    if (current != null) {
                        current.Next = block;
                        current.Branch = null;
                    }

                    current = block;
                }

                current ??= graph.NewBlock(ip);
                current.LastIndex = ip;

                int orIp = ip++; // to simulate jumps correctly, the ip must be one instruction further (see squirrel3/sqvm.cpp:Execute())
                int address = ip + inst.Argument1;
                switch (inst.Opcode) {
                    case Opcodes.Jmp: {
                        if (address == ip) continue;
                        Block block = graph.GetBlock(address, orIp);
                        current.Next = block;
                        current.Branch = null;
                        current = null;
                        break;
                    }
                    case Opcodes.JCmp:
                    case Opcodes.Jz: {
                        if (address == ip) continue;
                        Block nextBlock = graph.GetBlock(ip, ip);
                        Block branchBlock = graph.GetBlock(address, ip);
                        current.Next = nextBlock;
                        current.Branch = branchBlock;
                        current = null;
                        break;
                    }
                    case Opcodes.ForEach: {
                        Block nextBlock = graph.GetBlock(ip, ip);
                        Block branchBlock = graph.GetBlock(address, ip);
                        current.Next = nextBlock;
                        current.Branch = branchBlock;
                        current = null;
                        break;
                    }
                    case Opcodes.Return: {
                        current = null;
                        break;
                    }
                    case Opcodes.Closure: // to see closure branches
                        ControlFlowGraph flowGraph = BuildControlFlowGraph(prototype.Functions[inst.Argument1]);
                        current.Closures.Add(orIp, flowGraph);
                        graph.Closures.Add((orIp, flowGraph));
                        break;
                }
            }

            //eliminate accidental circular block references
            foreach (Block block in graph.Blocks.Values.Distinct()) {
                if (block.Next == block) block.Next = null;
                if (block.Branch == block) block.Branch = null;
            }

            bool NotLastBlock(int last) => graph.HasBlock(last + 1);
            foreach (Block block in graph.Blocks.Values.Distinct()) {
                if (block.Next == null && NotLastBlock(block.LastIndex) && prototype.Instructions[block.LastIndex].Opcode != Opcodes.Return) block.Next = graph.GetBlockIncluded(block.LastIndex + 1);
            }

            List<Block> marked = new List<Block>();
            foreach (Block block in graph.Blocks.Values.Distinct()) {
                if (block.Parents.Count == 0 && block.FirstIndex != 0) {
                    block.Next = null;
                    block.Branch = null;
                    marked.Add(block);
                }
            }

            foreach (KeyValuePair<int, Block> kvp in graph.Blocks.ToArray()) {
                if (marked.Any(block => block == kvp.Value)) graph.Blocks.Remove(kvp.Key);
            }

            return graph;
        }

        public static Graph GenerateGraph(ControlFlowGraph graph, int digits = -1) {
            if (digits == -1) digits = graph.Prototype.MaxLineDigits().ToString().Length;
            Graph g = new Graph(graph.Prototype.Name.ToString());

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
                foreach (Block block in curGraph.UniqueBlocks.Where(block => AllBlocks.Add(block))) {
                    StringBuilder builder = new StringBuilder();
                    builder.Append(@"<<TABLE BORDER=""0"" CELLBORDER=""1"" CELLSPACING=""0"">");
                    if (block.FirstIndex == 0) builder.Append($"<TR><TD COLSPAN=\"3\">{block.Prototype.Name.ToString(true)}</TD></TR>");
                    else if (block.Parents.Count == 0) builder.Append($"<TR><TD COLSPAN=\"3\">Dead code from {block.Prototype.Name.ToString(true)}</TD></TR>");
                    for (int i = block.FirstIndex; i <= block.LastIndex; i++) {
                        // builder.Append($@"<TR><TD PORT=""{i}"">{i.ToString().PadLeft(digits, '0')} </TD></TR>");
                        builder.AppendLine();
                        builder.Append($@"<TR><TD PORT=""L{i}"">{i.ToString().PadLeft(digits, '0')}</TD><TD>{curGraph.Prototype.Instructions[i].ToString(curGraph.Prototype, true, false)}</TD><TD PORT=""R{i}"">{
                            curGraph.Prototype.Instructions[i].ToString(curGraph.Prototype, false, true)
                                .Replace("&", "&amp;")
                                .Replace("<", "&lt;").Replace(">", "&gt;")
                                // .Replace("[", "&#91;").Replace("]", "&#93;")
                        }</TD></TR>");
                    }
                    builder.Append("</TABLE>>");
                    g.AddVertex($"struct{GetIndex(block)}", builder.ToString());
                }

                foreach ((int _, var closureGraph) in curGraph.Closures) {
                    AddBlockClosure(closureGraph);
                }
            }

            AddBlockClosure(graph);

            foreach (Block block in AllBlocks) {
                if (block.Next is not null) {
                    g.AddEdge($"struct{GetIndex(block)}:R{block.LastIndex}", $"struct{GetIndex(block.Next)}:L{block.NextIndex}", $"{block.LastIndex}->{block.Next.FirstIndex}");
                }

                if (block.Branch is not null) {
                    g.AddEdge($"struct{GetIndex(block)}:R{block.LastIndex}", $"struct{GetIndex(block.Branch)}:L{block.BranchIndex}", $"Branch\\n{block.LastIndex}->{block.Branch.FirstIndex}");
                }
            }

            void AddClosureEdge(ControlFlowGraph curGraph) {
                foreach ((int address, ControlFlowGraph flowGraph) in curGraph.Closures) {
                    Block parent = curGraph.GetBlockIncluded(address);
                    Block child = flowGraph.Root;
                    g.AddEdge($"struct{GetIndex(parent)}:R{address}", $"struct{GetIndex(child)}", $"{curGraph.Prototype.Name}({parent.FirstIndex}-{parent.LastIndex})->" +
                                                                                                            $"{flowGraph.Prototype.Name}({child.FirstIndex}-{child.LastIndex})");
                    AddClosureEdge(flowGraph);
                }
            }

            AddClosureEdge(graph);

            return g;
        }
    }
}