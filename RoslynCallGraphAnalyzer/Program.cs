using System;

if (args.Length == 0)
{
    Console.WriteLine("Usage:\n" +
                      "  analyze <solutionPath> <entryMethod> <outputFolder>\n" +
                      "  paths <inputFolder> <targetMethod>");
    return;
}

if (args[0] == "analyze")
{
    if (args.Length < 4)
    {
        Console.WriteLine("Usage: analyze <solutionPath> <entryMethod> <outputFolder>");
        return;
    }

    var solutionPath = args[1];
    var entryMethod = args[2];
    var outputFolder = args[3];

    Directory.CreateDirectory(outputFolder);

    var analyzer = new CallGraphAnalyzer(entryMethod);
    await analyzer.AnalyzeSolution(solutionPath);
    analyzer.PrintJson(outputFolder);
}
else if (args[0] == "paths")
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: paths <inputFolder> <targetMethod>");
        return;
    }

    var inputFolder = args[1];
    var targetMethod = args[2];

    Directory.CreateDirectory(inputFolder);

    var inputPath = Path.Combine(inputFolder, "mtcallgraph.json");
    var finder = CallGraphPathFinder.FromFile(inputPath);
    finder.PrintPathsTo(targetMethod, inputFolder);
}
else
{
    Console.WriteLine($"Unknown command: {args[0]}");
}
