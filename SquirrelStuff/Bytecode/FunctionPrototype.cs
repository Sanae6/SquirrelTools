using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

#pragma warning disable 8618

namespace SquirrelStuff.Bytecode {
    public class FunctionPrototype {
        public const int Part = 0x50415254;

        public FunctionPrototype? Parent;
        public SquirrelObject SourceName;
        public SquirrelObject Name;

        public SquirrelObject[] Literals;
        public SquirrelObject[] Parameters;
        public OuterVar[] OuterVars;
        public LocalVar[] LocalVars;
        public LineInfo[] Lines;
        public int[] DefaultParams;
        public Instruction[] Instructions;
        public FunctionPrototype[] Functions;

        public int StackSize;
        public int ActualStackSize => StackSize + Parameters.Length;
        public bool IsGenerator;
        public bool VarParams;

        // todo support 64 bit squirrel generated files 
        public static FunctionPrototype ReadFunction(BinaryReader reader, FunctionPrototype? parent = null) {
            reader.AssertTag(Part, "PART tag");

            FunctionPrototype prototype = new FunctionPrototype {
                SourceName = reader.ReadObject(),
                Name = reader.ReadObject(),
                Parent = parent
            };

            reader.AssertTag(Part, "PART tag");
            prototype.Literals = new SquirrelObject[reader.ReadInt32()];
            prototype.Parameters = new SquirrelObject[reader.ReadInt32()];
            prototype.OuterVars = new OuterVar[reader.ReadInt32()];
            prototype.LocalVars = new LocalVar[reader.ReadInt32()];
            prototype.Lines = new LineInfo[reader.ReadInt32()];
            prototype.DefaultParams = new int[reader.ReadInt32()];
            prototype.Instructions = new Instruction[reader.ReadInt32()];
            prototype.Functions = new FunctionPrototype[reader.ReadInt32()];

            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.Literals.Length; i++) prototype.Literals[i] = reader.ReadObject();

            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.Parameters.Length; i++) prototype.Parameters[i] = reader.ReadObject();

            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.OuterVars.Length; i++)
                prototype.OuterVars[i] = new OuterVar {
                    Type = (OuterType) reader.ReadUInt32(),
                    Src = reader.ReadObject(),
                    Name = reader.ReadObject(),
                };

            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.LocalVars.Length; i++)
                prototype.LocalVars[i] = new LocalVar {
                    Name = reader.ReadObject(),
                    Pos = reader.ReadUInt32(),
                    StartOp = reader.ReadUInt32(),
                    EndOp = reader.ReadUInt32(),
                };

            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.Lines.Length; i++)
                prototype.Lines[i] = new LineInfo {
                    Line = reader.ReadInt32(),
                    Op = reader.ReadInt32(),
                };

            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.DefaultParams.Length; i++) prototype.DefaultParams[i] = reader.ReadInt32();

            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.Instructions.Length; i++)
                prototype.Instructions[i] = new Instruction {
                    Position = i,
                    Argument1 = reader.ReadInt32(),
                    Opcode = (Opcodes) reader.ReadByte(),
                    Argument0 = reader.ReadByte(),
                    Argument2 = reader.ReadByte(),
                    Argument3 = reader.ReadByte()
                };

            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.Functions.Length; i++) prototype.Functions[i] = ReadFunction(reader, prototype);

            prototype.StackSize = reader.ReadInt32();
            prototype.IsGenerator = reader.ReadBoolean();
            prototype.VarParams = reader.ReadInt32() != 0;

            return prototype;
        }

        public LocalVar? GetLocalVar(int location, int ip) {
            ip++;
            return LocalVars.FirstOrDefault(local => {
                // Console.WriteLine($"Trying {local.Name}: {location}=={local.Pos} {local.StartOp}<{ip}<{local.EndOp}");
                return local.Pos == location && local.IsDefinedAt(ip);
            });
        }

        public int GetLine(int ip) {
            int low = 0;
            int high = Lines.Length - 1;
            int mid = 0;
            while (low <= high) {
                mid = low + ((high - low) >> 1);
                int curOp = Lines[mid].Op;
                if (curOp > ip) {
                    high = mid - 1;
                } else if (curOp < ip) {
                    if (mid < Lines.Length - 1
                        && Lines[mid + 1].Op >= ip) {
                        break;
                    }

                    low = mid + 1;
                } else { //equal
                    break;
                }
            }

            while (mid > 0 && Lines[mid].Op >= ip) mid--;
            return Lines[mid].Line;
        }

        public SquirrelObject? GetStackNameObject(int location, int ip) => GetLocalVar(location, ip)?.Name ?? Parameters.ElementAtOrDefault(location);

        public string GetStackName(int location, int ip) {
            return GetStackNameObject(location, ip)?.ToString() ?? $"$stackVar{location}";
        }

        public bool HasNamedLocal(int sp, int ip) {
            return GetLocalVar(sp, ip) is {Name: not null} || Parameters.ElementAtOrDefault(sp) is not null;
        }

        public LocalVar? GetLocal(int sp, int ip) {
            return GetLocalVar(sp, ip) ?? (Parameters.ElementAtOrDefault(sp) is { } obj ? new LocalVar {
                StartOp = 0,
                EndOp = (uint) (Instructions.Length - 1),
                Pos = (uint) sp,
                Name = obj
            } : null);
        }

        public class LocalVar {
            public SquirrelObject Name;
            public uint StartOp;
            public uint EndOp;
            public uint Pos;

            public bool StartsAt(int ip) => ++ip == StartOp;
            public bool IsDefinedAt(int ip) => StartOp <= ip && ip <= EndOp;
            public override string ToString() => Name.ToString();
        }

        public enum OuterType {
            OtLocal = 0,
            OtOuter = 1
        };

        public class OuterVar {
            public OuterType Type;
            public SquirrelObject Src;
            public SquirrelObject Name;
        }

        public class LineInfo {
            public int Line;
            public int Op;
        }

        public class Instruction {
            public int Position;
            public Opcodes Opcode;
            public int Argument1; // todo long support
            public float Argument1f => BitConverter.ToSingle(BitConverter.GetBytes(Argument1)); // todo double support
            public byte Argument0;
            public byte Argument2;
            public byte Argument3;
        }
    }
}