using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ToDo.Models;
using ToDo.Services;
using ToDo.ViewModels;

namespace ToDo;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        RestoreWindowBounds();

        Closing += (_, _) => SaveWithBounds();
    }

    private void RestoreWindowBounds()
    {
        var bounds = StorageService.Load().WindowBounds;
        if (bounds is null) return;

        var screenWidth = SystemParameters.VirtualScreenWidth;
        var screenHeight = SystemParameters.VirtualScreenHeight;

        Width = Math.Max(MinWidth, Math.Min(bounds.Width, screenWidth));
        Height = Math.Max(MinHeight, Math.Min(bounds.Height, screenHeight));
        Left = Math.Max(0, Math.Min(bounds.Left, screenWidth - Width));
        Top = Math.Max(0, Math.Min(bounds.Top, screenHeight - Height));
    }

    private void SaveWithBounds()
    {
        _viewModel.Save(new WindowBounds
        {
            Left = Left,
            Top = Top,
            Width = Width,
            Height = Height
        });
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Tab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TodoTabViewModel tab)
            _viewModel.SelectedTab = tab;
    }

    private void TabName_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement fe && fe.DataContext is TodoTabViewModel tab)
        {
            tab.IsRenaming = true;
            e.Handled = true;
        }
    }

    private void TabRename_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TodoTabViewModel tab)
            tab.IsRenaming = false;
    }

    private void TabRename_KeyDown(object sender, KeyEventArgs e)
    {
        if ((e.Key == Key.Enter || e.Key == Key.Escape) &&
            sender is FrameworkElement fe && fe.DataContext is TodoTabViewModel tab)
            tab.IsRenaming = false;
    }

    private void NewItem_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel.SelectedTab is { } tab)
        {
            if (tab.AddItemCommand.CanExecute(null))
                tab.AddItemCommand.Execute(null);
        }
    }

    // --- Inline item editing ---

    private void ItemText_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement fe && fe.DataContext is TodoItemViewModel item)
        {
            item.IsEditing = true;
            e.Handled = true;
        }
    }

    private void ItemEditBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    private void ItemEdit_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is FrameworkElement fe && fe.DataContext is TodoItemViewModel item)
        {
            item.IsEditing = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && sender is FrameworkElement fe2 && fe2.DataContext is TodoItemViewModel item2)
        {
            item2.IsEditing = false;
            e.Handled = true;
        }
    }

    private void DescriptionEdit_KeyDown(object sender, KeyEventArgs e)
    {
        // Escape closes edit mode; Enter adds newline (AcceptsReturn=True)
        if (e.Key == Key.Escape && sender is FrameworkElement fe && fe.DataContext is TodoItemViewModel item)
        {
            item.IsEditing = false;
            e.Handled = true;
        }
    }

    private void ItemEdit_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not TodoItemViewModel item)
            return;

        // Delay to check if focus moved to the sibling edit field (title ↔ description)
        Dispatcher.BeginInvoke(() =>
        {
            var focused = FocusManager.GetFocusedElement(this) as DependencyObject;
            if (focused is TextBox tb && tb.DataContext == item)
                return; // Focus moved within the same item's editors — stay in edit mode

            item.IsEditing = false;
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        SaveWithBounds();
    }
}