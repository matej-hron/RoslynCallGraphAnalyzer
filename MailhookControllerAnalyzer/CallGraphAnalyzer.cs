// Required NuGet packages:
// Microsoft.CodeAnalysis.CSharp.Workspaces
// Microsoft.Build.Locator

using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.FindSymbols;

public class CallGraphAnalyzer
{
    private readonly Dictionary<string, List<string>> _callGraph = new();
    private readonly HashSet<string> _visited = new();
    private readonly string _entryMethodName;
    private readonly Dictionary<string, List<IMethodSymbol>> _callsByMethod = new();
    private Solution? _solution;

    public CallGraphAnalyzer(string entryMethodName)
    {
        _entryMethodName = entryMethodName;
    }

    public async Task AnalyzeSolution(string solutionPath)
    {
        MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();
        _solution = await workspace.OpenSolutionAsync(solutionPath);

        await BuildCallIndex(_solution);

        foreach (var methodId in _callsByMethod.Keys)
        {
            if (methodId == _entryMethodName)
            {
                await TraverseCalls(methodId);
            }
        }
    }

    private async Task BuildCallIndex(Solution solution)
    {
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
                    var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
                    if (methodSymbol == null) continue;

                    var methodId = methodSymbol.ToDisplayString();
                    if (!_callsByMethod.ContainsKey(methodId))
                        _callsByMethod[methodId] = new List<IMethodSymbol>();

                    var invocations = methodDecl.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    foreach (var invocation in invocations)
                    {
                        var calledSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                        if (calledSymbol == null) continue;
                        if (calledSymbol.ContainingAssembly?.Name?.StartsWith("System") == true)
                            continue;

                        _callsByMethod[methodId].Add(calledSymbol);
                    }
                }
            }

            foreach (var type in compilation.GlobalNamespace.GetNamespaceTypes())
            {
                foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
                {
                    var methodId = method.ToDisplayString();
                    if (!_callsByMethod.ContainsKey(methodId))
                        _callsByMethod[methodId] = new List<IMethodSymbol>();

                    foreach (var syntaxRef in method.DeclaringSyntaxReferences)
                    {
                        var syntax = await syntaxRef.GetSyntaxAsync();
                        if (syntax is MethodDeclarationSyntax methodSyntax)
                        {
                            var tree = methodSyntax.SyntaxTree;
                            if (!compilation.SyntaxTrees.Contains(tree)) continue;

                            var semanticModel = compilation.GetSemanticModel(tree);
                            var invocations = methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>();

                            foreach (var invocation in invocations)
                            {
                                var calledSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                                if (calledSymbol == null) continue;
                                if (calledSymbol.ContainingAssembly?.Name?.StartsWith("System") == true)
                                    continue;

                                _callsByMethod[methodId].Add(calledSymbol);
                            }
                        }
                    }
                }
            }
        }
    }

    private async Task TraverseCalls(string methodId)
    {
        if (_visited.Contains(methodId)) return;
        _visited.Add(methodId);
        _callGraph[methodId] = new List<string>();

        if (!_callsByMethod.TryGetValue(methodId, out var callees)) return;

        foreach (var callee in callees)
        {
            var calleeId = callee.ToDisplayString();
            if (!_callGraph[methodId].Contains(calleeId))
                _callGraph[methodId].Add(calleeId);

            await TraverseCalls(calleeId);

            if (callee.ContainingType.TypeKind == TypeKind.Interface && _solution != null)
            {
                var impls = await SymbolFinder.FindImplementationsAsync(callee, _solution);
                foreach (var impl in impls.OfType<IMethodSymbol>())
                {
                    var implId = impl.ToDisplayString();
                    if (!_callGraph[methodId].Contains(implId))
                        _callGraph[methodId].Add(implId);

                    await TraverseCalls(implId);
                }
            }
        }
    }

    public void PrintJson()
    {
        var json = JsonSerializer.Serialize(_callGraph, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(@"c:\\temp\\mtcallgraph.json", json);
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

// var analyzer = new CallGraphAnalyzer("MyNamespace.MyController.MyMethod()");
// await analyzer.AnalyzeSolution(@"path\to\your.sln");
// analyzer.PrintJson();
