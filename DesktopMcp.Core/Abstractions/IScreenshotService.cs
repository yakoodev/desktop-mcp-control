using DesktopMcp.Core.Models;

namespace DesktopMcp.Core.Abstractions;

public interface IScreenshotService
{
    CaptureResult Capture(CaptureRequest request);
}
