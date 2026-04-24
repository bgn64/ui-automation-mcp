namespace UIAutomation.Core.Services;

/// <summary>
/// Parses a key input string into an array of Win32 INPUT structures for SendInput.
/// 
/// Format:
///   - Plain characters are sent as Unicode keystrokes.
///   - Special keys use brace syntax: {Enter}, {Tab}, {Escape}, {Up}, {Down}, etc.
///   - Modifier combos: {Ctrl+C}, {Alt+F4}, {Shift+Tab}, {Ctrl+Shift+A}
///   - Literal braces: {{ for '{' and }} for '}'
/// </summary>
internal static class KeyInputParser
{
    // Virtual key codes for named keys
    private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = 0x0D,
        ["Return"] = 0x0D,
        ["Tab"] = 0x09,
        ["Escape"] = 0x1B,
        ["Esc"] = 0x1B,
        ["Backspace"] = 0x08,
        ["Back"] = 0x08,
        ["Delete"] = 0x2E,
        ["Del"] = 0x2E,
        ["Insert"] = 0x2D,
        ["Ins"] = 0x2D,
        ["Space"] = 0x20,
        ["Up"] = 0x26,
        ["Down"] = 0x28,
        ["Left"] = 0x25,
        ["Right"] = 0x27,
        ["Home"] = 0x24,
        ["End"] = 0x23,
        ["PageUp"] = 0x21,
        ["PageDown"] = 0x22,
        ["F1"] = 0x70, ["F2"] = 0x71, ["F3"] = 0x72, ["F4"] = 0x73,
        ["F5"] = 0x74, ["F6"] = 0x75, ["F7"] = 0x76, ["F8"] = 0x77,
        ["F9"] = 0x78, ["F10"] = 0x79, ["F11"] = 0x7A, ["F12"] = 0x7B,
        ["PrintScreen"] = 0x2C,
        ["Pause"] = 0x13,
        ["CapsLock"] = 0x14,
        ["NumLock"] = 0x90,
        ["ScrollLock"] = 0x91,
    };

    // Modifier name → virtual key code
    private static readonly Dictionary<string, ushort> Modifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ctrl"] = 0xA2,    // VK_LCONTROL
        ["Control"] = 0xA2,
        ["Alt"] = 0xA4,     // VK_LMENU
        ["Shift"] = 0xA0,   // VK_LSHIFT
        ["Win"] = 0x5B,     // VK_LWIN
    };

    // Extended keys that need KEYEVENTF_EXTENDEDKEY
    private static readonly HashSet<ushort> ExtendedKeys =
    [
        0x21, 0x22, 0x23, 0x24, // PageUp, PageDown, End, Home
        0x25, 0x26, 0x27, 0x28, // Left, Up, Right, Down
        0x2D, 0x2E,             // Insert, Delete
        0x5B, 0x5C,             // LWin, RWin
        0x2C,                   // PrintScreen
    ];

    public static NativeMethods.INPUT[] Parse(string keys)
    {
        if (string.IsNullOrEmpty(keys))
            return [];

        var inputs = new List<NativeMethods.INPUT>();
        int i = 0;

        while (i < keys.Length)
        {
            if (keys[i] == '{')
            {
                // Check for escaped braces {{ and }}
                if (i + 1 < keys.Length && keys[i + 1] == '{')
                {
                    AddUnicodeChar(inputs, '{');
                    i += 2;
                    continue;
                }

                // Find matching close brace
                int closeIndex = keys.IndexOf('}', i + 1);
                if (closeIndex < 0)
                    throw new ArgumentException($"Unmatched '{{' at position {i} in keys string.");

                string token = keys[(i + 1)..closeIndex];
                ParseToken(inputs, token);
                i = closeIndex + 1;
            }
            else if (keys[i] == '}')
            {
                // Check for escaped }}
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

    private static void ParseToken(List<NativeMethods.INPUT> inputs, string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Empty key name in braces.");

        // Check for modifier combo: "Ctrl+C", "Alt+F4", "Ctrl+Shift+A"
        var parts = token.Split('+');

        if (parts.Length == 1)
        {
            // Simple named key
            if (!NamedKeys.TryGetValue(token, out var vk))
                throw new ArgumentException($"Unknown key name '{token}'.");

            AddVirtualKey(inputs, vk);
        }
        else
        {
            // Modifier combo — last part is the key, preceding parts are modifiers
            var modifierVks = new List<ushort>();
            for (int m = 0; m < parts.Length - 1; m++)
            {
                var modName = parts[m].Trim();
                if (!Modifiers.TryGetValue(modName, out var modVk))
                    throw new ArgumentException($"Unknown modifier '{modName}' in key combo '{token}'.");
                modifierVks.Add(modVk);
            }

            var keyName = parts[^1].Trim();
            ushort keyVk;

            if (NamedKeys.TryGetValue(keyName, out var namedVk))
            {
                keyVk = namedVk;
            }
            else if (keyName.Length == 1 && char.IsLetterOrDigit(keyName[0]))
            {
                // Single character key — use VkKeyScan equivalent (uppercase letter = VK code)
                keyVk = (ushort)char.ToUpper(keyName[0]);
            }
            else
            {
                throw new ArgumentException($"Unknown key '{keyName}' in combo '{token}'.");
            }

            // Press modifiers
            foreach (var mod in modifierVks)
                AddKeyDown(inputs, mod);

            // Press and release the key
            AddVirtualKey(inputs, keyVk);

            // Release modifiers in reverse order
            for (int m = modifierVks.Count - 1; m >= 0; m--)
                AddKeyUp(inputs, modifierVks[m]);
        }
    }

    private static void AddUnicodeChar(List<NativeMethods.INPUT> inputs, char c)
    {
        // Key down
        inputs.Add(new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            union = { ki = new NativeMethods.KEYBDINPUT
            {
                wVk = 0,
                wScan = c,
                dwFlags = NativeMethods.KEYEVENTF_UNICODE,
            }}
        });

        // Key up
        inputs.Add(new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            union = { ki = new NativeMethods.KEYBDINPUT
            {
                wVk = 0,
                wScan = c,
                dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP,
            }}
        });
    }

    private static void AddVirtualKey(List<NativeMethods.INPUT> inputs, ushort vk)
    {
        AddKeyDown(inputs, vk);
        AddKeyUp(inputs, vk);
    }

    private static void AddKeyDown(List<NativeMethods.INPUT> inputs, ushort vk)
    {
        uint flags = 0;
        if (ExtendedKeys.Contains(vk))
            flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;

        inputs.Add(new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            union = { ki = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = flags,
            }}
        });
    }

    private static void AddKeyUp(List<NativeMethods.INPUT> inputs, ushort vk)
    {
        uint flags = NativeMethods.KEYEVENTF_KEYUP;
        if (ExtendedKeys.Contains(vk))
            flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;

        inputs.Add(new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            union = { ki = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = flags,
            }}
        });
    }
}
