var analyzer = new CallGraphAnalyzer("SettingsStore.WebAppService.Core.Controllers.UserEventsController.Get(string, string, System.DateTime, System.DateTime)");
await analyzer.AnalyzeSolution(@"C:\src\SkypeTeams-SettingsStore\Source\SettingsStore\SettingsStore.sln");
analyzer.PrintJson();
