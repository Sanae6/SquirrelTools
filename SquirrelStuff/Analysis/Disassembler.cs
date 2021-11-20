using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Analysis {
    public static class Disassembler {
        // friendly disassembly
        internal static int MaxLineDigits(this FunctionPrototype prototype) {
            int max = prototype.Functions.Select(MaxLineDigits).Prepend(prototype.Instructions.Length).Max();
            // Console.WriteLine($"{max} {prototype.Name} {max.ToString()} {max.ToString().Length}");
            return max;
        }


        public static string ToString(this FunctionPrototype.Instruction inst, FunctionPrototype prototype, bool showName, bool showArgs) {
            StringBuilder builder = new StringBuilder();
            if (showName) builder.Append(showArgs ? $"{inst.Opcode.ToString()} ".PadRight(12) : inst.Opcode.ToString());
            if (showArgs)
                switch (inst.Opcode) {
                    case Opcodes.Load:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.Literals[inst.Argument1].ToString(false)}");
                        break;
                    case Opcodes.LoadInt:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {inst.Argument1}");
                        break;
                    case Opcodes.LoadBool:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {inst.Argument1 != 0}");
                        break;
                    case Opcodes.LoadFloat:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {inst.Argument1d}");
                        break;
                    case Opcodes.DLoad: // load two literals
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.Literals[inst.Argument1].ToString(false)}, ")
                            .Append($"{prototype.GetStackName(inst.Argument2, inst.Position)} = {prototype.Literals[inst.Argument3].ToString(false)}");
                        break;
                    case Opcodes.LoadNulls:
                        builder.Append($"{inst.Argument1} nulls starting at {inst.Argument0}");
                        break;

                    case Opcodes.Add:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} + {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.Sub:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} - {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.Mul:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} * {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.Div:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} / {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.Mod:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} % {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.Bitw:
                        string op = inst.Argument3 switch {
                            0 => "&",
                            2 => "|",
                            3 => "^",
                            4 => "<<",
                            5 => ">>",
                            6 => ">>>",
                            _ => "????"
                        };
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} {op} {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;

                    case Opcodes.Inc:
                    case Opcodes.PInc:
                        if (inst.Opcode == Opcodes.Inc) builder.Append(inst.Argument3 == 1 ? "++" : "--");
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument1, inst.Position)}.{prototype.GetStackName(inst.Argument2, inst.Position)}");
                        if (inst.Opcode == Opcodes.PInc) builder.Append(inst.Argument3 == 1 ? "++" : "--");
                        break;
                    case Opcodes.IncL:
                    case Opcodes.PIncL:
                        if (inst.Opcode == Opcodes.IncL) builder.Append(inst.Argument3 == 1 ? "++" : "--");
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        if (inst.Opcode == Opcodes.PIncL) builder.Append(inst.Argument3 == 1 ? "++" : "--");
                        break;

                    case Opcodes.And:
                        builder.Append($"if (!{prototype.GetStackName(inst.Argument2, inst.Position)}) ip += {inst.Argument1} [{inst.Argument1 + 1 + inst.Position}]");
                        break;
                    case Opcodes.Or:
                        builder.Append($"if (!{prototype.GetStackName(inst.Argument2, inst.Position)}) ip += {inst.Argument1} [{inst.Argument1 + 1 + inst.Position}]");
                        break;
                    case Opcodes.Neg:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = -{prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.Not:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = !{prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.Cmp:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} ");
                        builder.Append(inst.Argument3 switch {
                            0 => ">",
                            2 => ">=",
                            3 => ">",
                            4 => ">=",
                            5 => "<=>",
                            _ => "????",
                        });
                        builder.Append($" {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;

                    case Opcodes.Eq:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} == ");
                        builder.Append(inst.Argument3 != 0 ? prototype.Literals[inst.Argument1].ToString(true) : $"{prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.Ne:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)} != ");
                        builder.Append(inst.Argument3 != 0 ? prototype.Literals[inst.Argument1].ToString(true) : $"{prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;

                    case Opcodes.ForEach:
                        builder.Append($"foreach ({prototype.GetStackName(inst.Argument2, inst.Position)}, {prototype.GetStackName(inst.Argument2 + 1, inst.Position)} in {prototype.GetStackName(inst.Argument0, inst.Position)}) ip += {inst.Argument1} [{inst.Position + 1 + inst.Argument1}]");
                        break;
                    case Opcodes.PostForEach:
                        // postforeach subtracts 1 from inst.arg1 so offset doesn't need the extra 1 like other jumps
                        builder.Append($"if ({prototype.GetStackName(inst.Argument0, inst.Position)}) ip += {prototype.GetStackName(inst.Argument1, inst.Position)} [{inst.Argument1 + inst.Position}]");
                        break;

                    case Opcodes.Jz:
                        builder.Append($"if ({prototype.GetStackName(inst.Argument0, inst.Position)} == 0) ip += {inst.Argument1} [{inst.Position + 1 + inst.Argument1}]");
                        break;
                    case Opcodes.JCmp:
                        builder.Append($"if ({prototype.GetStackName(inst.Argument2, inst.Position)} ");
                        builder.Append(inst.Argument3 switch {
                            0 => ">",
                            2 => ">=",
                            3 => ">",
                            4 => ">=",
                            5 => "<=>",
                            _ => "????"
                        });
                        builder.Append($" {prototype.GetStackName(inst.Argument0, inst.Position)}) ");
                        builder.Append($"ip += {inst.Argument1} [{inst.Position + 1 + inst.Argument1}]");
                        break;
                    case Opcodes.Jmp:
                        builder.Append($"ip += {inst.Argument1} [{inst.Position + 1 + inst.Argument1}]");
                        break;

                    case Opcodes.NewObj:
                        switch (inst.Argument3) {
                            case 0:
                                builder.Append($"Table(InitialSize = {inst.Argument1})");
                                break;
                            case 1:
                                builder.Append($"Array(ReservedSize = {inst.Argument1})");
                                break;
                            case 2:
                                builder.Append($"Class(BaseClass = {inst.Argument1}, Attributes = {inst.Argument2:X8})");
                                break;
                            default:
                                builder.Append("Unknown type");
                                break;
                        }

                        break;
                    case Opcodes.AppendArray:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} += ");
                        switch (inst.Argument2) {
                            case 0:
                                builder.Append($"{prototype.GetStackName(inst.Argument1, inst.Position)}");
                                break;
                            case 1:
                                builder.Append($"{prototype.Literals[inst.Argument1].ToString(false)}");
                                break;
                            case 2:
                                builder.Append($"{inst.Argument1}");
                                break;
                            case 3:
                                builder.Append($"{inst.Argument1d}");
                                break;
                            case 4:
                                builder.Append($"{inst.Argument1 != 0}");
                                break;
                            default:
                                builder.Append("invalid");
                                break;
                        }

                        break;

                    case Opcodes.Closure:
                        builder
                            .Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = Closure {prototype.Functions[inst.Argument1].Name}");
                        break;
                    case Opcodes.PrepCall:
                    case Opcodes.PrepCallK:
                        builder
                            .Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = ")
                            .Append($"({prototype.GetStackName(inst.Argument3, inst.Position)} = {prototype.GetStackName(inst.Argument2, inst.Position)})")
                            .Append($"[{(inst.Opcode == Opcodes.PrepCallK ? prototype.Literals[inst.Argument1].ToString(false) : $"{prototype.GetStackName(inst.Argument1, inst.Position)}")}]");
                        break;
                    case Opcodes.Call:
                        builder.Append($"{prototype.GetStackName(inst.Argument1, inst.Position)}(");
                        for (int i = 0; i < inst.Argument3; i++) {
                            if (i != 0) builder.Append(", ");
                            builder.Append(prototype.GetStackName(inst.Argument2 + i, inst.Position));
                        }

                        builder.Append(")");
                        break;
                    case Opcodes.Move:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} <= {prototype.GetStackName(inst.Argument1, inst.Position)}");
                        break;
                    case Opcodes.NewSlot:
                        if (inst.Argument0 != 0xFF) builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = (");
                        builder
                            .Append($"{prototype.GetStackName(inst.Argument1, inst.Position)}[{prototype.GetStackName(inst.Argument2, inst.Position)}] <- ")
                            .Append($"{prototype.GetStackName(inst.Argument3, inst.Position)}");
                        if (inst.Argument0 != 0xFF) builder.Append(')');
                        break;
                    case Opcodes.Set:
                        if (inst.Argument0 != 0xFF) builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = (");
                        builder
                            .Append($"{prototype.GetStackName(inst.Argument1, inst.Position)}.")
                            .Append(prototype.GetStackName(inst.Argument2, inst.Position))
                            .Append($" = {prototype.GetStackName(inst.Argument3, inst.Position)}");
                        if (inst.Argument0 != 0xFF) builder.Append(")");
                        break;
                    case Opcodes.Get:
                        builder
                            .Append(prototype.GetStackName(inst.Argument0, inst.Position))
                            .Append($" = {($"{prototype.GetStackName(inst.Argument1, inst.Position)}.")}")
                            .Append($"{prototype.GetStackName(inst.Argument2, inst.Position)}");
                        break;
                    case Opcodes.GetK:
                        builder
                            .Append(prototype.GetStackName(inst.Argument0, inst.Position))
                            .Append($" = {prototype.GetStackName(inst.Argument2, inst.Position)}.")
                            .Append($"{prototype.Literals[inst.Argument1]}");
                        break;
                    case Opcodes.LoadRoot:
                        builder.Append($"{prototype.GetStackName(inst.Argument0, inst.Position)} = Root");
                        break;
                    case Opcodes.Return:
                        builder.Append(inst.Argument0 != 0xFF ? $"{prototype.GetStackName(inst.Argument1, inst.Position)}" : "$null");
                        break;
                    default:
                        builder.Append($"({inst.Opcode:D}) - todo implement this opcode");
                        break;
                }

            return builder.ToString();
        }

        public static string Disassemble(this FunctionPrototype prototype, string indent = "", bool showClosure = false, bool showLineJumps = true, int maxLineDigits = -1) {
            if (maxLineDigits < 0) maxLineDigits = MaxLineDigits(prototype).ToString().Length;
            // Console.WriteLine($"{maxLineDigits} {prototype.Name}");
            StringBuilder builder = new StringBuilder();
            builder.Append($"function {prototype.Name}(");
            for (int i = 0; i < prototype.Parameters.Length; i++) {
                if (i != 0) builder.Append(", ");
                if (prototype.VarParams && i == prototype.Parameters.Length - 1) builder.Append("...");
                else builder.Append(prototype.Parameters[i]);
            }

            builder.AppendLine($") {{ /* Max stack size {prototype.StackSize}, Outer var count: {prototype.OuterVars.Length} */");
            indent += "  ";
            int ip = 0;
            foreach (FunctionPrototype.Instruction inst in prototype.Instructions) {
                builder.Append($"{indent}{$"{ip++}".PadLeft(maxLineDigits, '0')}    ");
                if (inst.Opcode == Opcodes.Closure && showClosure)
                    builder.AppendLine($"{inst.ToString(prototype, true, false).PadRight(12)}{prototype.Functions[inst.Argument1].Disassemble(indent, showClosure, showLineJumps, maxLineDigits)}");
                else
                    builder.AppendLine(inst.ToString(prototype, true, true));
            }

            indent = indent[..^2];

            builder.Append($"{indent}}}");

            string result = builder.ToString();

            return result;
        }
    }
}