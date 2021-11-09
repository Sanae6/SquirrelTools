using System;
using System.Globalization;
using System.Linq;
using System.Text;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Analysis {
    public static class Disassembler {
        // friendly disassembly
        private static int MaxLineDigits(FunctionPrototype prototype) {
            int max = prototype.Functions.Select(MaxLineDigits).Prepend(prototype.Instructions.Length).Max();
            Console.WriteLine($"{max} {prototype.Name.ToString()} {max.ToString()} {max.ToString().Length}");
            return max;
        }


        public static string ToString(this FunctionPrototype.Instruction inst, FunctionPrototype prototype, bool showArgs) {
            if (!showArgs) {
                return inst.Opcode.ToString();
            }

            StringBuilder builder = new StringBuilder();
            builder.Append($"{inst.Opcode.ToString()} ".PadRight(12));
            switch (inst.Opcode) {
                case Opcodes.Load:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.Literals[inst.Argument1].ToString(false)}");
                    break;
                case Opcodes.LoadInt:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {inst.Argument1}");
                    break;
                case Opcodes.LoadBool:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {inst.Argument1 != 0}");
                    break;
                case Opcodes.LoadFloat:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {BitConverter.ToSingle(BitConverter.GetBytes(inst.Argument1)).ToString(CultureInfo.CurrentCulture)}");
                    break;
                case Opcodes.DLoad: // load two literals
                    builder.Append($"{prototype.GetStackName(inst.Argument0)} = {prototype.Literals[inst.Argument1]}, ")
                        .AppendLine($"{prototype.GetStackName(inst.Argument2)} = {prototype.Literals[inst.Argument3]}");
                    break;
                case Opcodes.LoadNulls:
                    builder.AppendLine($"{inst.Argument1} nulls starting at {inst.Argument0}");
                    break;

                case Opcodes.Add:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} + {prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.Sub:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} - {prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.Mul:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} * {prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.Div:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} / {prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.Mod:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} % {prototype.GetStackName(inst.Argument1)}");
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
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} {op} {prototype.GetStackName(inst.Argument1)}");
                    break;

                case Opcodes.And:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} && {prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.Or:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} && {prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.Neg:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = -{prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.Not:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = !{prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.Cmp:
                    builder.Append($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} ");
                    builder.Append(inst.Argument3 switch {
                        0 => ">",
                        2 => ">=",
                        3 => ">",
                        4 => ">=",
                        5 => "<=>",
                        _ => "????",
                    });
                    builder.AppendLine($" {prototype.GetStackName(inst.Argument1)}");
                    break;

                case Opcodes.Eq:
                    builder.Append($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} == ");
                    builder.AppendLine(inst.Argument3 != 0 ? prototype.Literals[inst.Argument1].ToString(true) : $"{inst.Argument1}");
                    break;
                case Opcodes.Ne:
                    builder.Append($"{prototype.GetStackName(inst.Argument0)} = {prototype.GetStackName(inst.Argument2)} != ");
                    builder.AppendLine(inst.Argument3 != 0 ? prototype.Literals[inst.Argument1].ToString(true) : $"{inst.Argument1}");
                    break;

                case Opcodes.Jz:
                    builder.AppendLine($"if ({prototype.GetStackName(inst.Argument0)} == 0) ip += {inst.Argument1}");
                    break;
                case Opcodes.JCmp:
                    builder.Append($"if ({prototype.GetStackName(inst.Argument2)} ");
                    builder.Append(inst.Argument3 switch {
                        0 => ">",
                        2 => ">=",
                        3 => ">",
                        4 => ">=",
                        5 => "<=>",
                        _ => "????",
                    });
                    builder.Append($" {prototype.GetStackName(inst.Argument0)}) ");
                    builder.AppendLine($"ip += {inst.Argument1}");
                    break;
                case Opcodes.Jmp:
                    builder.AppendLine($"ip += {inst.Argument1}");
                    break;

                case Opcodes.NewObj:
                    switch (inst.Argument3) {
                        case 0:
                            builder.AppendLine($"Table(InitialSize = {inst.Argument1})");
                            break;
                        case 1:
                            builder.AppendLine($"Array(ReservedSize = {inst.Argument1})");
                            break;
                        case 2:
                            builder.AppendLine($"Class( = {inst.Argument1})");
                            break;
                        default:
                            builder.AppendLine("Unknown type");
                            break;
                    }

                    break;
                case Opcodes.AppendArray:
                    builder.AppendLine();
                    break;

                case Opcodes.Closure:
                    builder
                        .Append($"{prototype.GetStackName(inst.Argument0)} = Closure {prototype.Functions[inst.Argument1].Name}");
                    break;
                case Opcodes.PrepCall:
                case Opcodes.PrepCallK:
                    builder
                        .Append($"{prototype.GetStackName(inst.Argument0)} = ")
                        .Append($"({prototype.GetStackName(inst.Argument3)} = {prototype.GetStackName(inst.Argument2)})")
                        .AppendLine($"[{(inst.Opcode == Opcodes.PrepCallK ? prototype.Literals[inst.Argument1].ToString(false) : $"{prototype.GetStackName(inst.Argument1)}")}]");
                    break;
                case Opcodes.Call:
                    builder.Append($"{prototype.GetStackName(inst.Argument1)}(");
                    for (int i = 0; i < inst.Argument3; i++) {
                        if (i != 0) builder.Append(", ");
                        builder.Append(prototype.GetStackName(inst.Argument2 + i));
                    }

                    builder.AppendLine(")");
                    break;
                case Opcodes.Move:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} <= {prototype.GetStackName(inst.Argument1)}");
                    break;
                case Opcodes.NewSlot:
                    builder
                        .Append($"Self = {prototype.GetStackName(inst.Argument1)}, ")
                        .Append($"Key = {prototype.GetStackName(inst.Argument2)}, ")
                        .AppendLine($"Value = {prototype.GetStackName(inst.Argument3)} ");
                    break;
                case Opcodes.Set:
                    if (inst.Argument0 != 0xFF) builder.Append($"{prototype.GetStackName(inst.Argument0)} = (");
                    builder
                        .Append($"{(inst.Argument1 == 0 ? "::" : $"{prototype.GetStackName(inst.Argument1)}.")}")
                        .Append(prototype.GetStackName(inst.Argument2))
                        .Append($" = {prototype.GetStackName(inst.Argument3)}");
                    if (inst.Argument0 != 0xFF) builder.AppendLine(")");
                    else builder.AppendLine();
                    break;
                case Opcodes.Get:
                    builder
                        .Append(prototype.GetStackName(inst.Argument0))
                        .Append($" = {(inst.Argument1 == 0 ? "::" : $"{prototype.GetStackName(inst.Argument1)}.")})")
                        .AppendLine($"{prototype.GetStackName(inst.Argument2)}");
                    break;
                case Opcodes.GetK:
                    builder
                        .Append(prototype.GetStackName(inst.Argument0))
                        .Append($" = {(inst.Argument2 == 0 ? "::" : $"{prototype.GetStackName(inst.Argument2)}.")}")
                        .AppendLine($"{prototype.Literals[inst.Argument1]}");
                    break;
                case Opcodes.LoadRoot:
                    builder.AppendLine($"{prototype.GetStackName(inst.Argument0)} = Root");
                    break;
                case Opcodes.Return:
                    builder.AppendLine(inst.Argument0 != 0xFF ? $"{prototype.GetStackName(inst.Argument1)}" : "$null");
                    break;
                default:
                    builder.AppendLine($"({inst.Opcode:D}) - todo implement this opcode");
                    break;
            }

            return builder.ToString();
        }

        public static string Disassemble(this FunctionPrototype prototype, string indent = "", bool showClosure = false, int maxLineDigits = -1) {
            if (maxLineDigits < 0) maxLineDigits = MaxLineDigits(prototype).ToString().Length;
            Console.WriteLine($"{maxLineDigits} {prototype.Name}");
            StringBuilder builder = new StringBuilder();
            builder.Append($"function {prototype.Name}(");
            for (int i = 0; i < prototype.Parameters.Length; i++) {
                if (i != 0) builder.Append(", ");
                if (prototype.VarParams && i == prototype.Parameters.Length - 1) builder.Append("...");
                else builder.Append(prototype.Parameters[i]);
            }

            builder.AppendLine(") {");
            indent += "  ";
            int ip = 0;
            foreach (FunctionPrototype.Instruction inst in prototype.Instructions) {
                builder.Append($"{$"{ip++}".PadLeft(maxLineDigits, '0')}{indent}");
                if (inst.Opcode == Opcodes.Closure && showClosure) {
                    builder.Append($"{inst.ToString(prototype, false).PadRight(12)}{prototype.Functions[inst.Argument1].Disassemble(indent, showClosure, maxLineDigits)}");
                } else {
                    builder.Append($"{inst.ToString(prototype, true)}");
                }
            }

            // todo find out what outer variables are used for
            // if (prototype.OuterVars.Length > 0) {
            //     builder.AppendLine();
            //     builder.AppendLine($"{indent}Outer Vars (Count = {prototype.OuterVars.Length})");
            //     indent += "  ";
            //     foreach (FunctionPrototype.OuterVar outer in prototype.OuterVars) {
            //         builder.AppendLine($"{indent}{outer.Src}:{outer.Name} = {outer.Type}");
            //     }
            //
            //     indent = indent[..^2];
            // }

            indent = indent[..^2];

            builder.AppendLine($"{indent}}}");

            return builder.ToString();
        }
    }
}