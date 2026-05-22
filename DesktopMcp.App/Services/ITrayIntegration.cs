namespace DesktopMcp.App.Services;

public interface ITrayIntegration : IDisposable
{
    bool IsSupported { get; }

    void Initialize(
        Action onOpenControlPanel,
        Action onOpenTools,
        Action onOpenSettings,
        Func<Task> onExitAsync);
}
