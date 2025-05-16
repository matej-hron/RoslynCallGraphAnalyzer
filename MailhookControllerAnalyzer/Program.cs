var analyzer = new CallGraphAnalyzer("SettingsStore.WebAppService.Core.Controllers.AboutController.Get");
await analyzer.AnalyzeSolution(@"C:\src\SkypeTeams-SettingsStore\Source\SettingsStore\SettingsStore.sln");
analyzer.PrintJson();
