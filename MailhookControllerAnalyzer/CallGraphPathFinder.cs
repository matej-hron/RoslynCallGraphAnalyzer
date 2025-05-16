using System.Text.Json;
using System.Text.RegularExpressions;

public class CallGraphPathFinder
{
    private readonly Dictionary<string, List<string>> _callGraph;

    public CallGraphPathFinder(Dictionary<string, List<string>> callGraph)
    {
        _callGraph = callGraph;
    }

    public static CallGraphPathFinder FromFile(string path)
    {
        var json = File.ReadAllText(path);
        var graph = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
        return new CallGraphPathFinder(graph ?? new());
    }

    public void PrintPathsTo(string targetMethod)
    {
        var reverseGraph = BuildReverseGraph();
        var paths = new List<List<string>>();

        foreach (var kvp in _callGraph)
        {
            foreach (var callee in kvp.Value)
            {
                if (callee.Contains(targetMethod))
                {
                    var path = new List<string>();
                    var visited = new HashSet<string>();
                    Backtrack(kvp.Key, reverseGraph, path, paths, visited);
                }
            }
        }

        var simplifiedPaths = paths
            .Select(p => SimplifyPath(p))
            .Distinct(new SequenceComparer())
            .OrderBy(p => string.Join(" -> ", p))
            .ToList();

        File.WriteAllText("c:\\temp\\mtcallgraphpaths.json", JsonSerializer.Serialize(simplifiedPaths, new JsonSerializerOptions { WriteIndented = true }));

        var mermaid = GenerateMermaidGraph(simplifiedPaths);
        File.WriteAllText("c:\\temp\\mtcallgraphpaths.mmd", mermaid);
    }

    private static List<string> SimplifyPath(List<string> path)
    {
        var simplified = new List<string>();
        string? lastType = null;

        foreach (var fullMethod in path)
        {
            var paramStart = fullMethod.IndexOf('(');
            if (paramStart < 0)
            {
                simplified.Add(fullMethod);
                continue;
            }

            var methodNameStart = fullMethod.LastIndexOf('.', paramStart);
            if (methodNameStart < 0)
            {
                simplified.Add(fullMethod);
                continue;
            }

            var typeName = fullMethod.Substring(0, methodNameStart);
            var methodWithParams = fullMethod.Substring(methodNameStart + 1);

            if (typeName == lastType)
                simplified.Add("~" + methodWithParams);
            else
                simplified.Add(fullMethod);

            lastType = typeName;
        }

        return simplified;
    }

    private static string GenerateMermaidGraph(List<List<string>> paths)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("graph TD");
        var edges = new HashSet<string>();
        var labels = new Dictionary<string, string>();
        int idCounter = 1;

        string GetOrCreateId(string label)
        {
            if (!labels.ContainsKey(label))
                labels[label] = $"A{idCounter++}";
            return labels[label];
        }

        foreach (var path in paths)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                var fromLabel = path[i];
                var toLabel = path[i + 1];

                var fromId = GetOrCreateId(fromLabel);
                var toId = GetOrCreateId(toLabel);

                var edge = $"{fromId} --> {toId}";
                edges.Add(edge);
            }
        }

        foreach (var kvp in labels)
        {
            var safeLabel = kvp.Key.Replace("\"", "'");
            sb.AppendLine($"{kvp.Value}[\"{safeLabel}\"]");
        }

        foreach (var edge in edges)
        {
            sb.AppendLine(edge);
        }

        return sb.ToString();
    }

    private Dictionary<string, List<string>> BuildReverseGraph()
    {
        var reverse = new Dictionary<string, List<string>>();
        foreach (var kvp in _callGraph)
        {
            var caller = kvp.Key;
            foreach (var callee in kvp.Value)
            {
                if (!reverse.ContainsKey(callee))
                    reverse[callee] = new List<string>();
                reverse[callee].Add(caller);
            }
        }
        return reverse;
    }

    private void Backtrack(string current, Dictionary<string, List<string>> reverseGraph, List<string> path, List<List<string>> results, HashSet<string> visited)
    {
        if (visited.Contains(current)) return;
        visited.Add(current);
        path.Insert(0, current);

        if (!reverseGraph.ContainsKey(current))
        {
            results.Add(new List<string>(path));
        }
        else
        {
            foreach (var parent in reverseGraph[current])
            {
                Backtrack(parent, reverseGraph, path, results, visited);
            }
        }

        path.RemoveAt(0);
        visited.Remove(current);
    }

    private class SequenceComparer : IEqualityComparer<List<string>>
    {
        public bool Equals(List<string>? x, List<string>? y)
        {
            if (x == null || y == null) return false;
            if (x.Count != y.Count) return false;
            for (int i = 0; i < x.Count; i++)
            {
                if (x[i] != y[i]) return false;
            }
            return true;
        }

        public int GetHashCode(List<string> obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var s in obj)
                {
                    hash = hash * 31 + s.GetHashCode();
                }
                return hash;
            }
        }
    }
}