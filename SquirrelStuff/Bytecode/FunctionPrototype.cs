﻿using System;
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
            for (int i = 0; i < prototype.OuterVars.Length; i++) prototype.OuterVars[i] = new OuterVar {
                Type = (OuterType) reader.ReadUInt32(),
                Src = reader.ReadObject(),
                Name = reader.ReadObject(),
            };
            
            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.LocalVars.Length; i++) prototype.LocalVars[i] = new LocalVar {
                Name = reader.ReadObject(),
                Pos = reader.ReadUInt32(),
                StartOp = reader.ReadUInt32(),
                EndOp = reader.ReadUInt32(),
            };
            
            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.Lines.Length; i++) prototype.Lines[i] = new LineInfo {
                Line = reader.ReadInt32(),
                Op = reader.ReadInt32(),
            };
            
            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.DefaultParams.Length; i++) prototype.DefaultParams[i] = reader.ReadInt32();
            
            reader.AssertTag(Part, "PART tag");
            for (int i = 0; i < prototype.Instructions.Length; i++) prototype.Instructions[i] = new Instruction {
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

        public LocalVar? GetLocalVar(int location) {
            return LocalVars.FirstOrDefault(local => local.Pos == location);
        }

        public string GetStackName(int location) {
            return GetLocalVar(location)?.Name.ToString() ?? Parameters.ElementAtOrDefault(location)?.ToString() ?? $"$stackVar{location}";
        }

        public class LocalVar {
            public SquirrelObject Name;
            public uint StartOp;
            public uint EndOp;
            public uint Pos;
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
            public int Argument1;
            public byte Argument0;
            public byte Argument2;
            public byte Argument3;
        }
    }
}