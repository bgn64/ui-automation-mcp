using Microsoft.Extensions.DependencyInjection;
using UIAutomation.Core.Platforms.MacOS;
using UIAutomation.Core.Services;

namespace UIAutomation.Core.Tests.MacOS;

public class MacServiceRegistrationTests
{
    [Fact]
    public void AddUIAutomationServices_RegistersMacBackendsDirectly()
    {
        var services = new ServiceCollection();

        services.AddUIAutomationServices();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<MacUIAutomationBackend>(provider.GetRequiredService<IUIAutomationService>());
        Assert.IsType<MacScreenCaptureBackend>(provider.GetRequiredService<IScreenCaptureService>());
    }
}
