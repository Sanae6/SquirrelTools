using System.Collections.Generic;
using System.Text;

namespace SquirrelStuff.Graphing {
    public class Graph {
        /*
         * digraph G {
             node [fontsize = "12"];
             edge [fontsize = "8"];
             "0--1" -> "6--1";
             "0--1" -> "2--1";
             0 [label="0--1"];
             6 [label="6--1"];
             2 [label="2--1"];
            }
         */
        private record Edge(string Head, string Tail, string Label);

        private Dictionary<string, string> vertices = new Dictionary<string, string>();
        private List<Edge> edges = new List<Edge>();
        private readonly string name;

        public Graph(string name) {
            this.name = name;
        }
        public void AddVertex(string index, string label) => vertices.TryAdd(index, label);

        public void AddEdge(string parent, string child, string label = "") => edges.Add(new Edge(parent, child, label)); 

        public override string ToString() {
            StringBuilder builder = new StringBuilder();
            const string indent = "    ";
            builder.AppendLine($"digraph {name} {{");
            builder.AppendLine($@"{indent}node [fontsize = ""20"", fontname=""Comic Sans MS"", shape=""plaintext""]");
            builder.AppendLine($@"{indent}edge [fontsize = ""14""]");
            builder.AppendLine($@"{indent}rankdir=LR");
            foreach ((string index, string label) in vertices) {
                builder.AppendLine($@"{indent}{index} [label={label}];");
            }
            foreach (Edge edge in edges) {
                string newLabel = edge.Label.Replace("\n", "\\n").Replace("\r", "").Replace("\"","\\\"");
                builder.AppendLine($@"{indent}{edge.Head} -> {edge.Tail} [label=""{newLabel}""];");
            }
            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}