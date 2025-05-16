// Required NuGet packages:
// Microsoft.CodeAnalysis.CSharp.Workspaces
// Microsoft.Build.Locator

using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

public class CallGraphAnalyzer
{
    private readonly Dictionary<string, List<string>> _callGraph = new();
    private readonly HashSet<string> _visited = new();
    private readonly string _entryMethodName;

    public CallGraphAnalyzer(string entryMethodName)
    {
        _entryMethodName = entryMethodName;
    }

    public async Task AnalyzeSolution(string solutionPath)
    {
        MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var document in project.Documents)
            {
                var syntaxRoot = await document.GetSyntaxRootAsync();
                var semanticModel = await document.GetSemanticModelAsync();
                if (syntaxRoot == null || semanticModel == null) continue;

                var methodDeclarations = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var methodDecl in methodDeclarations)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(methodDecl);
                    if (symbol == null) continue;

                    var methodId = symbol.ToDisplayString();
                    if (methodId.Contains(_entryMethodName))
                    {
                        await TraverseCalls(symbol, solution);
                    }
                }
            }
        }
    }

    private async Task TraverseCalls(IMethodSymbol methodSymbol, Solution solution)
    {
        var methodId = methodSymbol.ToDisplayString();
        if (_visited.Contains(methodId)) return;
        _visited.Add(methodId);
        _callGraph[methodId] = new List<string>();

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var document in project.Documents)
            {
                var syntaxRoot = await document.GetSyntaxRootAsync();
                var semanticModel = await document.GetSemanticModelAsync();
                if (syntaxRoot == null || semanticModel == null) continue;

                var invocations = syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>();
                foreach (var invocation in invocations)
                {
                    var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol == null) continue;

                    var containingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    if (containingMethod == null) continue;

                    var containerSymbol = semanticModel.GetDeclaredSymbol(containingMethod);
                    if (!SymbolEqualityComparer.Default.Equals(containerSymbol, methodSymbol)) continue;

                    var calledId = symbol.ToDisplayString();
                    _callGraph[methodId].Add(calledId);
                    await TraverseCalls(symbol, solution);

                    // Handle interface calls
                    if (symbol.ContainingType.TypeKind == TypeKind.Interface)
                    {
                        var impls = await FindImplementations(symbol, solution);
                        foreach (var impl in impls)
                        {
                            var implId = impl.ToDisplayString();
                            _callGraph[methodId].Add(implId);
                            await TraverseCalls(impl, solution);
                        }
                    }
                }
            }
        }
    }

    private async Task<List<IMethodSymbol>> FindImplementations(IMethodSymbol interfaceMethod, Solution solution)
    {
        var results = new List<IMethodSymbol>();
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var type in compilation.GlobalNamespace.GetNamespaceTypes())
            {
                if (!type.AllInterfaces.Contains(interfaceMethod.ContainingType)) continue;

                var match = type.GetMembers().OfType<IMethodSymbol>()
                    .FirstOrDefault(m => m.Name == interfaceMethod.Name);
                if (match != null) results.Add(match);
            }
        }
        return results;
    }

    public void PrintJson()
    {
        var json = JsonSerializer.Serialize(_callGraph, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(@"c:\temp\callgraph.json", json);
        Console.WriteLine(json);
    }
}

public static class Extensions
{
    public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol nestedNs)
                foreach (var nested in GetNamespaceTypes(nestedNs))
                    yield return nested;
            else if (member is INamedTypeSymbol type)
                yield return type;
        }
    }
} // usage example

// var analyzer = new CallGraphAnalyzer("MyController.MyMethod");
// await analyzer.AnalyzeSolution(@"path\to\your.sln");
// analyzer.PrintJson();
