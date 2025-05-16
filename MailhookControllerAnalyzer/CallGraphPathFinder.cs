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

        var simplifiedPairs = paths
            .Select(p => (original: p, simplified: SimplifyPath(p)))
            .DistinctBy(pair => string.Join(" -> ", pair.simplified))
            .OrderBy(pair => string.Join(" -> ", pair.simplified))
            .ToList();

        var simplifiedPaths = simplifiedPairs.Select(p => p.simplified).ToList();
        File.WriteAllText("c:\\temp\\mtcallgraphpaths.json", JsonSerializer.Serialize(simplifiedPaths, new JsonSerializerOptions { WriteIndented = true }));

        var mermaid = MermaidGraphRenderer.Generate(simplifiedPairs, targetMethod);
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
