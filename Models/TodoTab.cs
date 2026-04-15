namespace ToDo.Models;

public class TodoTab
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Tab";
    public List<TodoItem> Items { get; set; } = [];
}