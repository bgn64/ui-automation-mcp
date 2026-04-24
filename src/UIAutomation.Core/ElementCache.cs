using System.Collections.Concurrent;
using System.Windows.Automation;

namespace UIAutomation.Core;

/// <summary>
/// Thread-safe cache that assigns short string IDs to AutomationElement instances.
/// Enables cross-call element referencing in the MCP tools.
/// </summary>
public sealed class ElementCache
{
    private readonly ConcurrentDictionary<string, AutomationElement> _idToElement = new();
    private readonly ConcurrentDictionary<string, string> _runtimeIdToId = new();
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private int _nextId;
    private readonly int _maxCapacity;

    public ElementCache(int maxCapacity = 512)
    {
        _maxCapacity = maxCapacity;
    }

    public int Count => _idToElement.Count;

    /// <summary>
    /// Gets or adds an element to the cache. Returns the assigned ID.
    /// If the element (by RuntimeId) is already cached, returns the existing ID.
    /// </summary>
    public string GetOrAdd(AutomationElement element)
    {
        var runtimeIdKey = GetRuntimeIdKey(element);

        if (runtimeIdKey != null && _runtimeIdToId.TryGetValue(runtimeIdKey, out var existingId))
        {
            // Update the element reference (it may have been refreshed)
            _idToElement[existingId] = element;
            return existingId;
        }

        var id = $"e-{Interlocked.Increment(ref _nextId)}";

        _idToElement[id] = element;
        _insertionOrder.Enqueue(id);

        if (runtimeIdKey != null)
        {
            _runtimeIdToId[runtimeIdKey] = id;
        }

        EvictIfNeeded();

        return id;
    }

    /// <summary>
    /// Tries to retrieve a cached AutomationElement by its ID.
    /// </summary>
    public bool TryGet(string id, out AutomationElement? element)
    {
        return _idToElement.TryGetValue(id, out element);
    }

    /// <summary>
    /// Clears all cached elements.
    /// </summary>
    public void Clear()
    {
        _idToElement.Clear();
        _runtimeIdToId.Clear();
        while (_insertionOrder.TryDequeue(out _)) { }
    }

    private void EvictIfNeeded()
    {
        while (_idToElement.Count > _maxCapacity && _insertionOrder.TryDequeue(out var oldId))
        {
            if (_idToElement.TryRemove(oldId, out var removed))
            {
                var key = GetRuntimeIdKey(removed);
                if (key != null)
                {
                    _runtimeIdToId.TryRemove(key, out _);
                }
            }
        }
    }

    private static string? GetRuntimeIdKey(AutomationElement element)
    {
        try
        {
            var runtimeId = element.GetRuntimeId();
            if (runtimeId == null || runtimeId.Length == 0)
                return null;
            return string.Join(".", runtimeId);
        }
        catch
        {
            return null;
        }
    }
}
