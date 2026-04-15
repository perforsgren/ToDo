using System.IO;
using System.Text.Json;
using ToDo.Models;

namespace ToDo.Services;

public static class StorageService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToDo", "data.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? CreateDefault();
            }
        }
        catch
        {
            // If deserialization fails, return default
        }

        return CreateDefault();
    }

    public static void Save(AppData data)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    private static AppData CreateDefault()
    {
        return new AppData
        {
            Tabs = [new TodoTab { Name = "My Tasks" }],
            SelectedTabIndex = 0
        };
    }
}