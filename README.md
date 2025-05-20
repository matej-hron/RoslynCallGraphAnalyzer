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
RoslynCallGraphAnalyzer.exe analyze "C:\src\Teamspace-MiddleTier\Source\MiddleTier.sln" "Microsoft.Teams.MiddleTier.Mailhook.Controllers.MailhookController.ProvisionEmailAddress(string)" "C:\temp\callgraph"
```

This produces:

* `mtcallgraph.json` — full call graph starting from `entryMethod`

### ⚠️ Performance Note

> Analyzing large solutions may take several minutes or longer depending on size and complexity. Roslyn processes all syntax trees and semantic models to build the call graph.

### 2. Find all paths to a target method

```
RoslynCallGraphAnalyzer.exe paths <inputFolder> <targetMethod>
```

Example:

```
.\RoslynCallGraphAnalyzer.exe paths "C:\temp\callgraph" "SetThreadPropertyOnBehalfOfUser"
```

This produces:

* `callgraphpaths.json` — all simplified call paths
* `callgraphpaths.mmd` — call paths in Mermaid syntax
* `callgraphleafs.json` — leaf methods

### Visualizing

To view the call graph paths:

1. Copy contents of `callgraphpaths.mmd`
2. Go to [https://mermaid.live](https://mermaid.live)
3. Paste and render the graph

> 📷 You can see an example of a rendered Mermaid diagram below:
>
> ![Example Mermaid Graph](example.jpg)
>
> Or view it directly on Mermaid Live Editor: [https://mermaid.live/edit#pako\:eNp1kE1PwzAMhl\_F8pWwUraCkJqhVlazs7GxHSWx0AJNHKZP\_vcoDbebsbBzDMM8Z3fXKNwMl2j-icj1ESAHCndOQymuHoxl6yB65lMbzy8kH2sF2TjKz9koCrIZWWVvIEQESarvFGEciSk-dRmhhpPg5W27zPlqzmFyXqht\_JZ1FdhlokjeWsXzZ4lzYXfhQ6mfqZHOYLGWIR7MuQEd4fKkkXbXM6YbpCmcwHz\_3\_56-d-oKtg](https://mermaid.live/edit#pako:eNp1kE1PwzAMhl_F8pWwUraCkJqhVlazs7GxHSWx0AJNHKZP_vcoDbebsbBzDMM8Z3fXKNwMl2j-icj1ESAHCndOQymuHoxl6yB65lMbzy8kH2sF2TjKz9koCrIZWWVvIEQESarvFGEciSk-dRmhhpPg5W27zPlqzmFyXqht_JZ1FdhlokjeWsXzZ4lzYXfhQ6mfqZHOYLGWIR7MuQEd4fKkkXbXM6YbpCmcwHz_3_56-d-oKtg)

---

Generated with ❤️ by AI and refined by human input.
