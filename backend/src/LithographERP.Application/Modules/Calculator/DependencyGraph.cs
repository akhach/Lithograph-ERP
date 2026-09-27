namespace LithographERP.Application.Modules.Calculator;

public sealed record DependencyAnalysis(bool HasCycle, IReadOnlyList<string> Cycle, IReadOnlyList<string> EvaluationOrder);

public static class DependencyGraph
{
    public static DependencyAnalysis Analyze(IReadOnlyDictionary<string, IReadOnlyCollection<string>> dependencies)
    {
        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new List<string>();
        var order = new List<string>();
        List<string>? cycle = null;

        foreach (var key in dependencies.Keys)
        {
            if (cycle is not null)
            {
                break;
            }

            Visit(key);
        }

        return new DependencyAnalysis(cycle is not null, cycle ?? [], cycle is null ? order : []);

        void Visit(string key)
        {
            if (cycle is not null || !dependencies.ContainsKey(key))
            {
                return;
            }

            if (state.TryGetValue(key, out var mark))
            {
                if (mark == 1)
                {
                    var start = stack.IndexOf(key);
                    cycle = stack.Skip(start).Append(key).ToList();
                }

                return;
            }

            state[key] = 1;
            stack.Add(key);
            foreach (var edge in dependencies[key])
            {
                Visit(edge);
                if (cycle is not null)
                {
                    return;
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[key] = 2;
            order.Add(key);
        }
    }

    public static string FormatCycle(IReadOnlyList<string> cycle) =>
        "A circular Calculator dependency was detected: " + string.Join(" → ", cycle);
}
