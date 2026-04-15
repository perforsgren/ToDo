using System.Collections.ObjectModel;
using System.Windows.Input;
using ToDo.Models;
using ToDo.Services;

namespace ToDo.ViewModels;

public class MainViewModel : ViewModelBase
{
    private TodoTabViewModel? _selectedTab;
    private bool _isAlwaysOnTop = true;

    public MainViewModel()
    {
        var data = StorageService.Load();
        Tabs = new ObservableCollection<TodoTabViewModel>(
            data.Tabs.Select(t => new TodoTabViewModel(t)));

        if (Tabs.Count == 0)
            AddNewTab();

        SelectedTab = Tabs.Count > data.SelectedTabIndex && data.SelectedTabIndex >= 0
            ? Tabs[data.SelectedTabIndex]
            : Tabs.FirstOrDefault();

        AddTabCommand = new RelayCommand(_ => AddNewTab());
        RemoveTabCommand = new RelayCommand(p => RemoveTab(p as TodoTabViewModel), p => Tabs.Count > 1);
        RemoveItemCommand = new RelayCommand(p => SelectedTab?.RemoveItemCommand.Execute(p));
        SaveCommand = new RelayCommand(_ => Save());
        ToggleAlwaysOnTopCommand = new RelayCommand(_ => IsAlwaysOnTop = !IsAlwaysOnTop);
    }

    public ObservableCollection<TodoTabViewModel> Tabs { get; }

    public TodoTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set => SetProperty(ref _isAlwaysOnTop, value);
    }

    public ICommand AddTabCommand { get; }
    public ICommand RemoveTabCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ToggleAlwaysOnTopCommand { get; }

    private void AddNewTab()
    {
        var tab = new TodoTabViewModel(new TodoTab { Name = $"Tab {Tabs.Count + 1}" });
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    private void RemoveTab(TodoTabViewModel? tab)
    {
        if (tab is null || Tabs.Count <= 1) return;
        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
    }

    public void Save(WindowBounds? bounds = null)
    {
        var data = new AppData
        {
            Tabs = Tabs.Select(t => t.ToModel()).ToList(),
            SelectedTabIndex = SelectedTab is not null ? Tabs.IndexOf(SelectedTab) : 0,
            WindowBounds = bounds
        };
        StorageService.Save(data);
    }
}