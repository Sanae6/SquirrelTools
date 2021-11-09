using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SquirrelVisualDisassembler {
    public class Item {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class FileItem : Item { }
    
    public class DirectoryItem : Item {
        public List<Item> Items { get; set; }

        public DirectoryItem() {
            Items = new List<Item>();
            
        }
    }

    public static class ItemProvider {
        public static List<Item> GetItems(string path) {
            DirectoryInfo dirInfo = new DirectoryInfo(path);

            List<Item> items = dirInfo.GetDirectories().Select(directory => new DirectoryItem {Name = directory.Name, Path = directory.FullName, Items = GetItems(directory.FullName)}).Cast<Item>().ToList();
            items.AddRange(dirInfo.GetFiles().Where(file => file.Extension == ".nut").Select(file => new FileItem {Name = file.Name, Path = file.FullName}));

            return items;
        }
    }
}