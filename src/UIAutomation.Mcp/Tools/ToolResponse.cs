using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace UIAutomation.Mcp.Tools;

/// <summary>
/// Shared helper for producing consistent JSON tool responses.
/// </summary>
internal static class ToolResponse
{
    /// <summary>Serializes a successful result.</summary>
    public static string Success(object data) =>
        JsonSerializer.Serialize(data, SerializerOptions.Default);

    /// <summary>Returns a clean error message for the caller (validation or expected errors).</summary>
    public static string Error(string message) =>
        JsonSerializer.Serialize(new { error = message }, SerializerOptions.Default);

    /// <summary>
    /// Handles an unexpected exception: logs it at Error level and returns a clean error
    /// response to the caller (no stack trace).
    /// </summary>
    public static string UnexpectedError(ILogger logger, Exception ex, string toolName)
    {
        logger.LogError(ex, "Unexpected error in tool '{ToolName}'", toolName);
        return JsonSerializer.Serialize(new { error = $"{ex.GetType().Name}: {ex.Message}" }, SerializerOptions.Default);
    }

    /// <summary>
    /// Returns true if the exception represents a validation/expected error
    /// (bad input from the caller) rather than an unexpected failure.
    /// </summary>
    public static bool IsValidationError(Exception ex) =>
        ex is KeyNotFoundException              // bad elementId
        || ex is InvalidOperationException      // unsupported pattern
        || ex is ArgumentException              // bad parameter value
        || ex is System.Windows.Automation.ElementNotAvailableException;  // stale element
}
