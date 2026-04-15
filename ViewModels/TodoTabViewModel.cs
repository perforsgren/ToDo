using System.Collections.ObjectModel;
using System.ComponentModel;
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
        _name  = model.Name;

        // Load sorted: active items first (original order), then done newest-first
        var sorted = model.Items
            .OrderBy(i => i.IsDone)
            .ThenByDescending(i => i.DoneAt ?? DateTime.MinValue);

        Items = new ObservableCollection<TodoItemViewModel>(
            sorted.Select(CreateVm));

        AddItemCommand        = new RelayCommand(_ => AddItem(), _ => !string.IsNullOrWhiteSpace(NewItemText));
        RemoveItemCommand     = new RelayCommand(p => RemoveItem(p as TodoItemViewModel));
        StartRenamingCommand  = new RelayCommand(_ => IsRenaming = true);
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

    public ICommand AddItemCommand        { get; }
    public ICommand RemoveItemCommand     { get; }
    public ICommand StartRenamingCommand  { get; }
    public ICommand FinishRenamingCommand { get; }

    private TodoItemViewModel CreateVm(TodoItem model)
    {
        var vm = new TodoItemViewModel(model);
        vm.PropertyChanged += ItemViewModel_PropertyChanged;
        return vm;
    }

    private void AddItem()
    {
        var model = new TodoItem { Text = NewItemText.Trim() };
        _model.Items.Add(model);

        // New active items insert just before the first done item
        var vm       = CreateVm(model);
        var insertAt = FirstDoneIndex();
        Items.Insert(insertAt, vm);

        NewItemText = string.Empty;
    }

    private void RemoveItem(TodoItemViewModel? item)
    {
        if (item is null) return;
        item.PropertyChanged -= ItemViewModel_PropertyChanged;
        _model.Items.RemoveAll(i => i.Id == item.Id);
        Items.Remove(item);
    }

    private void ItemViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Listen to IsDone — at this point DoneAt is already updated in the VM
        if (e.PropertyName != nameof(TodoItemViewModel.IsDone)) return;
        if (sender is not TodoItemViewModel vm) return;

        Items.Remove(vm);

        if (vm.IsDone)
        {
            // Insert at the top of the done section (newest first).
            // Done section starts at FirstDoneIndex(); since this item
            // is the freshest it goes right at that boundary.
            Items.Insert(FirstDoneIndex(), vm);
        }
        else
        {
            // Un-done: move back to the bottom of the active section
            Items.Insert(FirstDoneIndex(), vm);
        }
    }

    /// <summary>Returns the index of the first done item, or Count if none.</summary>
    private int FirstDoneIndex() =>
        Items.TakeWhile(i => !i.IsDone).Count();

    public TodoTab ToModel() => _model;
}