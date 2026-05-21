using Avalonia.Controls;
using Avalonia.Input;

namespace DesktopMcp.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        NativeWindowRegion.ApplyRoundedCorners(this, 16);
    }

    private void DragSurface_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.Handled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CloseButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        Hide();
    }
}
