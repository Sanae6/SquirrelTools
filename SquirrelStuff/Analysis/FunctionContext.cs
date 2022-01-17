using System.Collections.Generic;
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
            Expressions = new Expression[Prototype.StackSize];
        }

        public LocalExpression GetLocal(int sp, FunctionPrototype.Instruction inst) {
            return new LocalExpression(sp, Prototype.GetLocalVar(sp, inst.Position), inst.Position) {Used = true};
        }

        // todo more local recovery (i.e. prepcall copies this to stackVar3 and handling that when there's no locals available)
        // public FunctionPrototype.LocalVar NewLocal(int sp, int start, SquirrelObject name) {
        //     return new DecompLocal {
        //         Name = name,
        //         Pos = (uint) sp,
        //         StartOp = (uint) start,
        //         EndOp = (uint) start
        //     };
        // }

        public Expression? Take(int sp, FunctionPrototype.Instruction inst) {
            switch (sp) {
                case 0:
                    return new ThisExpression {Used = true};//todo parameter expression
                case > 0 when sp < Expressions.Length: {
                    Expression expr = Expressions[sp - 1];
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

        public void Update(int sp, Expression replacement) {
            Add(replacement);
            SilentReplace(sp, replacement);
        }

        public void SilentReplace(int sp, Expression replacement) {
            Expressions[sp - 1] = replacement;
        }
    }
}