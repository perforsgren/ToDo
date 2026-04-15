using System.Collections.ObjectModel;
using System.Windows.Input;
using ToDo.Models;

namespace ToDo.ViewModels;

public class TodoTabViewModel : ViewModelBase
{
    private readonly TodoTab _model;
    private string _name;
    private bool _isRenaming;
    private string _newItemText = string.Empty;

    public TodoTabViewModel(TodoTab model)
    {
        _model = model;
        _name = model.Name;
        Items = new ObservableCollection<TodoItemViewModel>(
            model.Items.Select(i => new TodoItemViewModel(i)));

        AddItemCommand = new RelayCommand(_ => AddItem(), _ => !string.IsNullOrWhiteSpace(NewItemText));
        RemoveItemCommand = new RelayCommand(p => RemoveItem(p as TodoItemViewModel));
        StartRenamingCommand = new RelayCommand(_ => IsRenaming = true);
        FinishRenamingCommand = new RelayCommand(_ => IsRenaming = false);
    }

    public string Id => _model.Id;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                _model.Name = value;
        }
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set => SetProperty(ref _isRenaming, value);
    }

    public string NewItemText
    {
        get => _newItemText;
        set => SetProperty(ref _newItemText, value);
    }

    public ObservableCollection<TodoItemViewModel> Items { get; }

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand StartRenamingCommand { get; }
    public ICommand FinishRenamingCommand { get; }

    private void AddItem()
    {
        var item = new TodoItem { Text = NewItemText.Trim() };
        _model.Items.Add(item);
        Items.Add(new TodoItemViewModel(item));
        NewItemText = string.Empty;
    }

    private void RemoveItem(TodoItemViewModel? item)
    {
        if (item is null) return;
        _model.Items.RemoveAll(i => i.Id == item.Id);
        Items.Remove(item);
    }

    public TodoTab ToModel() => _model;
}