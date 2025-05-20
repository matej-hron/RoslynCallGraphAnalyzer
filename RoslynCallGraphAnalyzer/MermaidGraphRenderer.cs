using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public static class MermaidGraphRenderer
{
    public static string Generate(List<(List<string> original, List<string> simplified)> pairs, string targetMethod)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("graph TD");
        var edges = new HashSet<string>();
        var idMap = new Dictionary<string, string>();
        var labelMap = new Dictionary<string, string>();
        var highlightNodes = new HashSet<string>();
        int idCounter = 1;

        foreach (var (original, simplified) in pairs)
        {
            for (int j = 0; j < original.Count; j++)
            {
                var full = original[j];
                if (!idMap.ContainsKey(full))
                {
                    string id = $"A{idCounter++}";
                    idMap[full] = id;

                    // Properly extract method name and namespace
                    var match = Regex.Match(full, @"^(.*)\.([^.]+\(.*\))$");
                    if (match.Success)
                    {
                        var ns = match.Groups[1].Value;
                        var methodSignature = match.Groups[2].Value;
                        labelMap[id] = methodSignature + (string.IsNullOrEmpty(ns) ? "" : $" [{ns}]");
                    }
                    else
                    {
                        labelMap[id] = full; // fallback
                    }
                }
            }

            for (int j = 0; j < original.Count - 1; j++)
            {
                var from = idMap[original[j]];
                var to = idMap[original[j + 1]];
                edges.Add($"{from} --> {to}");
            }

            // always highlight the leaf node of the path
            var last = original.Last();
            if (idMap.TryGetValue(last, out var lastId))
            {
                highlightNodes.Add(lastId);
            }
        }

        foreach (var kvp in labelMap)
        {
            sb.AppendLine($"{kvp.Key}[\"{kvp.Value}\"]");
        }

        foreach (var node in highlightNodes)
        {
            sb.AppendLine($"style {node} fill:#ffffff,stroke:#d33,stroke-width:2px,color:#000000");
        }

        foreach (var edge in edges)
        {
            sb.AppendLine(edge);
        }

        return sb.ToString();
    }

    public static void GenerateMermaidLiveLink(string mermaidFilePath, string content)
    {
        var compressed = CompressToDeflate(content);
        var base64 = Convert.ToBase64String(compressed)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var url = $"https://mermaid.live/edit#pako:{base64}";
        Console.WriteLine("Mermaid preview link:");
        Console.WriteLine(url);

        // Optionally write it to file
        File.WriteAllText(Path.ChangeExtension(mermaidFilePath, ".url.txt"), url);
    }

    private static byte[] CompressToDeflate(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(bytes, 0, bytes.Length);
        }
        output.Position = 0;
        return output.ToArray();
    }
}
