#if MACOS
using System.Collections.Concurrent;
using System.Globalization;

namespace UIAutomation.Core.Platforms.MacOS;

/// <summary>
/// Thread-safe cache that assigns string IDs to retained AXUIElement handles.
/// </summary>
public sealed class MacElementCache
{
    private readonly ConcurrentDictionary<string, IntPtr> _idToElement = new();
    private readonly ConcurrentDictionary<string, string> _identityToId = new();
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private readonly int _maxCapacity;
    private int _nextId;

    public MacElementCache(int maxCapacity = 512)
    {
        _maxCapacity = maxCapacity;
    }

    public int Count => _idToElement.Count;

    public string GetOrAdd(IntPtr element)
    {
        if (element == IntPtr.Zero)
        {
            throw new ArgumentException("AXUIElement handle cannot be zero.", nameof(element));
        }

        var identityKey = GetIdentityKey(element);
        if (_identityToId.TryGetValue(identityKey, out var existingId))
        {
            var retained = MacNativeMethods.CFRetain(element);
            if (_idToElement.TryGetValue(existingId, out var oldElement))
            {
                _idToElement[existingId] = retained;
                MacNativeMethods.CFRelease(oldElement);
            }
            else
            {
                _idToElement[existingId] = retained;
            }

            return existingId;
        }

        var id = $"e-{Interlocked.Increment(ref _nextId)}";
        _idToElement[id] = MacNativeMethods.CFRetain(element);
        _identityToId[identityKey] = id;
        _insertionOrder.Enqueue(id);

        EvictIfNeeded();
        return id;
    }

    public bool TryGet(string id, out IntPtr element) =>
        _idToElement.TryGetValue(id, out element);

    public void Clear()
    {
        foreach (var element in _idToElement.Values)
        {
            MacNativeMethods.CFRelease(element);
        }

        _idToElement.Clear();
        _identityToId.Clear();
        while (_insertionOrder.TryDequeue(out _)) { }
    }

    private void EvictIfNeeded()
    {
        while (_idToElement.Count > _maxCapacity && _insertionOrder.TryDequeue(out var oldId))
        {
            if (_idToElement.TryRemove(oldId, out var removed))
            {
                _identityToId.TryRemove(GetIdentityKey(removed), out _);
                MacNativeMethods.CFRelease(removed);
            }
        }
    }

    private static string GetIdentityKey(IntPtr element) =>
        MacNativeMethods.CFHash(element).ToString(CultureInfo.InvariantCulture);
}
#endif
