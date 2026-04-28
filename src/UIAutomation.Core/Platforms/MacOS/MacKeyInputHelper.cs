#if MACOS
namespace UIAutomation.Core.Platforms.MacOS;

internal enum MacKeyInputKind
{
    VirtualKey,
    UnicodeCharacter,
}

internal readonly record struct MacKeyInput(MacKeyInputKind Kind, ushort KeyCode, char Character, bool KeyDown);

/// <summary>
/// Parses the MCP key syntax into macOS keyboard events and posts them with CGEvent.
/// </summary>
internal static class MacKeyInputHelper
{
    private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = 36,
        ["Return"] = 36,
        ["Tab"] = 48,
        ["Escape"] = 53,
        ["Esc"] = 53,
        ["Backspace"] = 51,
        ["Back"] = 51,
        ["Delete"] = 117,
        ["Del"] = 117,
        ["Insert"] = 114,
        ["Ins"] = 114,
        ["Space"] = 49,
        ["Up"] = 126,
        ["Down"] = 125,
        ["Left"] = 123,
        ["Right"] = 124,
        ["Home"] = 115,
        ["End"] = 119,
        ["PageUp"] = 116,
        ["PageDown"] = 121,
        ["F1"] = 122, ["F2"] = 120, ["F3"] = 99, ["F4"] = 118,
        ["F5"] = 96, ["F6"] = 97, ["F7"] = 98, ["F8"] = 100,
        ["F9"] = 101, ["F10"] = 109, ["F11"] = 103, ["F12"] = 111,
    };

    private static readonly Dictionary<string, ushort> Modifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ctrl"] = 59,
        ["Control"] = 59,
        ["Alt"] = 58,
        ["Option"] = 58,
        ["Shift"] = 56,
        ["Cmd"] = 55,
        ["Command"] = 55,
        ["Win"] = 55,
    };

    private static readonly Dictionary<char, ushort> CharacterKeys = new()
    {
        ['A'] = 0, ['S'] = 1, ['D'] = 2, ['F'] = 3, ['H'] = 4, ['G'] = 5,
        ['Z'] = 6, ['X'] = 7, ['C'] = 8, ['V'] = 9, ['B'] = 11, ['Q'] = 12,
        ['W'] = 13, ['E'] = 14, ['R'] = 15, ['Y'] = 16, ['T'] = 17,
        ['1'] = 18, ['2'] = 19, ['3'] = 20, ['4'] = 21, ['6'] = 22,
        ['5'] = 23, ['='] = 24, ['9'] = 25, ['7'] = 26, ['-'] = 27,
        ['8'] = 28, ['0'] = 29, [']'] = 30, ['O'] = 31, ['U'] = 32,
        ['['] = 33, ['I'] = 34, ['P'] = 35, ['L'] = 37, ['J'] = 38,
        ['\''] = 39, ['K'] = 40, [';'] = 41, ['\\'] = 42, [','] = 43,
        ['/'] = 44, ['N'] = 45, ['M'] = 46, ['.'] = 47, ['`'] = 50,
    };

    public static MacKeyInput[] Parse(string keys)
    {
        if (string.IsNullOrEmpty(keys))
        {
            return [];
        }

        var inputs = new List<MacKeyInput>();
        int i = 0;

        while (i < keys.Length)
        {
            if (keys[i] == '{')
            {
                if (i + 1 < keys.Length && keys[i + 1] == '{')
                {
                    AddUnicodeChar(inputs, '{');
                    i += 2;
                    continue;
                }

                int closeIndex = keys.IndexOf('}', i + 1);
                if (closeIndex < 0)
                {
                    throw new ArgumentException($"Unmatched '{{' at position {i} in keys string.");
                }

                ParseToken(inputs, keys[(i + 1)..closeIndex]);
                i = closeIndex + 1;
            }
            else if (keys[i] == '}')
            {
                if (i + 1 < keys.Length && keys[i + 1] == '}')
                {
                    AddUnicodeChar(inputs, '}');
                    i += 2;
                    continue;
                }

                throw new ArgumentException($"Unmatched '}}' at position {i} in keys string.");
            }
            else
            {
                AddUnicodeChar(inputs, keys[i]);
                i++;
            }
        }

        return inputs.ToArray();
    }

    public static void Post(string keys)
    {
        foreach (var input in Parse(keys))
        {
            Post(input);
        }
    }

    private static void ParseToken(List<MacKeyInput> inputs, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Empty key name in braces.");
        }

        var parts = token.Split('+');
        if (parts.Length == 1)
        {
            if (!NamedKeys.TryGetValue(token, out var keyCode))
            {
                throw new ArgumentException($"Unknown key name '{token}'.");
            }

            AddVirtualKey(inputs, keyCode);
            return;
        }

        var modifierCodes = new List<ushort>();
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var modifierName = parts[i].Trim();
            if (!Modifiers.TryGetValue(modifierName, out var modifierCode))
            {
                throw new ArgumentException($"Unknown modifier '{modifierName}' in key combo '{token}'.");
            }

            modifierCodes.Add(modifierCode);
        }

        var keyName = parts[^1].Trim();
        ushort targetKeyCode;
        if (NamedKeys.TryGetValue(keyName, out var namedKeyCode))
        {
            targetKeyCode = namedKeyCode;
        }
        else if (keyName.Length == 1 && TryGetCharacterKeyCode(keyName[0], out var characterKeyCode))
        {
            targetKeyCode = characterKeyCode;
        }
        else
        {
            throw new ArgumentException($"Unknown key '{keyName}' in combo '{token}'.");
        }

        foreach (var modifierCode in modifierCodes)
        {
            AddKeyDown(inputs, modifierCode);
        }

        AddVirtualKey(inputs, targetKeyCode);

        for (int i = modifierCodes.Count - 1; i >= 0; i--)
        {
            AddKeyUp(inputs, modifierCodes[i]);
        }
    }

    private static bool TryGetCharacterKeyCode(char character, out ushort keyCode) =>
        CharacterKeys.TryGetValue(char.ToUpperInvariant(character), out keyCode);

    private static void AddUnicodeChar(List<MacKeyInput> inputs, char character)
    {
        inputs.Add(new MacKeyInput(MacKeyInputKind.UnicodeCharacter, 0, character, true));
        inputs.Add(new MacKeyInput(MacKeyInputKind.UnicodeCharacter, 0, character, false));
    }

    private static void AddVirtualKey(List<MacKeyInput> inputs, ushort keyCode)
    {
        AddKeyDown(inputs, keyCode);
        AddKeyUp(inputs, keyCode);
    }

    private static void AddKeyDown(List<MacKeyInput> inputs, ushort keyCode) =>
        inputs.Add(new MacKeyInput(MacKeyInputKind.VirtualKey, keyCode, '\0', true));

    private static void AddKeyUp(List<MacKeyInput> inputs, ushort keyCode) =>
        inputs.Add(new MacKeyInput(MacKeyInputKind.VirtualKey, keyCode, '\0', false));

    private static void Post(MacKeyInput input)
    {
        var cgEvent = MacNativeMethods.CGEventCreateKeyboardEvent(
            IntPtr.Zero,
            input.KeyCode,
            input.KeyDown);

        if (cgEvent == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to create macOS keyboard event.");
        }

        try
        {
            if (input.Kind == MacKeyInputKind.UnicodeCharacter && input.KeyDown)
            {
                MacNativeMethods.CGEventKeyboardSetUnicodeString(cgEvent, 1, [input.Character]);
            }

            MacNativeMethods.CGEventPost(MacNativeMethods.KCGHIDEventTap, cgEvent);
        }
        finally
        {
            MacNativeMethods.CFRelease(cgEvent);
        }
    }
}
#endif
