using Avalonia.Controls;
using Avalonia.Input;

namespace DesktopMcp.App.Views;

public partial class TrayPopupWindow : Window
{
    public TrayPopupWindow()
    {
        InitializeComponent();
        NativeWindowRegion.ApplyRoundedCorners(this, 13);
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Avalonia.Controls.Button)
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
