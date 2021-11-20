using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SquirrelStuff.Bytecode;
using SquirrelStuff.Graphing;

namespace SquirrelStuff.Analysis {
    public class Decompiler {
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
        }

        public static Decompiler Decompile(FunctionPrototype prototype) {
            ControlFlowGraph cfg = GraphGenerator.BuildControlFlowGraph(prototype);
            Decompiler decompiler = new Decompiler(cfg);
            FunctionContext functionContext = new FunctionContext(prototype);

            foreach (Block block in cfg.UniqueBlocks) {
                decompiler.DecompileBlock(block, functionContext);
            }

            return decompiler;
        }

        private void DecompileBlock(Block block, FunctionContext context) {
            void Replace(int location, Expression expression) {
                if (Get(location) != null)
                    context.ExpressionList.Add(Get(location)!);
                else if (ip == endIp)
                    context.ExpressionList.Add(expression);
                else
                    Update(location, expression);
            }

            void Assign(int location, Expression expression) {
                FunctionPrototype.LocalVar? local = Prototype.GetLocalVar(location, ip);
                if (local is not null)
                    context.ExpressionList.Add(new AssignmentStatement(new LocalExpression(location, local, ip), expression));
                else
                    Replace(location, expression);
            }

            Expression? Get(int location) => context.Expressions[location]; // suppressed because normal code will never not have something there

            Expression Take(int location) {
                if (location == 0) return new ThisExpression();
                Expression temp = Get(location)!;
                Update(location, null!);
                return temp;
            }

            void Update(int location, Expression expression) => context.Expressions[location] = expression;

            Console.WriteLine($"decompiling block {block.FirstIndex}-{block.LastIndex}");
            endIp = block.LastIndex;
            for (ip = block.FirstIndex; ip <= endIp; ip++) {
                FunctionPrototype.Instruction inst = Prototype.Instructions[ip];
                Console.WriteLine($" {ip} {inst.Opcode}");
                switch (inst.Opcode) {
                    // case Opcodes.Load:
                    //     Assign(inst.Argument0, new LiteralExpression(Prototype.Literals[inst.Argument1]));
                    //     break;
                    // case Opcodes.LoadBool:
                    //     Assign(inst.Argument0, new LiteralExpression(DecompObject.Bool(inst.Argument1 != 0)));
                    //     break;
                    // case Opcodes.LoadFloat:
                    //     Assign(inst.Argument0, new LiteralExpression(DecompObject.Float(inst.Argument1d)));
                    //     break;
                    // case Opcodes.LoadInt:
                    //     Assign(inst.Argument0, new LiteralExpression(DecompObject.Int(inst.Argument1)));
                    //     break;
                    // case Opcodes.LoadNulls:
                    //     for (int i = 0; i < inst.Argument1; i++) {
                    //         Assign(inst.Argument0 + i, new LiteralExpression(DecompObject.Null()));
                    //     }
                    //
                    //     break;
                    // case Opcodes.LoadRoot:
                    //     Assign(inst.Argument0, new RootTableExpression());
                    //     break;
                    // case Opcodes.NewSlot:
                    //     NewSlotStatement newSlotStatement = new NewSlotStatement(new AccessorExpression(Take(inst.Argument1), Take(inst.Argument2)), Take(inst.Argument3));
                    //     if (inst.Argument0 != 0xFF)
                    //         Assign(inst.Argument0, newSlotStatement.Value);
                    //     else
                    //         context.ExpressionList.Add(newSlotStatement);
                    //     break;
                    // case Opcodes.Set:
                    //     AssignmentStatement assignmentStatement = new AssignmentStatement(new AccessorExpression(Take(inst.Argument1), Take(inst.Argument2)), Take(inst.Argument3));
                    //     if (inst.Argument0 != 0xFF)
                    //         Assign(inst.Argument0, assignmentStatement.RightSide);
                    //     else
                    //         context.ExpressionList.Add(assignmentStatement);
                    //     break;
                    case Opcodes.Return:
                        // context.ExpressionList.Add(new ReturnStatement(inst.Argument0 != 0xFF ? Take(inst.Argument1) : null));
                        // break;
                    case Opcodes.JCmp: {
                        // context.ExpressionList.Add(inst.Opcode == Opcodes.Jz
                        //     ? new IfStatement(Take(inst.Argument0))
                        //     : new IfStatement(new CompareExpression(Take(inst.Argument2), new OperatorExpression(inst.Argument3 switch {
                        //         0 => Operator.CompareGreater,
                        //         2 => Operator.CompareGreaterEqual,
                        //         3 => Operator.CompareLess,
                        //         4 => Operator.CompareLessEqual,
                        //         5 => Operator.CompareThreeWay,
                        //         byte num => throw new IndexOutOfRangeException($"There is no comparison operator at {num}")
                        //     }), Take(inst.Argument0))));
                        // break;
                        continue;
                    }
                    case Opcodes.Jmp: {
                        // context.ExpressionList.Add(new JumpStatement());
                        break;
                    }
                }
            }
        }

        private AnalysisBlock HighLevelAnalysis(Block block, FunctionContext context) {
            return null!;
        }

        public override string ToString() {
            return RootContext.Root!.ToString(null!, this);
        }
    }
}