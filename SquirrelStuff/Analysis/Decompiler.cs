using System.Text;
using SquirrelStuff.Bytecode;

namespace SquirrelStuff.Analysis {
    public class Decompiler {
        public const int StackSize = 2048;

        private const string Indent = "    ";
        private int indentationLevel;
        public string Indentation { get; private set; } = "";

        public int IndentationLevel {
            get => indentationLevel;
            set {
                indentationLevel = value;

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < IndentationLevel; i++) {
                    sb.Append(Indent);
                }

                Indentation = sb.ToString();
            }
        }

        public static void Decompile(FunctionPrototype prototype) {
            
        }
    }
}