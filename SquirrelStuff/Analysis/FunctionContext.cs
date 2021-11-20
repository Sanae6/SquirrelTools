using System.Collections.Generic;
using SquirrelStuff.Bytecode;
using SquirrelStuff.Graphing;

namespace SquirrelStuff.Analysis {
    internal class FunctionContext {
        internal readonly FunctionPrototype Prototype;
        internal Expression? Root;
        internal Expression?[]? Expressions;
        internal List<FunctionPrototype.LocalVar> StackVars = new List<FunctionPrototype.LocalVar>();
        internal HashSet<Block> VisitedBlocks = new HashSet<Block>();
        internal List<AnalysisBlock> AnalysisBlocks = new List<AnalysisBlock>();
        internal List<Expression> ExpressionList = new List<Expression>();

        public FunctionContext(FunctionPrototype prototype) {
            this.Prototype = prototype;
        }
    }
}