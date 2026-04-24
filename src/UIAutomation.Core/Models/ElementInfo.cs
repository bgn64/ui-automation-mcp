using System.Text.Json.Serialization;

namespace UIAutomation.Core.Models;

/// <summary>
/// Serializable representation of a UI Automation element.
/// </summary>
public sealed class ElementInfo
{
    public required string ElementId { get; init; }
    public string Name { get; init; } = "";
    public string AutomationId { get; init; } = "";
    public string ControlType { get; init; } = "";
    public string ClassName { get; init; } = "";
    public string LocalizedControlType { get; init; } = "";
    public BoundsInfo? BoundingRectangle { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsOffscreen { get; init; }
    public int ProcessId { get; init; }
    public string[] SupportedPatterns { get; init; } = [];
    public bool HasKeyboardFocus { get; init; }
    public bool IsKeyboardFocusable { get; init; }
    public string? HelpText { get; init; }
    public string? AcceleratorKey { get; init; }
    public string? AccessKey { get; init; }
    public int? NativeWindowHandle { get; init; }
    public string? FrameworkId { get; init; }
    public List<ElementInfo>? Children { get; set; }

    public override string ToString() =>
        $"[{ControlType}] Name=\"{Name}\" AutomationId=\"{AutomationId}\" Id={ElementId}";
}

public sealed class BoundsInfo
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }

    public override string ToString() => $"({X},{Y},{Width},{Height})";
}
