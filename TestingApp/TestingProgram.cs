using System;
using System.IO;
using Graphviz4Net;
using SquirrelStuff.Analysis;
using SquirrelStuff.Bytecode;
using SquirrelStuff.Graphing;

namespace TestingApp {
    internal static class TestingProgram {
        private static void Main(string[] args) {
            BinaryReader binaryReader = new BinaryReader(new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            FunctionPrototype root = BytecodeParser.Parse(binaryReader);

            void PrintFunction(FunctionPrototype parent) {
                Console.WriteLine(parent.Disassemble(showClosure: true));
            }

            PrintFunction(root);
            ControlFlowGraph cfg = GraphGenerator.BuildControlFlowGraph(root);
            Graph graph = GraphGenerator.GenerateGraph(cfg);
            Console.WriteLine(graph.ToString());

            DotExeRunner runner = new DotExeRunner();
            runner.DotExecutablePath = Path.GetDirectoryName(FindExePath(runner.DotExecutable));
            TextReader reader = runner.Run(writer => {
                writer.WriteLine(graph.ToString());
            }, "png:cairo");
            File.WriteAllText($"{args[0]}.png", reader.ReadToEnd());
            Console.WriteLine();
        }

        public static string FindExePath(string exe) {
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (File.Exists(exe)) return Path.GetFullPath(exe);
            if (Path.GetDirectoryName(exe) != String.Empty) throw new FileNotFoundException(new FileNotFoundException().Message, exe);
            foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';')) {
                string path = test.Trim();
                if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                    return Path.GetFullPath(path);
            }

            throw new FileNotFoundException(new FileNotFoundException().Message, exe);

        }
    }
}