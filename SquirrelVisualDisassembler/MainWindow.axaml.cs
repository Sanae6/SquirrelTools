using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using SquirrelStuff.Analysis;
using SquirrelStuff.Bytecode;
using SquirrelStuff.Graphing;

#pragma warning disable 8618

namespace SquirrelVisualDisassembler {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class MainWindow : Window {
        private FunctionTextBinding? ftb;
        private FunctionTextBinding? CurrentFile {
            get => ftb;
            set {
                ftb = value;
                if (ftb == null) {
                    Title = DefaultTitle;
                    disassemblyEditor.Text = "";
                    decompilationEditor.Text = "";
                    graphEditor.Text = "";
                    return;
                }
                
                disassemblyEditor.Text = ftb.Disassembly;
                decompilationEditor.Text = ftb.Decompilation;
                graphEditor.Text = ftb.FlowGraph;
                Title = $"{DefaultTitle} - {ftb.File.Name}";
            }
        }
        private const string DefaultTitle = "Squirrel 3.1 Disassembler";
        public string CurrentDirectory;

        private readonly SemaphoreSlim TreeSemaphore = new SemaphoreSlim(1, 1);

        private MainWindowViewModel Model;
        private ControlFlowGraph CurrentGraph;
        private readonly IHighlightingDefinition asmSyntax;
        
        private TextEditor disassemblyEditor;
        private TextEditor decompilationEditor;
        private TextEditor graphEditor;
        private List<FileItem> itemsFiles;

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
            // disassemblyEditor.TextArea.TextView.ElementGenerators.Add(new ElementGenerator());
            this.AttachDevTools();
        }

        private void OnDisassemblyJump(int line) {
            disassemblyEditor.ScrollToLine(line);
        }

        private TextEditor GetEditor(string name) {
            TextEditor editor = this.FindControl<TextEditor>(name);
            editor.Background = Brushes.Transparent;
            editor.SyntaxHighlighting = asmSyntax;
            editor.Encoding = Encoding.UTF8;
            
            // editor.FontFamily = FontFamily.;
            return editor;
        }

        private void FileTreeOnSelectedItemChanged(object? sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count == 1) {
                if (e.AddedItems[0] is FileItem {Binding: {} binding}) CurrentFile = binding;
                else if (e.AddedItems[0] is FileItem { Binding: null } item) {
                    try {
                        LoadFile(item);
                    } catch {
                        // ignore
                    }
                }
            }
        }

        private FunctionTextBinding? LoadFile(FileItem item) {
            if (!File.Exists(item.Path)) return null;

            using BinaryReader reader = new BinaryReader(File.OpenRead(item.Path));
            FunctionPrototype function;
            try {
                function = BytecodeParser.Parse(reader);
            }
            catch (Exception) {
                return null;
            }

            FunctionTextBinding binding = new FunctionTextBinding {
                File = item,
                Prototype = function
            };

            try {
                binding.Disassembly = function.Disassemble(showClosure: true);
            }
            catch (Exception ex) {
                binding.Disassembly = $"Disassembly failed, ran into an exception: \n{ex}";
            }

            try {
                binding.Decompilation = Decompiler.Decompile(function);
            }
            catch (Exception ex) {
                binding.Decompilation = $"Decompilation failed, ran into an exception: \n{ex}";
            }

            try {
                binding.FlowGraph = GraphGenerator.GenerateGraph(CurrentGraph = GraphGenerator.BuildControlFlowGraph(function)).ToString();
            }
            catch (Exception ex) {
                binding.FlowGraph = $"Graph generation failed, ran into an exception: \n{ex}";
            }

            return item.Binding = binding;
        }

        private static IEnumerable<FileItem> Flatten(IEnumerable<Item> e) {
            IEnumerable<Item> items = e as Item[] ?? e.ToArray();
            IEnumerable<DirectoryItem> dirs = items.Where(item => item is DirectoryItem).Cast<DirectoryItem>();
            IEnumerable<FileItem> files = items.Where(item => item is FileItem).Cast<FileItem>();
            return dirs.SelectMany(dir => Flatten(dir.Items)).Concat(files);
        }

        public void UpdateTree(Action? extra = null!) {
            // if (Dispatcher.UIThread.CheckAccess()) {
            //     try {
            //         Update();
            //     }
            //     catch (Exception e) {
            //         Console.WriteLine(e);
            //         throw;
            //     }
            //
            //     return;
            // }

            async Task Update() {
                await TreeSemaphore.WaitAsync();
                try {
                    Model.Items = ItemProvider.GetItems(CurrentDirectory);
                    extra?.Invoke();
                    itemsFiles = Flatten(Model.Items).ToList();
                    // Model.Progress = 0;
                    // double done = 0;
                    // // Console.WriteLine($"{files.Count} {100}");
                    // Task[] tasks = itemsFiles.Select(file => {
                    //     // Thread.Sleep(50);
                    //     return Task.Run(() => {
                    //         try {
                    //             // Console.WriteLine($"Loading {file.Name} {done}/{files.Count}");
                    //             LoadFile(file);
                    //         } finally {
                    //             Model.Progress = 100.0 / itemsFiles.Count * ++done;
                    //             // Console.WriteLine($"Done {file.Name} {done}/{files.Count}");
                    //         }
                    //
                    //     });
                    // }).ToArray();
                    // Task.WaitAll(tasks);
                    Model.Progress = 100.0;
                } finally {
                    TreeSemaphore.Release(1);
                }
            }

            Task.Run(Update);

            // Dispatcher.UIThread.Post((Action) Update);
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
            watcher.EnableRaisingEvents = true;

            Console.WriteLine($"Open folder is {CurrentDirectory}");
            UpdateTree();
        }

        private void OnError(object sender, ErrorEventArgs e) {
            Debug.WriteLine(e.GetException());
        }

        private void OnRenamed(object sender, RenamedEventArgs e) {
            UpdateTree(() => {
                if (CurrentFile?.File.Path == e.OldFullPath) {
                    CurrentFile = ((FileItem) Model.Items.First(item => item.Path == e.FullPath)).Binding; // probably null?
                }
            });
        }

        private void OnDeleted(object sender, FileSystemEventArgs e) {
            Console.WriteLine($"something was deleted {e.FullPath}");
            UpdateTree();
        }

        private void OnCreated(object sender, FileSystemEventArgs e) {
            UpdateTree();
        }

        private void OnChanged(object sender, FileSystemEventArgs e) {
            if (PathEquals(CurrentFile?.File.Path, e.FullPath))
                LoadFile(CurrentFile!.File);
        }

        private async void OpenMenuOnClick(object? sender, RoutedEventArgs e) {
            if (await AskForNewDirectory()) UpdateTree();
        }

        private static bool PathEquals(string? left, string? right) => left != null && right != null && string.Equals(Path.GetFullPath(left).TrimEnd(Path.PathSeparator),
            Path.GetFullPath(right).TrimEnd(Path.PathSeparator), StringComparison.InvariantCultureIgnoreCase);

        private void ReloadAllMenuOnClick(object? sender, RoutedEventArgs e) {
            UpdateTree();
            CurrentFile = itemsFiles.Find(item => item.Path == CurrentFile?.File.Path)?.Binding;
        }

        private void ReloadCurrentMenuOnClick(object? sender, RoutedEventArgs e) {
            Console.WriteLine(CurrentFile is null);
            if (CurrentFile != null && itemsFiles.Find(item => item.Path == CurrentFile?.File.Path) is { } file) {
                CurrentFile = LoadFile(file);
            }
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged {
        private static MainWindowViewModel? instance;
        private List<Item> items;
        private string statusText = "Idle.";
        private Encoding currentEncoding = Encoding.UTF8;
        private double progress;
        private object lockDummy = new object();
        public static MainWindowViewModel Instance => instance ??= new MainWindowViewModel();
        public List<Item> Items {
            get => items;
            set {
                items = value;
                ChangeGuard(nameof(Items));
            }
        }
        public double Progress {
            get {
                lock (lockDummy) return progress;
            }
            set {
                lock (lockDummy) progress = value;
                ChangeGuard(nameof(Progress));
            }
        }
        public Encoding Encoding {
            get => currentEncoding;
            set {
                currentEncoding = value;
                ChangeGuard(nameof(Encoding));
            }
        }
        public event Action<int>? OnDisassemblyJumpClicked;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void ChangeGuard(string name) {
            if (Dispatcher.UIThread.CheckAccess()) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            else Dispatcher.UIThread.Post(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }

        public void DisassemblyJumpClick(int line) {
            OnDisassemblyJumpClicked?.Invoke(line);
        }
    }

    public class FunctionTextBinding {
        public FunctionPrototype Prototype { get; set; }
        public string Disassembly { get; set; }
        public string FlowGraph { get; set; }
        public string Decompilation { get; set; }
        public FileItem File { get; set; }
    }
}