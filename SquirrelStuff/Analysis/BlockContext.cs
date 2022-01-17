using System.Collections.Generic;
using SquirrelStuff.Graphing;

namespace SquirrelStuff.Analysis {
    internal record BlockContext(AnalysisBlock Block, Expression[] FinalStack) {
        internal List<Expression> Expressions => Block.Expressions;
        internal Block GraphBlock => Block.Block;
    }
}