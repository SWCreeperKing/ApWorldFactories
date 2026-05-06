using CreepyUtil.ClrCnsl;

namespace ApWorldFactories;

public class Folder(string name)
{
    public Color HighlightColor = new(200, 255, 200);
    public Color SelectedColor = Color.Black;
    public Color Color = Color.White;

    public string Name = name;
    public List<Folder> SubFolders = [];
    public List<string> Items = [];
    private bool Opened = false;

    private void CloseSubfolders()
    {
        foreach (var folder in SubFolders)
        {
            folder.Opened = false;
            folder.CloseSubfolders();
        }
    }

    public string? Display()
    {
        ClrCnsl.Clr();
        CloseSubfolders();
        Opened = true;
        var selected = 0;

        while (true)
        {
            object? selectedItem = null;

            var index = 0;

            DisplayFolder(this, ref index, ref selectedItem);

            switch (ClrCnsl.GetKey())
            {
                case ConsoleKey.Spacebar or ConsoleKey.Enter:
                    if (selectedItem is null) return null;
                    if (selectedItem is string s) return s;
                    if (selectedItem is Folder f)
                    {
                        if (f == this) return null;
                        f.Opened = !f.Opened;
                    }
                    break;
                
                case ConsoleKey.W or ConsoleKey.UpArrow:
                    selected--;
                    if (selected < 0) selected = index - 1; 
                    break;
                
                case ConsoleKey.S or ConsoleKey.DownArrow:
                    selected++;
                    if (selected > index - 1) selected = 0;
                    break;
            }

            ClrCnsl.Clr();
        }

        void DisplayFolder(
            Folder folder, ref int index, ref object? selectedItem, int indent = -1, bool isSubFolder = false,
            string prefix = ""
        )
        {
            if (selected == index) selectedItem = folder;
            var indentString = indent is -1 ? "" : "  ".Repeat(indent);

            if (folder.Length is 0)
            {
                ClrCnsl.WriteLine(
                    $"{indentString}{prefix}X {(selected == index ? $"[!{folder.HighlightColor}][#{folder.SelectedColor}]" : $"[#{folder.Color}]")}{folder.Name}"
                );
                index++;
                return;
            }

            if (!folder.Opened)
            {
                ClrCnsl.WriteLine(
                    $"{indentString}{prefix}> {(selected == index ? $"[!{folder.HighlightColor}][#{folder.SelectedColor}]" : $"[#{folder.Color}]")}{folder.Name}"
                );
                index++;
                return;
            }

            ClrCnsl.WriteLine(
                $"{indentString}{prefix}V {(selected == index ? $"[!{folder.HighlightColor}][#{folder.SelectedColor}]" : $"[#{folder.Color}]")}{folder.Name}"
            );
            index++;
            var i = 0;
            for (; i < folder.SubFolders.Count; i++)
            {
                var subfolder = folder.SubFolders[i];
                DisplayFolder(
                    subfolder, ref index, ref selectedItem, indent + 1, true, "| "
                );
                if (selected == index) selectedItem = subfolder;
            }

            for (var j = 0; j < folder.Items.Count; j++)
            {
                var item = folder.Items[j];
                ClrCnsl.WriteLine(
                    $"  {indentString}|- {(selected == index ?  $"[!{folder.HighlightColor}][#{folder.SelectedColor}]" : $"[#{folder.Color}]")}{item}"
                );
                if (selected == index) selectedItem = item;
                index++;
            }
        }
    }

    public int Length => SubFolders.Count + Items.Count;
}