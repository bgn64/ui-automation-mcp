#if MACOS
using UIAutomation.Core.Platforms.MacOS;

namespace UIAutomation.Core.Tests;

public class MacKeyInputHelperTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var inputs = MacKeyInputHelper.Parse("");

        Assert.Empty(inputs);
    }

    [Fact]
    public void Parse_PlainText_CreatesUnicodeInputs()
    {
        var inputs = MacKeyInputHelper.Parse("Hi");

        Assert.Equal(4, inputs.Length);
        Assert.All(inputs, input => Assert.Equal(MacKeyInputKind.UnicodeCharacter, input.Kind));
    }

    [Fact]
    public void Parse_NamedKey_CreatesVirtualKeyInputs()
    {
        var inputs = MacKeyInputHelper.Parse("{Enter}");

        Assert.Equal(2, inputs.Length);
        Assert.Equal(MacKeyInputKind.VirtualKey, inputs[0].Kind);
        Assert.Equal((ushort)36, inputs[0].KeyCode);
        Assert.True(inputs[0].KeyDown);
        Assert.False(inputs[1].KeyDown);
    }

    [Fact]
    public void Parse_ModifierCombo_CreatesCorrectSequence()
    {
        var inputs = MacKeyInputHelper.Parse("{Cmd+A}");

        Assert.Equal(4, inputs.Length);
        Assert.Equal((ushort)55, inputs[0].KeyCode);
        Assert.True(inputs[0].KeyDown);
        Assert.Equal((ushort)0, inputs[1].KeyCode);
        Assert.False(inputs[2].KeyDown);
        Assert.False(inputs[3].KeyDown);
    }

    [Fact]
    public void Parse_EscapedBraces_ProducesLiteralBraces()
    {
        var inputs = MacKeyInputHelper.Parse("{{}}");

        Assert.Equal(4, inputs.Length);
        Assert.Equal('{', inputs[0].Character);
        Assert.Equal('}', inputs[2].Character);
    }

    [Fact]
    public void Parse_UnknownKeyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => MacKeyInputHelper.Parse("{UnknownKey}"));
    }
}
#endif
