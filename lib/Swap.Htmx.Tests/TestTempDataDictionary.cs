using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;

namespace Swap.Htmx.Tests;

public class TestTempDataDictionary : ITempDataDictionary
{
    private readonly Dictionary<string, object?> _data = new();

    public object? this[string key] { get => _data.ContainsKey(key) ? _data[key] : null; set => _data[key] = value; }

    public ICollection<string> Keys => _data.Keys;

    public ICollection<object?> Values => _data.Values;

    public int Count => _data.Count;

    public bool IsReadOnly => false;

    public void Add(string key, object? value) => _data.Add(key, value);

    public void Add(KeyValuePair<string, object?> item) => _data.Add(item.Key, item.Value);

    public void Clear() => _data.Clear();

    public bool Contains(KeyValuePair<string, object?> item) => _data.Contains(item);

    public bool ContainsKey(string key) => _data.ContainsKey(key);

    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, object?>>)_data).CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _data.GetEnumerator();

    public void Keep() { }

    public void Keep(string key) { }

    public void Load() { }

    public object? Peek(string key) => _data.ContainsKey(key) ? _data[key] : null;

    public bool Remove(string key) => _data.Remove(key);

    public bool Remove(KeyValuePair<string, object?> item) => _data.Remove(item.Key);

    public void Save() { }

    public bool TryGetValue(string key, out object? value) => _data.TryGetValue(key, out value);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _data.GetEnumerator();
}
