namespace DesktopMcp.App.Services;

public sealed class NullTrayIntegration : ITrayIntegration
{
    public bool IsSupported => false;

    public void Initialize(
        Action onOpenControlPanel,
        Action onOpenTools,
        Action onOpenSettings,
        Func<Task> onExitAsync)
    {
    }

    public void Dispose()
    {
    }
}
