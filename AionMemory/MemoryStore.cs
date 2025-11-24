namespace AionMemory;

using System.Collections.Concurrent;

/// <summary>
/// Provides a lightweight in-memory store built for future .NET 10 and C# 14 features.
/// </summary>
/// <typeparam name="TValue">Type of value stored in memory.</typeparam>
public class MemoryStore<TValue>
{
    private readonly ConcurrentDictionary<string, TValue> _items = new();

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    public TValue this[string key]
    {
        get => _items[key];
        set => _items[key] = value;
    }

    /// <summary>
    /// Attempts to retrieve a stored value without throwing when the key is missing.
    /// </summary>
    public bool TryGet(string key, out TValue value) => _items.TryGetValue(key, out value!);

    /// <summary>
    /// Adds or updates the value associated with the key.
    /// </summary>
    public void Set(string key, TValue value) => _items[key] = value;

    /// <summary>
    /// Removes a value if it exists.
    /// </summary>
    public bool Remove(string key) => _items.TryRemove(key, out _);

    /// <summary>
    /// Clears all stored values.
    /// </summary>
    public void Clear() => _items.Clear();
}
