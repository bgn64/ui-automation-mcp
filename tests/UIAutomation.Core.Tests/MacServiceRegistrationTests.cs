#if MACOS
using Microsoft.Extensions.DependencyInjection;
using UIAutomation.Core.Backends;
using UIAutomation.Core.Platforms.MacOS;
using UIAutomation.Core.Services;

namespace UIAutomation.Core.Tests;

public class MacServiceRegistrationTests
{
    [Fact]
    public void AddUIAutomationServices_RegistersSharedServicesAndMacBackends()
    {
        var services = new ServiceCollection();

        services.AddUIAutomationServices();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<UIAutomationService>(provider.GetRequiredService<IUIAutomationService>());
        Assert.IsType<ScreenCaptureService>(provider.GetRequiredService<IScreenCaptureService>());
        Assert.IsType<MacUIAutomationBackend>(provider.GetRequiredService<IUIAutomationBackend>());
        Assert.IsType<MacScreenCaptureBackend>(provider.GetRequiredService<IScreenCaptureBackend>());
    }
}
#endif
