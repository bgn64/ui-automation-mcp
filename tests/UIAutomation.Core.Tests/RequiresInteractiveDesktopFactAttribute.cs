namespace UIAutomation.Core.Tests;

/// <summary>
/// Marks an xUnit fact that requires a live, interactive Windows desktop —
/// e.g., a populated UI Automation tree, real top-level windows, a desktop
/// root that reports realistic pattern support, a primary monitor, etc.
///
/// Tests using this attribute are automatically skipped when the conventional
/// <c>CI</c> environment variable is set (GitHub Actions, Azure DevOps,
/// GitLab CI, and most other providers set it to <c>true</c>). On developer
/// machines the variable is unset, so the tests run normally.
///
/// Use this instead of <c>[Fact]</c> for any test that exercises the live
/// UI Automation API or screen-capture stack against the host's real desktop.
/// </summary>
public sealed class RequiresInteractiveDesktopFactAttribute : FactAttribute
{
    public RequiresInteractiveDesktopFactAttribute()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
        {
            Skip = "Requires an interactive Windows desktop; skipped in CI.";
        }
    }
}
