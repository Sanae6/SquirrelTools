using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SquirrelStuff.Bytecode;
using SquirrelStuff.Graphing;

namespace SquirrelStuff.Analysis {
    public partial class Decompiler {
        private const string Indent = "    ";
        private int indentationLevel;
        public string Indentation { get; private set; } = "";

        public int IndentationLevel {
            get => indentationLevel;
            set {
                indentationLevel = value;

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < IndentationLevel; i++) {
                    sb.Append(Indent);
                }

                Indentation = sb.ToString();
            }
        }

        private readonly ControlFlowGraph Graph;
        private FunctionPrototype Prototype => Graph.Prototype;
        private FunctionContext RootContext;
        private (int, Expression)? lastStackExpression;
        private int ip, endIp;

        private Decompiler(ControlFlowGraph graph) {
            Graph = graph;
            RootContext = new FunctionContext(graph);
        }

        public static string Decompile(FunctionPrototype prototype) {
            ControlFlowGraph cfg = GraphGenerator.BuildControlFlowGraph(prototype);
            Decompiler decompiler = new Decompiler(cfg);

            return decompiler.Decompile();
        }

        private string Decompile() {
            foreach (AnalysisBlock block in RootContext.AnalysisBlocks) {
                DecompileBlock(block, RootContext);
            }

            return RootContext.Root.ToString(null!, this);
        }

        private void DecompileBlock(AnalysisBlock analysisBlock, FunctionContext context) {
            context.ExpressionList = new List<Expression>();
            Block block = analysisBlock.Block;
            for (ip = 0; ip < block.Instructions.Length; ip++) {
                FunctionPrototype.Instruction inst = block.Instructions[ip];
                switch (inst.Opcode) {
                    case Opcodes.Load:
                        context.Update(inst.Argument0, new LiteralExpression(context.Prototype.Literals[inst.Argument1]));
                        break;
                    case Opcodes.DLoad:
                        context.Update(inst.Argument0, new LiteralExpression(context.Prototype.Literals[inst.Argument1]));
                        context.Update(inst.Argument2, new LiteralExpression(context.Prototype.Literals[inst.Argument3]));
                        break;
                    case Opcodes.LoadBool:
                        context.Update(inst.Argument0, new LiteralExpression(DecompObject.Bool(inst.Argument1 != 0)));
                        break;
                    case Opcodes.LoadInt:
                        context.Update(inst.Argument0, new LiteralExpression(DecompObject.Int(inst.Argument1)));
                        break;
                    case Opcodes.LoadFloat:
                        context.Update(inst.Argument0, new LiteralExpression(DecompObject.Float(inst.Argument1f)));
                        break;
                    case Opcodes.LoadRoot:
                        context.Update(inst.Argument0, new RootTableExpression());
                        break;
                    case Opcodes.LoadNulls:
                        context.Update(inst.Argument0, new LiteralExpression(DecompObject.Null()));
                        break;
                    case Opcodes.NewObj:
                        switch (inst.Argument3) {
                            case 0:
                                context.Update(inst.Argument0, new TableExpression(inst.Argument1));
                                break;
                            case 1:
                                context.Update(inst.Argument0, new ArrayExpression(inst.Argument1));
                                break;
                            case 2:
                                // builder.Append($"Class(BaseClass = {inst.Argument1}, Attributes = 0x{inst.Argument2:X2})");
                                throw new NotImplementedException("todo classes 🙈");
                                // break;
                            default:
                                throw new IndexOutOfRangeException($"There is no new object type case for {inst.Argument3}");
                        }
                        break;
                    case Opcodes.AppendArray: {
                        Expression self = context.Take(inst.Argument0, inst)!;
                        if (self is ArrayExpression array) { // array is not full due to check from inside of Take
                            switch (inst.Argument2) {
                                case 0:
                                    array.AddElement(context.Take(inst.Argument1, inst)!);
                                    break;
                                case 1:
                                    array.AddElement(new LiteralExpression(context.Prototype.Literals[inst.Argument1]));
                                    break;
                                case 2:
                                    array.AddElement(new LiteralExpression(DecompObject.Int(inst.Argument1)));
                                    break;
                                case 3:
                                    array.AddElement(new LiteralExpression(DecompObject.Float(inst.Argument1f)));
                                    break;
                                case 4:
                                    array.AddElement(new LiteralExpression(DecompObject.Bool(inst.Argument1 != 0)));
                                    break;
                                default:
                                    throw new IndexOutOfRangeException($"There is no array append type case for {inst.Argument2}");
                            }
                        } else {
                            throw new IndexOutOfRangeException($"Cannot append onto non array element {self}");
                        }
                        break;
                    }
                    case Opcodes.NewSlotA: {
                        
                        break;
                    }
                    case Opcodes.NewSlot: {
                        Expression self = context.Take(inst.Argument1, inst)!;
                        Expression key = context.Take(inst.Argument2, inst)!;
                        Expression value = context.Take(inst.Argument3, inst)!;
                        NewSlotStatement newSlot = new NewSlotStatement(new AccessorExpression(self, key), value);
                        if (inst.Argument0 != 0xFF) {
                            context.Update(inst.Argument0, newSlot);
                        } else {
                            context.Add(newSlot);
                        }
                        break;
                    }
                    case Opcodes.Set: {
                        Expression self = context.Take(inst.Argument1, inst)!;
                        Expression key = context.Take(inst.Argument2, inst)!;
                        Expression value = context.Take(inst.Argument3, inst)!;
                        AssignmentStatement assignment = new AssignmentStatement(new AccessorExpression(self, key), value);
                        if (inst.Argument0 != 0xFF) {
                            context.Update(inst.Argument0, assignment);
                        } else {
                            context.Add(assignment);
                        }
                        break;
                    }
                    case Opcodes.PrepCall:
                    case Opcodes.PrepCallK: {
                        Expression key = inst.Opcode == Opcodes.PrepCallK ? new LiteralExpression(context.Prototype.Literals[inst.Argument1]) : context.GetLocal(inst.Argument1, inst)!;
                        Expression self = context.Take(inst.Argument2, inst)!;
                        context.Update(inst.Argument3, self);
                        context.Update(inst.Argument0, new AccessorExpression(self, key));
                        break;
                    }
                    case Opcodes.Call: {
                        CallExpression call = new CallExpression(context.Take(inst.Argument1, inst)!);
                        // skip argument 1 because it's *always* the scope, and the scope is already included from prepcall(k)
                        for (int i = 1; i < inst.Argument3; i++) {
                            call.Arguments.Add(context.Take(inst.Argument2 + i, inst)!);
                        }
                        if (inst.Argument0 != 0xFF) context.Update(inst.Argument0, call);
                        else context.Add(call);
                        break;
                    }
                    case Opcodes.Jz: {
                        Expression expr = context.Take(inst.Argument0, inst) ?? throw new InvalidOperationException();
                        if (expr is not BinaryExpression or UnaryExpression {Operation: {Operation: Operator.UnaryNot}})
                            expr = new UnaryExpression(new OperatorExpression(Operator.UnaryNot), expr);
                        context.Add(new IfStatement(expr));
                        break;
                    }
                    case Opcodes.Jmp:
                        context.Add(new JumpStatement());
                        break;
                    case Opcodes.Return:
                        context.Add(new ReturnStatement(inst.Argument0 != 0xFF ? context.Take(inst.Argument1, inst) : null));
                        break;
                    default: throw new NotImplementedException($"Opcode {inst.Opcode} has no decompile case implemented yet");
                }
            }

            BlockContext blockCtx = new BlockContext(analysisBlock, context.Expressions.ToArray());
            analysisBlock.Expressions = context.ExpressionList.Where(expr => !expr.Used).ToList();
        }

        private AnalysisBlock HighLevelAnalysis(FunctionContext context) {
            foreach (Block block in Graph.UniqueBlocks) {
                if (block.BranchIndex != null) {
                    // this is a branch block
                }
            }

            return null!;
        }

        public override string ToString() {
            return RootContext.Root!.ToString(null!, this);
        }
    }
}