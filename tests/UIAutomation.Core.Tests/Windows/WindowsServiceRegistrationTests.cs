using Microsoft.Extensions.DependencyInjection;
using UIAutomation.Core.Platforms.Windows;
using UIAutomation.Core.Services;

namespace UIAutomation.Core.Tests.Windows;

public class WindowsServiceRegistrationTests
{
    [Fact]
    public void AddUIAutomationServices_RegistersWindowsBackendsDirectly()
    {
        var services = new ServiceCollection();

        services.AddUIAutomationServices();

        using var provider = services.BuildServiceProvider();
        Assert.IsType<WindowsUIAutomationBackend>(provider.GetRequiredService<IUIAutomationService>());
        Assert.IsType<WindowsScreenCaptureBackend>(provider.GetRequiredService<IScreenCaptureService>());
    }

    [Fact]
    public void AddUIAutomationServices_RegistersAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddUIAutomationServices();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IUIAutomationService>();
        var second = provider.GetRequiredService<IUIAutomationService>();
        Assert.Same(first, second);
    }
}
