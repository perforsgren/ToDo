using System.Windows.Input;
using ToDo.Models;

namespace ToDo.ViewModels;

public class TodoItemViewModel : ViewModelBase
{
    private readonly TodoItem _model;
    private bool _isDone;
    private string _text;
    private string _description;
    private bool _isEditing;
    private bool _isExpanded;
    private DateTime? _doneAt;

    public TodoItemViewModel(TodoItem model)
    {
        _model       = model;
        _isDone      = model.IsDone;
        _text        = model.Text;
        _description = model.Description;
        _doneAt      = model.DoneAt;

        StartEditCommand    = new RelayCommand(_ => IsEditing = true);
        FinishEditCommand   = new RelayCommand(_ => IsEditing = false);
        ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
    }

    public string Id => _model.Id;

    public bool IsDone
    {
        get => _isDone;
        set
        {
            if (!SetProperty(ref _isDone, value)) return;
            _model.IsDone = value;

            // Update DoneAt first — then notify IsDone so listeners
            // see the final DoneAt value when they react to IsDone changing.
            _doneAt       = value ? DateTime.Now : null;
            _model.DoneAt = _doneAt;

            OnPropertyChanged(nameof(DoneAt));
            OnPropertyChanged(nameof(DoneAtLabel));
            OnPropertyChanged(nameof(HasDoneAt));
            // Fire IsDone last so TabViewModel sees correct DoneAt
            OnPropertyChanged(nameof(IsDone));
        }
    }

    public DateTime? DoneAt  => _doneAt;
    public string DoneAtLabel => _doneAt?.ToString("yyyy-MM-dd") ?? string.Empty;
    public bool HasDoneAt     => _doneAt.HasValue && _isDone;

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
                _model.Text = value;
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                _model.Description = value;
                OnPropertyChanged(nameof(HasDescription));
            }
        }
    }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public ICommand StartEditCommand    { get; }
    public ICommand FinishEditCommand   { get; }
    public ICommand ToggleExpandCommand { get; }

    public TodoItem ToModel() => _model;
}