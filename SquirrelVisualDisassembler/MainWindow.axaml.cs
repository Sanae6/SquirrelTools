using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Rendering;
using SquirrelStuff.Analysis;
using SquirrelStuff.Bytecode;
using SquirrelStuff.Graphing;

#pragma warning disable 8618

namespace SquirrelVisualDisassembler {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class MainWindow : Window {
        private FunctionTextBinding CurrentFile;
        private const string DefaultTitle = "Squirrel 3.1 Disassembler";
        public string CurrentDirectory;

        private MainWindowViewModel Model;
        private ControlFlowGraph CurrentGraph;
        private readonly IHighlightingDefinition asmSyntax;
        
        private TextEditor disassemblyEditor;
        private TextEditor decompilationEditor;
        private TextEditor graphEditor;

        public MainWindow() {
            DataContext = Model = MainWindowViewModel.Instance;
            using (XmlTextReader reader = new XmlTextReader("sqasm.xshd")) {
                asmSyntax = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
            HighlightingManager.Instance.RegisterHighlighting("sqasm", Array.Empty<string>(), asmSyntax);

            AvaloniaXamlLoader.Load(this);
            disassemblyEditor = GetEditor("DisassemblyEditor");
            graphEditor = GetEditor("GraphEditor");
            decompilationEditor = GetEditor("DecompileEditor");
            Model.OnDisassemblyJumpClicked += OnDisassemblyJump;
            disassemblyEditor.TextArea.TextView.ElementGenerators.Add(new JumpGenerator());
        }

        private void OnDisassemblyJump(int line) {
            disassemblyEditor.ScrollToLine(line);
        }

        private TextEditor GetEditor(string name) {
            TextEditor editor = this.FindControl<TextEditor>(name);
            editor.Background = Brushes.Transparent;
            editor.SyntaxHighlighting = asmSyntax;
            return editor;
        }

        private void FileTreeOnSelectedItemChanged(object? sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count == 1 && e.AddedItems[0] is FileItem item) LoadFile(item);
        }

        private void LoadFile(FileItem item) {
            if (!File.Exists(item.Path)) return;

            if (Dispatcher.UIThread.CheckAccess()) {
                using BinaryReader reader = new BinaryReader(File.OpenRead(item.Path));
                FunctionPrototype function;
                try {
                    function = BytecodeParser.Parse(reader);
                }
                catch (Exception e) {
                    // dialog.Show(this, $"Failed to open {item.Name}.\n {e}", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    Console.WriteLine(e);
                    return;
                }

                CurrentFile = new FunctionTextBinding {
                    File = item,
                    Prototype = function
                };

                Title = $"{DefaultTitle} - {CurrentFile.File.Name}";
                try {
                    disassemblyEditor.Text = Model.DisassemblyText = function.Disassemble(showClosure: true);
                }
                catch (Exception ex) {
                    disassemblyEditor.Text = $"Disassembly failed, ran into an exception: \n{ex}";
                }

                try {
                    decompilationEditor.Text = ""; // DecompilerDecompile(function);
                }
                catch (Exception ex) {
                    Model.DecompileText = $"Decompilation failed, ran into an exception: \n{ex}";
                }

                try {
                    graphEditor.Text = GraphGenerator.GenerateGraph(CurrentGraph = GraphGenerator.BuildControlFlowGraph(function)).ToString();
                }
                catch (Exception ex) {
                    graphEditor.Text = $"Graph generation failed, ran into an exception: \n{ex}";
                }
            } else {
                Dispatcher.UIThread.Post(() => LoadFile(item));
            }
        }

        public void UpdateTree(Action extra = null!) {
            if (Dispatcher.UIThread.CheckAccess()) {
                try {
                    Update();
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                    throw;
                }

                return;
            }

            void Update() {
                Model.Items = ItemProvider.GetItems(CurrentDirectory);
                extra?.Invoke();
            }

            Dispatcher.UIThread.Post((Action) Update);
        }

        private async Task<bool> AskForNewDirectory() {
            OpenFolderDialog vfb = new OpenFolderDialog {
                Title = "Pick the folder where your Squirrel nuts are stored"
            };
            string? directory = await vfb.ShowAsync(this);
            if (directory is null) return false;
            CurrentDirectory = directory;
            return true;
        }

        private async void OnLoad(object? _, EventArgs _2) {
            // return;
            string[] cla = Environment.GetCommandLineArgs();
            if (cla.Length == 2 && Directory.Exists(cla[1])) CurrentDirectory = cla[1];
            else if (!await AskForNewDirectory()) Environment.Exit(0);

            FileSystemWatcher watcher = new FileSystemWatcher(CurrentDirectory);
            watcher.NotifyFilter = NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Security
                                   | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            watcher.Filter = "*.nut";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = false;

            Console.WriteLine($"Open folder is {CurrentDirectory}");
            UpdateTree();
        }

        private void OnError(object sender, ErrorEventArgs e) {
            Debug.WriteLine(e.GetException());
        }

        private void OnRenamed(object sender, RenamedEventArgs e) {
            UpdateTree(() => {
                if (CurrentFile?.File.Path == e.OldFullPath) {
                    LoadFile((FileItem) Model.Items.First(item => item.Path == e.FullPath));
                }
            });
        }

        private void OnDeleted(object sender, FileSystemEventArgs e) {
            UpdateTree();
        }

        private void OnCreated(object sender, FileSystemEventArgs e) {
            UpdateTree();
        }

        private void OnChanged(object sender, FileSystemEventArgs e) {
            if (string.Equals(Path.GetFullPath(CurrentFile.File.Path).TrimEnd(Path.PathSeparator),
                Path.GetFullPath(e.FullPath).TrimEnd(Path.PathSeparator),
                StringComparison.InvariantCultureIgnoreCase))
                LoadFile(CurrentFile.File);
        }

        private async void OpenMenuOnClick(object? sender, RoutedEventArgs e) {
            if (await AskForNewDirectory()) UpdateTree();
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged {
        private static MainWindowViewModel? instance;
        private List<Item> items;
        private string disassemblyText = "Select a file to disassemble it";
        private string decompileText = "Select a file to decompile it";
        private string flowText = "Select a file to view its control flow";
        public static MainWindowViewModel Instance => instance ??= new MainWindowViewModel();
        public string DisassemblyText {
            get => disassemblyText;
            set {
                disassemblyText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisassemblyText)));
            }
        }
        public string DecompileText {
            get => decompileText;
            set {
                decompileText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DecompileText)));
            }
        }
        public string FlowText {
            get => flowText;
            set {
                flowText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlowText)));
            }
        }
        public List<Item> Items {
            get => items;
            set {
                items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            }
        }
        public event Action<int>? OnDisassemblyJumpClicked;
        public Encoding Encoding => Encoding.UTF8;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void DisassemblyJumpClick(int line) {
            OnDisassemblyJumpClicked?.Invoke(line);
        }
    }

    public class FunctionTextBinding {
        public FunctionPrototype Prototype { get; set; }
        public FileItem File { get; set; }
    }
    
    class ElementGenerator : VisualLineElementGenerator, IComparer<KeyValuePair<int, IControl>>
    {
        public List<KeyValuePair<int, IControl>> controls = new List<KeyValuePair<int, IControl>>();

        /// <summary>
        /// Gets the first interested offset using binary search
        /// </summary>
        /// <returns>The first interested offset.</returns>
        /// <param name="startOffset">Start offset.</param>
        public override int GetFirstInterestedOffset(int startOffset)
        {
            int pos = controls.BinarySearch(new KeyValuePair<int, IControl>(startOffset, null), this);
            if (pos < 0)
                pos = ~pos;
            if (pos < controls.Count)
                return controls[pos].Key;
            else
                return -1;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            int pos = controls.BinarySearch(new KeyValuePair<int, IControl>(offset, null!), this);
            if (pos >= 0)
                return new InlineObjectElement(0, controls[pos].Value);
            else
                return null!;
        }

        int IComparer<KeyValuePair<int, IControl>>.Compare(KeyValuePair<int, IControl> x, KeyValuePair<int, IControl> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}