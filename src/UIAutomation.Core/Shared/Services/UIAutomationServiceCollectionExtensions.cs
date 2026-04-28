using Microsoft.Extensions.DependencyInjection;
using UIAutomation.Core.Backends;
#if WINDOWS
using UIAutomation.Core.Platforms.Windows;
#endif

namespace UIAutomation.Core.Services;

public static class UIAutomationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared UI automation front-end services and the current platform backend.
    /// </summary>
    public static IServiceCollection AddUIAutomationServices(this IServiceCollection services)
    {
        services.AddSingleton<IUIAutomationService, UIAutomationService>();
        services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();

#if WINDOWS
        services.AddSingleton<WindowsElementCache>();
        services.AddSingleton<IUIAutomationBackend, WindowsUIAutomationBackend>();
        services.AddSingleton<IScreenCaptureBackend, WindowsScreenCaptureBackend>();
#else
        throw new PlatformNotSupportedException("ui-automation-mcp currently supports Windows.");
#endif

        return services;
    }
}
