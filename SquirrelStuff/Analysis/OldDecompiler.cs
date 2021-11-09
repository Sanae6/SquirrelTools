// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using SquirrelStuff.Bytecode;
//
// namespace SquirrelStuff.Decompiler {
//     public class Decompiler {
//         public const int StackSize = 2048;
//
//         private const string Indent = "    ";
//         private int indentationLevel;
//
//         public int IndentationLevel {
//             get => indentationLevel;
//             set {
//                 indentationLevel = value;
//
//                 StringBuilder sb = new StringBuilder();
//                 for (int i = 0; i < IndentationLevel; i++) {
//                     sb.Append(Indent);
//                 }
//
//                 Indentation = sb.ToString();
//             }
//         }
//
//         public class Block {
//             public FunctionPrototype.Instruction Start;
//             public FunctionPrototype.Instruction End;
//         }
//         
//         
//         public string Indentation { get; private set; } = "";
//
//         internal string DecompileFunction(FunctionPrototype prototype) {
//             StringBuilder builder = new StringBuilder();
//             // DecompObject?[] stack = new DecompObject[StackSize];
//             List<Expression> expressions = new List<Expression>();
//             Variable?[] vars = new Variable?[StackSize];
//             int instPos = 0;
//
//             FunctionExpression funcExpression = new FunctionExpression(prototype, expressions);
//
//             Variable? GetLocalVar(int location) {
//                 return vars.FirstOrDefault(localVar => localVar.Position == location && (instPos <= localVar.Local.EndOp || instPos >= localVar.Local.StartOp));
//             }
//
//             string GetStackName(int location) {
//                 return GetLocalVar(location)?.Name.ToString() /* todo add params to decomp output*/ ?? $"$stackVar{location}";
//             }
//
//             int StackResolve(int location) {
//                 return location < 0 ? StackSize - location : location - 1;
//             }
//
//             // foreach (FunctionPrototype.Instruction inst in prototype.Instructions) {
//             //     switch (inst.Opcode) {
//             //         case Opcodes.LoadRoot:
//             //             expressions.Add(new LiteralExpression(DecompObject.RootTable));
//             //             break;
//             //         case Opcodes.Load:
//             //             StackResolve(inst.Argument0) = new LiteralExpression(DecompObject.From(prototype.Literals[inst.Argument1]));
//             //             break;
//             //         case Opcodes.LoadBool:
//             //             StackResolve(inst.Argument0) = new LiteralExpression(DecompObject.Bool(inst.Argument1 != 0));
//             //             break;
//             //         case Opcodes.LoadFloat:
//             //             StackResolve(inst.Argument0) = new LiteralExpression(DecompObject.Float(inst.Argument1));
//             //             break;
//             //         case Opcodes.LoadInt:
//             //             StackResolve(inst.Argument0) = new LiteralExpression(DecompObject.Int(inst.Argument1));
//             //             break;
//             //         case Opcodes.Closure:
//             //             StackResolve(inst.Argument0) = DecompObject.Closure(prototype.Functions[inst.Argument1]);
//             //             break;
//             //         case Opcodes.Call:
//             //             break;
//             //     }
//             //
//             //     instPos++;
//             // }
//
//             //todo lol
//             return "";
//         }
//
//         public static string Decompile(FunctionPrototype prototype) {
//             Decompiler decompiler = new Decompiler();
//             return decompiler.DecompileFunction(prototype);
//         }
//
//         private class Variable {
//             public string Name;
//             public Expression Expression;
//             public FunctionPrototype.LocalVar Local;
//             public uint Position => Local.Pos;
//         }
//     }
// }