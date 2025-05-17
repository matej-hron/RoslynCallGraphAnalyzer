# Roslyn Call Graph Analyzer

This tool analyzes a .NET solution using Roslyn and extracts method call graphs, allowing tracing of paths from an entry method to a target method.

> **AI-generated & human-assisted**

## Features

* Builds call graph from Roslyn syntax and semantic model
* Resolves method implementations (including interface implementations)
* Outputs full call graph in JSON format
* Supports path tracing to a specific method
* Exports paths in simplified text and Mermaid diagram formats

## Usage

### 1. Analyze solution and export call graph

```
RoslynCallGraphAnalyzer.exe analyze <solutionPath> <entryMethod> <outputFolder>
```

Example:

```
.\RoslynCallGraphAnalyzer.exe analyze "C:\src\Teamspace-MiddleTier\Source\MiddleTier.sln" "Microsoft.Teams.MiddleTier.Mailhook.Controllers.MailhookController.ProvisionEmailAddress(string)" "C:\temp\callgraph"
```

This produces:

* `mtcallgraph.json` — full call graph starting from `entryMethod`

### 2. Find all paths to a target method

```
RoslynCallGraphAnalyzer.exe paths <inputFolder> <targetMethod>
```

Example:

```
RoslynCallGraphAnalyzer.exe paths "C:\temp\callgraph" "SetThreadPropertyOnBehalfOfUser"
```

This produces:

* `mtcallgraphpaths.json` — all simplified call paths
* `mtcallgraphpaths.mmd` — call paths in Mermaid syntax
* `mtcallgraphleafs.json` — leaf methods

### Visualizing

To view the call graph paths:

1. Copy contents of `mtcallgraphpaths.mmd`
2. Go to [https://mermaid.live](https://mermaid.live)
3. Paste and render the graph

---

Generated with ❤️ by AI and refined by human input.
