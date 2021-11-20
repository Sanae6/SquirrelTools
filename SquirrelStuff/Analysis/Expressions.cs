using System;
using System.Collections.Generic;
using System.Text;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Analysis {
    public abstract class Expression {
        public abstract string ToString(Expression parent, Decompiler decompiler);
    }

    // The problem with making statements separate from expressions is that squirrel doesn't actually make this separation itself,
    // so a program with the number 1 is a fully valid and compilable program.
    // Therefore, this is simply a convention and nothing more.
    public abstract class Statement : Expression {
        public abstract override string ToString(Expression parent, Decompiler decompiler);
    }

    public class LiteralExpression : Expression {
        public LiteralExpression(SquirrelObject literal) {
            Literal = literal;
        }

        public SquirrelObject Literal;

        public override string ToString(Expression parent, Decompiler decompiler) {
            return Literal.ToString(parent switch {
                AccessorExpression {Self: ThisExpression or RootTableExpression} => true,
                _ => false
            });
        }
    }

    public class LocalExpression : Expression {
        public LocalExpression(int location, FunctionPrototype.LocalVar local, int instruction) {
            Location = location;
            Local = local;
            ip = instruction;
        }

        public readonly int Location;
        public FunctionPrototype.LocalVar Local;
        private readonly int ip;
        public bool IsDefinition => Local.StartsAt(ip);

        public override string ToString(Expression parent, Decompiler decompiler) {
            return Local.ToString();
        }
    }

    // public class StackVarExpression : Expression {
    //     
    // }

    // denotes an access to the root table
    public class RootTableExpression : Expression {
        public override string ToString(Expression parent, Decompiler decompiler) => "::";
    }

    // denotes an access to the current scope
    public class ThisExpression : Expression {
        public override string ToString(Expression parent, Decompiler decompiler) => "this";
    }

    public class AccessorExpression : Expression {
        public AccessorExpression(Expression self, Expression key) {
            Self = self;
            Key = key;
        }

        public readonly Expression Self;
        public readonly Expression Key;

        public override string ToString(Expression parent, Decompiler decompiler) {
            return Self switch {
                RootTableExpression => Self.ToString(this, decompiler) + Key.ToString(this, decompiler),
                ThisExpression => Key.ToString(this, decompiler),
                _ => $"{Self.ToString(this, decompiler)}[{Key}]"
            };
        }
    }

    public class CompareExpression : Expression {
        public readonly Expression LeftSide;
        public readonly OperatorExpression Operation;
        public readonly Expression RightSide;

        public CompareExpression(Expression leftSide, OperatorExpression operation, Expression rightSide) {
            LeftSide = leftSide;
            Operation = operation;
            RightSide = rightSide;
        }

        public override string ToString(Expression parent, Decompiler decompiler) {
            return $"{LeftSide.ToString(this, decompiler)} {Operation.ToString(this, decompiler)} {RightSide.ToString(this, decompiler)}";
        }
    }

    public class AssignmentStatement : Statement {
        public readonly Expression LeftSide;
        public readonly Expression RightSide;

        public AssignmentStatement(Expression left, Expression right) {
            LeftSide = left;
            RightSide = right;
        }

        public override string ToString(Expression parent, Decompiler decompiler)
            => $"{(LeftSide is LocalExpression {IsDefinition: true} ? "local " : "")}{LeftSide.ToString(this, decompiler)} = {RightSide.ToString(this, decompiler)}";
    }

    public class NewSlotStatement : Statement {
        public NewSlotStatement(Expression self, Expression right) {
            Self = self;
            Value = right;
        }

        public readonly Expression Self;
        public readonly Expression Value;

        public override string ToString(Expression parent, Decompiler decompiler) {
            return $"{Self.ToString(this, decompiler)} <- {Value.ToString(this, decompiler)}";
        }
    }

    public class ReturnStatement : Statement {
        public ReturnStatement(Expression? value) {
            Value = value;
        }

        public readonly Expression? Value;

        public override string ToString(Expression parent, Decompiler decompiler) {
            return Value == null ? "return" : $"return {Value.ToString(this, decompiler)}";
        }
    }

    public class ClosureExpression : Expression {
        internal ClosureExpression(FunctionPrototype prototype, AnalysisBlock root) {
            Prototype = prototype;
            Root = root;
        }

        public readonly FunctionPrototype Prototype;
        public SquirrelObject Name => Prototype.Name;
        public SquirrelObject[] Parameters => Prototype.Parameters;
        public bool HasVariadicParameters => Prototype.VarParams;
        internal readonly AnalysisBlock Root;

        public override string ToString(Expression parent, Decompiler decompiler) {
            StringBuilder builder = new StringBuilder();
            builder.Append(Name.Type == ObjectType.OtNull ? "function(" : $"function {Name}(");
            for (int i = 0; i < Parameters.Length; i++) {
                if (i != 0) builder.Append(", ");
                if (HasVariadicParameters && i == Parameters.Length - 1) builder.Append("...");
                else builder.Append(Parameters[i]);
            }

            builder.AppendLine(") {");
            decompiler.IndentationLevel++;
            builder.Append(Root.ToString(this, decompiler));
            decompiler.IndentationLevel--;
            builder.AppendLine($"{decompiler.Indentation}}}");
            return builder.ToString();
        }
    }

    // wrapper for the operator enum
    public class OperatorExpression : Expression {
        public OperatorExpression(Operator operation) {
            Operation = operation;
        }

        public readonly Operator Operation;

        public override string ToString(Expression parent, Decompiler decompiler) {
            return Operation switch {
                Operator.UnaryNegate => "-",
                Operator.UnaryNot => "!",

                Operator.ArithmeticAdd => "+",
                Operator.ArithmeticSub => "-",
                Operator.ArithmeticMul => "*",
                Operator.ArithmeticDiv => "/",
                Operator.ArithmeticMod => "%",
                Operator.ArithmeticAnd => "&",
                Operator.ArithmeticOr => "|",
                Operator.ArithmeticXor => "^",

                Operator.CompareAnd => "&&",
                Operator.CompareOr => "||",
                Operator.CompareGreater => ">",
                Operator.CompareLess => "<",
                Operator.CompareGreaterEqual => ">=",
                Operator.CompareLessEqual => "<=",
                Operator.CompareEqual => "=",
                Operator.CompareThreeWay => "<=>",
                _ => throw new IndexOutOfRangeException()
            };
        }
    }

    public enum Operator {
        // Unary
        UnaryNegate,
        UnaryNot,
        // Arithmetic
        ArithmeticAdd,
        ArithmeticSub,
        ArithmeticMul,
        ArithmeticDiv,
        ArithmeticMod,
        ArithmeticAnd,
        ArithmeticOr,
        ArithmeticXor,
        // Comparison
        CompareAnd,
        CompareOr,
        CompareGreater,
        CompareLess,
        CompareGreaterEqual,
        CompareLessEqual,
        CompareEqual,
        CompareThreeWay
    }
}