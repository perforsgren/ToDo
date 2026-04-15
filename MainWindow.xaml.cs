using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ToDo.Models;
using ToDo.Services;
using ToDo.ViewModels;

namespace ToDo;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    // ── Drag state ────────────────────────────────────────────────
    private TodoItemViewModel? _draggedItem;
    private int _draggedIndex = -1;
    private int _lastIndicatorIndex = -1;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        RestoreWindowBounds();
        Closing += (_, _) => SaveWithBounds();
    }

    // ── Window bounds ─────────────────────────────────────────────

    private void RestoreWindowBounds()
    {
        var bounds = StorageService.Load().WindowBounds;
        if (bounds is null) return;

        var width = Math.Max(MinWidth, bounds.Width);
        var height = Math.Max(MinHeight, bounds.Height);

        var centerX = (int)(bounds.Left + width / 2);
        var centerY = (int)(bounds.Top + height / 2);

        var wa = MonitorHelper.GetWorkAreaForPoint(centerX, centerY);
        var dpi = GetCurrentDpiScale();

        double waLeft = wa.Left / dpi;
        double waTop = wa.Top / dpi;
        double waWidth = wa.Width / dpi;
        double waHeight = wa.Height / dpi;

        width = Math.Min(width, waWidth);
        height = Math.Min(height, waHeight);

        Left = Math.Max(waLeft, Math.Min(bounds.Left, waLeft + waWidth - width));
        Top = Math.Max(waTop, Math.Min(bounds.Top, waTop + waHeight - height));
        Width = width;
        Height = height;
    }

    private double GetCurrentDpiScale()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is not null)
            return source.CompositionTarget.TransformToDevice.M11;
        return 1.0;
    }

    private void SaveWithBounds()
    {
        if (WindowState != WindowState.Normal) return;
        _viewModel.Save(new WindowBounds { Left = Left, Top = Top, Width = Width, Height = Height });
    }

    // ── Title bar / window chrome ─────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // ── Tabs ──────────────────────────────────────────────────────

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

    // ── New item ──────────────────────────────────────────────────

    private void NewItem_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel.SelectedTab is { } tab &&
            tab.AddItemCommand.CanExecute(null))
            tab.AddItemCommand.Execute(null);
    }

    // ── Inline edit ───────────────────────────────────────────────

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
        if (sender is TextBox tb) { tb.Focus(); tb.SelectAll(); }
    }

    private void ItemEdit_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TodoItemViewModel item)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                item.IsEditing = false;
                e.Handled = true;
            }
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
        if (sender is not FrameworkElement fe || fe.DataContext is not TodoItemViewModel item) return;

        Dispatcher.BeginInvoke(() =>
        {
            var focused = FocusManager.GetFocusedElement(this) as DependencyObject;
            if (focused is TextBox tb && tb.DataContext == item) return;
            item.IsEditing = false;
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    // ── Drag-and-drop reorder ─────────────────────────────────────

    private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not TodoItemViewModel item) return;
        if (item.IsDone) return;

        var tab = _viewModel.SelectedTab;
        if (tab is null) return;

        _draggedItem  = item;
        _draggedIndex = tab.Items.IndexOf(item);

        DragDrop.DoDragDrop(fe, new DataObject("TodoItem", item), DragDropEffects.Move);

        ClearDropIndicators();
        _draggedItem        = null;
        _draggedIndex       = -1;
        _lastIndicatorIndex = -1;
    }

    private void ItemsList_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        e.Effects = e.Data.GetDataPresent("TodoItem") && _draggedItem is not null
            ? DragDropEffects.Move
            : DragDropEffects.None;

        if (e.Effects == DragDropEffects.None) return;

        var targetIndex = GetDropIndex(e);
        if (targetIndex == _lastIndicatorIndex) return;

        ClearDropIndicators();
        ShowDropIndicator(targetIndex);
        _lastIndicatorIndex = targetIndex;
    }

    private void ItemsList_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        ClearDropIndicators();

        if (!e.Data.GetDataPresent("TodoItem") || _draggedItem is null) return;

        var tab = _viewModel.SelectedTab;
        if (tab is null) return;

        tab.MoveItem(_draggedIndex, GetDropIndex(e));
    }

    private void ItemsList_DragLeave(object sender, DragEventArgs e)
    {
        var pos = e.GetPosition(ItemsScrollViewer);
        if (pos.X >= 0 && pos.Y >= 0 &&
            pos.X <= ItemsScrollViewer.ActualWidth &&
            pos.Y <= ItemsScrollViewer.ActualHeight)
            return;

        ClearDropIndicators();
        _lastIndicatorIndex = -1;
    }

    /// <summary>
    /// Returns the insertion index (0..doneStart) based on cursor Y.
    /// Uses top-edge of each item container - if cursor is above an item's top edge,
    /// insert before it. This matches how the indicator is drawn and feels
    /// intuitive regardless of drag direction.
    /// A dead zone prevents flickering at boundaries.
    /// </summary>
    private int GetDropIndex(DragEventArgs e)
    {
        var tab = _viewModel.SelectedTab;
        if (tab is null) return 0;

        var panel = GetItemsPanel();
        if (panel is null) return 0;

        var doneStart = tab.Items.TakeWhile(i => !i.IsDone).Count();
        if (doneStart == 0) return 0;

        var cursorY   = e.GetPosition(panel).Y;
        const double deadZone = 6.0;

        // Collect top Y of each active item container
        var tops = new double[doneStart];
        for (var i = 0; i < doneStart; i++)
        {
            var c = ItemsList.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            tops[i] = c is not null
                ? c.TransformToAncestor(panel).Transform(new Point(0, 0)).Y
                : (i > 0 ? tops[i - 1] : 0);
        }

        // Find the first item whose top edge is below the cursor (with dead zone)
        // → insert before that item
        for (var i = 0; i < doneStart; i++)
        {
            if (cursorY < tops[i] + deadZone)
                return i;
        }

        // Cursor is below all active items → insert at end of active section
        return doneStart;
    }

    /// <summary>
    /// Shows the correct visual indicator for insertion at <paramref name="index"/>:
    /// - index 0..N-1 → top indicator on item[index]
    /// - index == N   → bottom indicator on item[N-1]  (insert at end)
    /// This guarantees the line is always drawn at exactly the right gap.
    /// </summary>
    private void ShowDropIndicator(int index)
    {
        var tab = _viewModel.SelectedTab;
        if (tab is null) return;

        var doneStart = tab.Items.TakeWhile(i => !i.IsDone).Count();

        if (index < doneStart)
        {
            // Draw line above item[index]
            var c = ItemsList.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
            if (FindIndicator(c, top: true) is { } ind)
                ind.Visibility = Visibility.Visible;
        }
        else if (index == doneStart && doneStart > 0)
        {
            // Draw line below the last active item
            var c = ItemsList.ItemContainerGenerator.ContainerFromIndex(doneStart - 1) as FrameworkElement;
            if (FindIndicator(c, top: false) is { } ind)
                ind.Visibility = Visibility.Visible;
        }
    }

    private void ClearDropIndicators()
    {
        for (var i = 0; i < ItemsList.Items.Count; i++)
        {
            var c = ItemsList.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            if (FindIndicator(c, top: true)  is { } t) t.Visibility = Visibility.Collapsed;
            if (FindIndicator(c, top: false) is { } b) b.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Finds DropIndicatorTop (top=true) or DropIndicatorBottom (top=false) in a container.</summary>
    private static Border? FindIndicator(FrameworkElement? container, bool top)
    {
        if (container is null) return null;
        var stack = FindChild<StackPanel>(container);
        if (stack is null) return null;
        // StackPanel children: [0] DropIndicatorTop, [1] card Border, [2] DropIndicatorBottom
        var childIndex = top ? 0 : 2;
        return childIndex < stack.Children.Count ? stack.Children[childIndex] as Border : null;
    }

    private Panel? GetItemsPanel()
    {
        if (ItemsList.ItemContainerGenerator.ContainerFromIndex(0) is not FrameworkElement first)
            return null;
        return VisualTreeHelper.GetParent(first) as Panel;
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            var result = FindChild<T>(child);
            if (result is not null) return result;
        }
        return null;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        SaveWithBounds();
    }
}