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
        var model    = new TodoItem { Text = NewItemText.Trim() };
        _model.Items.Add(model);
        var vm       = CreateVm(model);
        Items.Insert(FirstDoneIndex(), vm);
        NewItemText = string.Empty;
    }

    private void RemoveItem(TodoItemViewModel? item)
    {
        if (item is null) return;
        item.PropertyChanged -= ItemViewModel_PropertyChanged;
        _model.Items.RemoveAll(i => i.Id == item.Id);
        Items.Remove(item);
    }

    /// <summary>
    /// Moves a not-done item from <paramref name="fromIndex"/> to
    /// <paramref name="toIndex"/> within the active section and keeps
    /// the underlying model list in sync by ID rather than positional index.
    /// </summary>
    public void MoveItem(int fromIndex, int toIndex)
    {
        var doneStart = FirstDoneIndex();

        // Both indices must be within the active section
        if (fromIndex < 0 || fromIndex >= doneStart) return;
        if (toIndex   < 0 || toIndex   >  doneStart) return;
        if (fromIndex == toIndex) return;

        // ObservableCollection.Move requires a valid index (0..Count-1).
        // When dropping after the last active item toIndex == doneStart,
        // so clamp to doneStart-1 when the item is moving forward.
        var vmTo = toIndex < doneStart ? toIndex : doneStart - 1;
        Items.Move(fromIndex, vmTo);

        // Sync model list: find by ID so positional mismatch never matters.
        var draggedModel = Items[vmTo].ToModel();
        var modelFrom    = _model.Items.FindIndex(m => m.Id == draggedModel.Id);

        // Target model position: ID of the VM that is now at vmTo+1 (the item
        // that should come after), or append at end of active section.
        int modelTo;
        if (vmTo + 1 < doneStart)
        {
            var afterId = Items[vmTo + 1].Id;
            modelTo = _model.Items.FindIndex(m => m.Id == afterId);
            if (modelTo < 0) modelTo = _model.Items.Count;
        }
        else
        {
            // Moved to last active slot — find position just before first done model
            var firstDoneId = Items.FirstOrDefault(i => i.IsDone)?.Id;
            modelTo = firstDoneId is not null
                ? _model.Items.FindIndex(m => m.Id == firstDoneId)
                : _model.Items.Count;
            if (modelTo < 0) modelTo = _model.Items.Count;
        }

        if (modelFrom < 0) return;

        _model.Items.RemoveAt(modelFrom);

        // Adjust target if we removed an element before it
        if (modelFrom < modelTo) modelTo--;
        modelTo = Math.Clamp(modelTo, 0, _model.Items.Count);

        _model.Items.Insert(modelTo, draggedModel);
    }

    private void ItemViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TodoItemViewModel.IsDone)) return;
        if (sender is not TodoItemViewModel vm) return;

        Items.Remove(vm);

        Items.Insert(FirstDoneIndex(), vm);
    }

    private int FirstDoneIndex() =>
        Items.TakeWhile(i => !i.IsDone).Count();

    public TodoTab ToModel() => _model;
}