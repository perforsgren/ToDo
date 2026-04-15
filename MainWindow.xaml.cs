using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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

        // Start with saved size, clamped to allowed minimums
        var width  = Math.Max(MinWidth,  bounds.Width);
        var height = Math.Max(MinHeight, bounds.Height);

        // Use the saved window's center point to find the correct monitor.
        // This survives partial off-screen positions gracefully and
        // falls back to the nearest monitor if that monitor is gone.
        var centerX = (int)(bounds.Left + width  / 2);
        var centerY = (int)(bounds.Top  + height / 2);

        var wa = MonitorHelper.GetWorkAreaForPoint(centerX, centerY);

        // Get the DPI scale of this window's source so we can convert
        // physical pixel coordinates to WPF device-independent units.
        // We read DPI after the HWND exists; before that fall back to
        // the system DPI reported by the primary screen.
        var dpi = GetCurrentDpiScale();

        double waLeft   = wa.Left   / dpi;
        double waTop    = wa.Top    / dpi;
        double waWidth  = wa.Width  / dpi;
        double waHeight = wa.Height / dpi;

        // Never let the window be larger than the work area
        width  = Math.Min(width,  waWidth);
        height = Math.Min(height, waHeight);

        // Clamp position so the entire window is visible on the monitor.
        // Works for monitors to the left/above the primary (negative coords).
        Left   = Math.Max(waLeft, Math.Min(bounds.Left, waLeft + waWidth  - width));
        Top    = Math.Max(waTop,  Math.Min(bounds.Top,  waTop  + waHeight - height));
        Width  = width;
        Height = height;
    }

    /// <summary>
    /// Returns the DPI scale (physical px / WPF DIP) for this window.
    /// Uses the HWND source when available, otherwise falls back to 1.0 (96 DPI).
    /// The fallback only triggers before the window is shown, at which point
    /// RestoreWindowBounds has no saved data to act on anyway.
    /// </summary>
    private double GetCurrentDpiScale()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is not null)
            return source.CompositionTarget.TransformToDevice.M11;

        // Safe fallback — assume 96 DPI (scale 1.0).
        // RestoreWindowBounds only runs when saved bounds exist, which means
        // the app has been run before and the window is already initialized.
        return 1.0;
    }

    private void SaveWithBounds()
    {
        // Never save minimized or maximized coordinates — they are not
        // the window's restored position and would cause wrong placement.
        if (WindowState != WindowState.Normal) return;

        _viewModel.Save(new WindowBounds
        {
            Left   = Left,
            Top    = Top,
            Width  = Width,
            Height = Height,
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

        Dispatcher.BeginInvoke(() =>
        {
            var focused = FocusManager.GetFocusedElement(this) as DependencyObject;
            if (focused is TextBox tb && tb.DataContext == item)
                return;

            item.IsEditing = false;
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        SaveWithBounds();
    }
}