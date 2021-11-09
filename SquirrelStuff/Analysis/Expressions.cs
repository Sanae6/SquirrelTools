using System.Collections.Generic;
using System.Text;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Analysis {
    public abstract class Statement : Expression {
        public abstract override string ToString(Decompiler decompiler);
    }
    public abstract class Expression {
        public abstract string ToString(Decompiler decompiler);
    }

    public class LiteralExpression : Expression {

        public LiteralExpression(SquirrelObject literal) {
            Literal = literal;
        }
        public SquirrelObject Literal;

        public override string ToString(Decompiler decompiler) {
            return Literal.ToString();
        }
    }

    public class AssignmentStatement : Statement {
        public Expression LeftSide;
        public Expression RightSide;
        public AssignmentStatement(Expression left, Expression right) {
            LeftSide = left;
            RightSide = right;
        }
        public override string ToString(Decompiler decompiler) 
            => LeftSide.ToString(decompiler) + " = " + RightSide.ToString(decompiler);
    }

    public class NewSlotStatement : Statement {
        public NewSlotStatement(Expression left, Expression right) {
            LeftSide = left;
            RightSide = right;
        }
        public readonly Expression LeftSide;
        public readonly Expression RightSide;
        public override string ToString(Decompiler decompiler)
            => LeftSide.ToString(decompiler) + " <- " + RightSide.ToString(decompiler);
    }

    public class FunctionExpression : Expression {
        public FunctionExpression(FunctionPrototype prototype, List<Expression> expressions) {
            Prototype = prototype;
            Expressions = expressions;
        }

        public readonly FunctionPrototype Prototype;
        public SquirrelObject Name => Prototype.Name;
        public SquirrelObject[] Parameters => Prototype.Parameters;
        public bool HasVariadicParameters => Prototype.VarParams;
        public readonly List<Expression> Expressions;
        public override string ToString(Decompiler decompiler) {
            StringBuilder builder = new StringBuilder();
            builder.Append($"function {Name}(");
            for (int i = 0; i < Parameters.Length; i++) {
                if (i != 0) builder.Append(", ");
                if (HasVariadicParameters && i == Parameters.Length - 1) builder.Append("...");
                else builder.Append(Parameters[i]);
            }
            builder.AppendLine(") {");
            decompiler.IndentationLevel++;
            foreach (Expression expression in Expressions) {
                builder.AppendLine(decompiler.Indentation + expression.ToString(decompiler));
            }
            decompiler.IndentationLevel--;
            builder.AppendLine("}");
            return builder.ToString();
        }
    }

    public enum Operator {
        // Unary
        UnaryMinus,
        UnaryNot,
        // Binary
        BinaryAdd,
        BinarySub,
        BinaryMul,
        BinaryDiv,
        BinaryMod,
        BinaryAnd,
        BinaryOr,
        BinaryXor
    }
}