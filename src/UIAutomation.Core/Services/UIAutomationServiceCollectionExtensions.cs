using Microsoft.Extensions.DependencyInjection;
#if MACOS
using UIAutomation.Core.Platforms.MacOS;
#elif WINDOWS
using UIAutomation.Core.Platforms.Windows;
#endif

namespace UIAutomation.Core.Services;

public static class UIAutomationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the platform-specific UI automation and screen capture services.
    /// </summary>
    public static IServiceCollection AddUIAutomationServices(this IServiceCollection services)
    {
#if WINDOWS
        services.AddSingleton<IUIAutomationService, WindowsUIAutomationBackend>();
        services.AddSingleton<IScreenCaptureService, WindowsScreenCaptureBackend>();
#elif MACOS
        services.AddSingleton<IUIAutomationService, MacUIAutomationBackend>();
        services.AddSingleton<IScreenCaptureService, MacScreenCaptureBackend>();
#else
        throw new PlatformNotSupportedException("ui-automation-mcp currently supports Windows and macOS.");
#endif

        return services;
    }
}
