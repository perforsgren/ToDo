using ToDo.Models;

namespace ToDo.Models;

public class AppData
{
    public List<TodoTab> Tabs { get; set; } = [];
    public int SelectedTabIndex { get; set; }
    public WindowBounds? WindowBounds { get; set; }
}

public class WindowBounds
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}