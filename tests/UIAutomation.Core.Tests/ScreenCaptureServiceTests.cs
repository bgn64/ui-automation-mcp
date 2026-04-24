using UIAutomation.Core.Services;

namespace UIAutomation.Core.Tests;

/// <summary>
/// Integration tests for ScreenCaptureService.
/// These run on the live desktop, so they are marked as Integration tests.
/// </summary>
[Trait("Category", "Integration")]
public class ScreenCaptureServiceTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly ScreenCaptureService _service = new();

    [Fact]
    public void CaptureScreen_ReturnsNonEmptyBytes()
    {
        byte[] result = _service.CaptureScreen();
        Assert.NotEmpty(result);
    }

    [Fact]
    public void CaptureScreen_ReturnedBytesHaveValidPngHeader()
    {
        byte[] result = _service.CaptureScreen();
        Assert.True(result.Length >= PngSignature.Length, "Result is too short to contain a PNG header.");
        Assert.Equal(PngSignature, result[..PngSignature.Length]);
    }
}
