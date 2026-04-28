using UIAutomation.Core.Platforms.Windows;

namespace UIAutomation.Core.Tests.Windows;

public class KeyInputParserTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var inputs = KeyInputParser.Parse("");
        Assert.Empty(inputs);
    }

    [Fact]
    public void Parse_Null_ReturnsEmpty()
    {
        var inputs = KeyInputParser.Parse(null!);
        Assert.Empty(inputs);
    }

    [Fact]
    public void Parse_PlainText_CreatesUnicodeInputs()
    {
        var inputs = KeyInputParser.Parse("Hi");
        // Each char = 2 inputs (down + up)
        Assert.Equal(4, inputs.Length);

        // All should be keyboard inputs with unicode flag
        for (int i = 0; i < inputs.Length; i++)
        {
            Assert.Equal(NativeMethods.INPUT_KEYBOARD, inputs[i].type);
        }
    }

    [Fact]
    public void Parse_NamedKey_CreatesVirtualKeyInputs()
    {
        var inputs = KeyInputParser.Parse("{Enter}");
        // 1 key = 2 inputs (down + up)
        Assert.Equal(2, inputs.Length);
        Assert.Equal(NativeMethods.INPUT_KEYBOARD, inputs[0].type);
        Assert.Equal(0x0D, inputs[0].union.ki.wVk); // VK_RETURN
    }

    [Fact]
    public void Parse_ModifierCombo_CreatesCorrectSequence()
    {
        var inputs = KeyInputParser.Parse("{Ctrl+A}");
        // Modifier down + key down + key up + modifier up = 4
        Assert.Equal(4, inputs.Length);

        // First: Ctrl down
        Assert.Equal(0xA2, inputs[0].union.ki.wVk); // VK_LCONTROL
        Assert.Equal(0u, inputs[0].union.ki.dwFlags & NativeMethods.KEYEVENTF_KEYUP);

        // Second: A down
        Assert.Equal((ushort)'A', inputs[1].union.ki.wVk);

        // Third: A up
        Assert.Equal((ushort)'A', inputs[2].union.ki.wVk);
        Assert.NotEqual(0u, inputs[2].union.ki.dwFlags & NativeMethods.KEYEVENTF_KEYUP);

        // Fourth: Ctrl up
        Assert.Equal(0xA2, inputs[3].union.ki.wVk);
        Assert.NotEqual(0u, inputs[3].union.ki.dwFlags & NativeMethods.KEYEVENTF_KEYUP);
    }

    [Fact]
    public void Parse_DoubleModifierCombo_CreatesCorrectSequence()
    {
        var inputs = KeyInputParser.Parse("{Ctrl+Shift+A}");
        // Ctrl down + Shift down + A down + A up + Shift up + Ctrl up = 6
        Assert.Equal(6, inputs.Length);
    }

    [Fact]
    public void Parse_EscapedBraces_ProducesLiteralBraces()
    {
        var inputs = KeyInputParser.Parse("{{}}");
        // { and } = 4 inputs (2 chars × 2)
        Assert.Equal(4, inputs.Length);
    }

    [Fact]
    public void Parse_MixedTextAndKeys_WorksCorrectly()
    {
        var inputs = KeyInputParser.Parse("a{Enter}b");
        // 'a' (2) + Enter (2) + 'b' (2) = 6
        Assert.Equal(6, inputs.Length);
    }

    [Fact]
    public void Parse_UnmatchedOpenBrace_Throws()
    {
        Assert.Throws<ArgumentException>(() => KeyInputParser.Parse("{unclosed"));
    }

    [Fact]
    public void Parse_UnmatchedCloseBrace_Throws()
    {
        Assert.Throws<ArgumentException>(() => KeyInputParser.Parse("text}more"));
    }

    [Fact]
    public void Parse_UnknownKeyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => KeyInputParser.Parse("{UnknownKey}"));
    }

    [Fact]
    public void Parse_EmptyBraces_Throws()
    {
        Assert.Throws<ArgumentException>(() => KeyInputParser.Parse("{}"));
    }

    [Fact]
    public void Parse_ExtendedKeys_HaveExtendedFlag()
    {
        var inputs = KeyInputParser.Parse("{Delete}");
        Assert.Equal(2, inputs.Length);
        // Delete is an extended key
        Assert.NotEqual(0u, inputs[0].union.ki.dwFlags & NativeMethods.KEYEVENTF_EXTENDEDKEY);
    }

    [Fact]
    public void Parse_AllNamedKeys_DoNotThrow()
    {
        var namedKeys = new[]
        {
            "Enter", "Return", "Tab", "Escape", "Esc", "Backspace", "Back",
            "Delete", "Del", "Insert", "Ins", "Space",
            "Up", "Down", "Left", "Right",
            "Home", "End", "PageUp", "PageDown",
            "F1", "F2", "F3", "F4", "F5", "F6",
            "F7", "F8", "F9", "F10", "F11", "F12",
            "PrintScreen", "Pause", "CapsLock", "NumLock", "ScrollLock",
        };

        foreach (var key in namedKeys)
        {
            var inputs = KeyInputParser.Parse($"{{{key}}}");
            Assert.True(inputs.Length >= 2, $"Key '{key}' should produce at least 2 inputs");
        }
    }

    [Fact]
    public void Parse_AllModifiers_DoNotThrow()
    {
        var modifiers = new[] { "Ctrl", "Control", "Alt", "Shift", "Win" };

        foreach (var mod in modifiers)
        {
            var inputs = KeyInputParser.Parse($"{{{mod}+A}}");
            Assert.True(inputs.Length >= 4, $"Modifier '{mod}' combo should produce at least 4 inputs");
        }
    }

    [Fact]
    public void Parse_ModifierWithNamedKey_Works()
    {
        var inputs = KeyInputParser.Parse("{Alt+F4}");
        // Alt down + F4 down + F4 up + Alt up = 4
        Assert.Equal(4, inputs.Length);
    }
}
