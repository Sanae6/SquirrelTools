namespace SquirrelStuff.Analysis;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SquirrelStuff.Bytecode;
using TableExpressionPair = System.Collections.Generic.KeyValuePair<Expression, Expression>;

public abstract class Expression {
    internal bool Used { get; set; }

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
            AccessorExpression => true,
            CallExpression call when call.Callee == this => true,
            _ => false
        });
    }
}

public class LocalExpression : Expression {
    public readonly int Location;
    public readonly FunctionPrototype.LocalVar? Local;
    public bool FirstOccurence;

    public LocalExpression(int location, FunctionPrototype.LocalVar? local, bool firstOccurence) {
        Location = location;
        Local = local;
        FirstOccurence = firstOccurence;
    }

    public override string ToString(Expression parent, Decompiler decompiler) {
        return Local?.ToString() ?? $"$stackVar{Location}";
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

public abstract class ContainerExpression : Expression {
    public abstract bool IsFull { get; }
}
public class ArrayExpression : ContainerExpression {
    public readonly Expression[] Pairs;

    public ArrayExpression(int size) {
        Pairs = new Expression[size];
    }

    private int start;
    public override bool IsFull => start >= Pairs.Length;
    public void AddElement(Expression value) {
        if (IsFull) throw new IndexOutOfRangeException("Array expression is already full");
        Pairs[start++] = value;
    }

    public override string ToString(Expression parent, Decompiler decompiler) {
        if (Pairs.Length == 0) {
            return "[]";
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[");
        decompiler.IndentationLevel++;
        for (int i = 0; i < Pairs.Length; i++) {
            Expression expression = Pairs[i];
            builder.AppendLine($"{decompiler.Indentation}{expression.ToString(this, decompiler)}{(i < Pairs.Length - 1 ? "," : "")}");
        }

        decompiler.IndentationLevel--;
        builder.Append($"{decompiler.Indentation}]");

        return builder.ToString();
    }
}

public class TableExpression : ContainerExpression {
    public readonly TableExpressionPair[] Pairs;

    public TableExpression(int size) {
        Pairs = new TableExpressionPair[size];
    }

    private int start;
    public override bool IsFull => start >= Pairs.Length;
    public void AddReservedElement(Expression key, Expression value) {
        if (IsFull) throw new IndexOutOfRangeException("Table expression is already full");
        Pairs[start++] = new TableExpressionPair(key, value);
    }

    public override string ToString(Expression parent, Decompiler decompiler) {
        if (Pairs.Length == 0) {
            return "{}";
        }
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("{");
        decompiler.IndentationLevel++;
        for (int i = 0; i < Pairs.Length; i++) {
            (Expression key, Expression value) = Pairs[i];
            builder.AppendLine($"{decompiler.Indentation}{key.ToString(this, decompiler)} = {value}{(i < Pairs.Length - 1 ? "," : "")}");
        }

        builder.Append($"{decompiler.Indentation}}}");
        decompiler.IndentationLevel--;

        return builder.ToString();
    }
}

public class AccessorExpression : Expression {
    public AccessorExpression(Expression self, Expression key) {
        Self = self;
        Key = key;
    }

    public readonly Expression Self;
    public readonly Expression Key;

    private static readonly Regex IdentifierMatch = new Regex("[a-zA-Z_][a-zA-Z0-9_]*");
    private static bool ValidateIdentifier(string text) => IdentifierMatch.IsMatch(text);

    public override string ToString(Expression parent, Decompiler decompiler) {
        return Self switch {
            RootTableExpression => Self.ToString(this, decompiler) + Key.ToString(this, decompiler),
            ThisExpression => Key.ToString(this, decompiler),
            _ => Key is LiteralExpression {Literal: {Type: ObjectType.OtString}} literal && ValidateIdentifier(literal.Literal.ToString())
                ? $"{Self.ToString(this, decompiler)}.{Key.ToString(this, decompiler)}"
                : $"{Self.ToString(this, decompiler)}[{Key.ToString(this, decompiler)}]"
        };
    }
}

public class BinaryExpression : Expression {
    public readonly Expression LeftSide;
    public readonly OperatorExpression Operation;
    public readonly Expression RightSide;

    public BinaryExpression(Expression leftSide, OperatorExpression operation, Expression rightSide) {
        LeftSide = leftSide;
        Operation = operation;
        RightSide = rightSide;
    }

    public override string ToString(Expression parent, Decompiler decompiler) {
        return $"{LeftSide.ToString(this, decompiler)} {Operation.ToString(this, decompiler)} {RightSide.ToString(this, decompiler)}";
    }
}

public class UnaryExpression : Expression {
    public readonly OperatorExpression Operation;
    public readonly Expression Expression;

    public UnaryExpression(OperatorExpression operation, Expression expression) {
        Operation = operation;
        Expression = expression;
    }

    public override string ToString(Expression parent, Decompiler decompiler) {
        return Operation.Operation is Operator.UnaryPostIncrement or Operator.UnaryPostDecrement ? $"{Expression.ToString(this, decompiler)}{Operation.ToString(this, decompiler)}" : $"{Operation.ToString(this, decompiler)}{Expression.ToString(this, decompiler)}";
    }
}

public class CallExpression : Expression {
    public readonly Expression Callee;
    public readonly List<Expression> Arguments = new List<Expression>();

    public CallExpression(Expression callee) {
        Callee = callee;
    }

    public override string ToString(Expression parent, Decompiler decompiler) {
        StringBuilder builder = new StringBuilder($"{Callee.ToString(this, decompiler)}(");
        for (int i = 0; i < Arguments.Count; i++) {
            Expression argument = Arguments[i];
            if (i != 0) builder.Append(", ");
            builder.Append(argument.ToString(this, decompiler));
        }

        builder.Append(')');
        return builder.ToString();
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
        => $"{(LeftSide is LocalExpression {FirstOccurence: true} ? "local " : "")}{LeftSide.ToString(this, decompiler)} = {RightSide.ToString(this, decompiler)}";
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
public enum Operator {
    // Unary
    UnaryNegate,
    UnaryNot,
    UnaryPreIncrement,
    UnaryPostIncrement,
    UnaryPreDecrement,
    UnaryPostDecrement,
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
    CompareThreeWay,
    CompareExists
}

public class OperatorExpression : Expression {
    public OperatorExpression(Operator operation) {
        Operation = operation;
    }

    public readonly Operator Operation;

    public override string ToString(Expression parent, Decompiler decompiler) {
        return Operation switch {
            Operator.UnaryNegate => "-",
            Operator.UnaryNot => "!",
            Operator.UnaryPreIncrement or Operator.UnaryPostIncrement => "++",
            Operator.UnaryPreDecrement or Operator.UnaryPostDecrement => "--",

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
            Operator.CompareExists => "in",
            _ => throw new IndexOutOfRangeException()
        };
    }
}