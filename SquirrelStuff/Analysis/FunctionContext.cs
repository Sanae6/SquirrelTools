using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SquirrelStuff.Bytecode;
using SquirrelStuff.Graphing;

namespace SquirrelStuff.Analysis {
    internal class FunctionContext {
        internal readonly ControlFlowGraph Graph;
        internal readonly FunctionPrototype Prototype;
        internal readonly ClosureExpression Root;
        internal readonly Expression[] Expressions;
        internal readonly List<AnalysisBlock> AnalysisBlocks;
        internal List<Expression> ExpressionList = new List<Expression>();
        private HashSet<FunctionPrototype.LocalVar> SeenVars = new HashSet<FunctionPrototype.LocalVar>();

        protected virtual Expression this[int index] {
            get => Expressions[index];
            set => Expressions[index] = value;
        }

        public FunctionContext(ControlFlowGraph graph) {
            Graph = graph;
            Prototype = graph.Prototype;
            AnalysisBlocks = graph.UniqueBlocks
                .Select(block => new AnalysisBlock(block))
                .ToList();
            foreach (AnalysisBlock block in AnalysisBlocks) {
                block.Next = AnalysisBlocks.FirstOrDefault(x => x.Block == block.Block.Next);
                block.Branch = AnalysisBlocks.FirstOrDefault(x => x.Block == block.Block.Branch);
            }

            // you can assume there will always be one block in a function
            // since a valid compiler will always add a return statement
            Root = new ClosureExpression(Prototype, AnalysisBlocks[0]);
            Expressions = new Expression[Prototype.ActualStackSize];
        }

        public LocalExpression GetLocal(int sp, FunctionPrototype.Instruction inst) {
            FunctionPrototype.LocalVar local = Prototype.GetLocal(sp, inst.Position)!;
            return new LocalExpression(sp, local, SeenVars.Add(local)) {Used = true};
        }

        public bool HasSeenLocal(int sp, int ip) => SeenVars.Contains(Prototype.GetLocal(sp, ip)!);

        // todo more local recovery (i.e. prepcall copies this to stackVar3 and handling that when there's no locals available)
        // public FunctionPrototype.LocalVar NewLocal(int sp, int start, SquirrelObject name) {
        //     return new DecompLocal {
        //         Name = name,
        //         Pos = (uint) sp,
        //         StartOp = (uint) start,
        //         EndOp = (uint) start
        //     };
        // }

        public Expression Take(int sp, FunctionPrototype.Instruction inst) {
            switch (sp) {
                case 0:
                    return new ThisExpression {Used = true};//todo parameter expression
                case > 0 when sp < Expressions.Length: {
                    Expression expr = this[sp - 1];
                    if (expr is AssignmentStatement { LeftSide: LocalExpression { FirstOccurence: true } } assn) {
                        expr.Used = false;
                        return assn.LeftSide;
                    }
                    expr.Used = true;
                    return expr;
                }
                default:
                    return GetLocal(sp, inst);
            }
        }

        public void Add(Expression expression) {
            ExpressionList.Add(expression);
        }

        public void Update(int sp, FunctionPrototype.Instruction inst, Expression replacement) {
            if (replacement is not AssignmentStatement && !HasSeenLocal(sp, inst.Position) && Prototype.HasNamedLocal(sp, inst.Position)) {
                replacement = new AssignmentStatement(GetLocal(sp, inst), replacement);
            }
            Add(replacement);
            SilentReplace(sp, replacement);
        }

        public void SilentReplace(int sp, Expression replacement) {
            this[sp - 1] = replacement;
        }
    }

    internal class SubFunctionContext : FunctionContext {
        internal readonly FunctionContext Parent;
        public SubFunctionContext(ControlFlowGraph graph, FunctionContext parent) : base(graph) {
            Parent = parent;
        }

        protected override Expression this[int index] {
            get => index < Parent.Prototype.StackSize ? base[index] : Expressions[index];
            set {
                if (index < Parent.Prototype.StackSize) base[index] = value;
                else Expressions[index] = value;
            }
        }
    }
}