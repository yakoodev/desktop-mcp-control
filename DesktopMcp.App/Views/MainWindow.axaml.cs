using Avalonia.Controls;
using Avalonia.Input;
using DesktopMcp.App.ViewModels;

namespace DesktopMcp.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        NativeWindowRegion.ApplyRoundedCorners(this, 18);
    }

    private void DragSurface_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);
        if (position.Y <= 38 && position.X >= Bounds.Width - 45)
        {
            CloseButton_OnPointerPressed(sender, e);
            return;
        }

        if (position.Y <= 38 && position.X >= Bounds.Width - 75)
        {
            MinimizeButton_OnPointerPressed(sender, e);
            return;
        }

        if (!e.Handled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.HideToTrayCommand.Execute(null);
        }
        else
        {
            Hide();
        }
    }
}
