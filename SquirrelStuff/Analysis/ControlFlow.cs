using System.Collections.Generic;
using System.Text;
using SquirrelStuff.Graphing;

namespace SquirrelStuff.Analysis {
    internal class AnalysisBlock : Expression {
        public AnalysisBlock? Next;
        public AnalysisBlock? Branch;
        public List<AnalysisBlock> Parents = new List<AnalysisBlock>();
        public List<Expression> Expressions = new List<Expression>();
        public List<FunctionContext> Children = new List<FunctionContext>();
        public readonly Block Block;

        public AnalysisBlock(Block block) {
            Block = block;
        }

        public override string ToString(Expression parent, Decompiler decompiler) {
            StringBuilder builder = new StringBuilder();

            foreach (Expression expression in Expressions) {
                builder.Append($"{decompiler.Indentation}{expression.ToString(this, decompiler)}");
                builder.AppendLine(expression is AnalysisBlock ? "" : ";");
            }

            return builder.ToString();
        }
    }

    internal class IfStatement : Statement {
        public Expression Condition;

        public IfStatement(Expression condition) {
            Condition = condition;
        }

        public override string ToString(Expression parent, Decompiler decompiler) {
            StringBuilder builder = new StringBuilder();
            builder.Append("if (")
                .Append(Condition.ToString(this, decompiler))
                .Append(")");
            // decompiler.IndentationLevel++;
            // builder.Append(base.ToString(this, decompiler));
            // decompiler.IndentationLevel--;
            // builder.Append($"{decompiler.Indentation}}}");
            return builder.ToString();
        }
    }

    //todo else statements
    internal class JumpStatement : Statement {
        public override string ToString(Expression parent, Decompiler decompiler) {
            return "jump to someplace";
        }
    }

    internal class ReturnBlock : AnalysisBlock {
        public ReturnBlock(Block block) : base(block) { }
    }

    internal class ForEachBlock {
        
    }
}