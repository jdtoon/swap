namespace Swap.Modularity.Internal;

internal static class TopologicalSort
{
    public static IReadOnlyList<T> Sort<T>(IEnumerable<T> nodes, Func<T, IEnumerable<string>> dependsOn, Func<T, string> name)
    {
        var map = nodes.ToDictionary(name, n => new Node<T>(n, name(n), new HashSet<string>(dependsOn(n))));
        var incoming = map.Values.ToDictionary(n => n.Name, n => new HashSet<string>());

        foreach (var n in map.Values)
        {
            foreach (var dep in n.DependsOn)
            {
                if (!map.ContainsKey(dep))
                    throw new InvalidOperationException($"Module '{n.Name}' depends on missing module '{dep}'.");
                incoming[n.Name].Add(dep);
            }
        }

        var result = new List<T>();
        var ready = new Queue<Node<T>>(map.Values.Where(n => incoming[n.Name].Count == 0));

        while (ready.Count > 0)
        {
            var n = ready.Dequeue();
            result.Add(n.Value);
            foreach (var m in map.Values)
            {
                if (m.DependsOn.Contains(n.Name))
                {
                    incoming[m.Name].Remove(n.Name);
                    if (incoming[m.Name].Count == 0)
                        ready.Enqueue(m);
                }
            }
        }

        if (result.Count != map.Count)
        {
            var cycle = string.Join(" -> ", incoming.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key));
            throw new InvalidOperationException($"Module dependency cycle detected: {cycle}");
        }

        return result;
    }

    private sealed class Node<T>(T value, string name, HashSet<string> dependsOn)
    {
        public T Value { get; } = value;
        public string Name { get; } = name;
        public HashSet<string> DependsOn { get; } = dependsOn;
    }
}
