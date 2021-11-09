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
        private record Edge(int Head, int Tail, string Label);

        private Dictionary<int, string> vertices = new Dictionary<int, string>();
        private List<Edge> edges = new List<Edge>();
        private readonly string name;

        public Graph(string name) {
            this.name = name;
        }
        public void AddVertex(int index, string label) => vertices.TryAdd(index, label);

        public void AddEdge(int parent, int child, string label = "") => edges.Add(new Edge(parent, child, label)); 

        public override string ToString() {
            StringBuilder builder = new StringBuilder();
            const string indent = "    ";
            builder.AppendLine($"digraph {name} {{");
            builder.AppendLine($@"{indent}node [fontsize = ""15""]");
            builder.AppendLine($@"{indent}edge [fontsize = ""10""]");
            foreach ((int index, string label) in vertices) {
                builder.AppendLine($@"{indent}{index} [label=""{label}""];");
            }
            foreach (Edge edge in edges) {
                builder.AppendLine($@"{indent}{edge.Head} -> {edge.Tail} [label=""{edge.Label}""];");
            }
            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}