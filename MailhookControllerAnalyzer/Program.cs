//var analyzer = new CallGraphAnalyzer("SettingsStore.WebAppService.Core.Controllers.UserEventsController.Get(string, string, System.DateTime, System.DateTime)");
//await analyzer.AnalyzeSolution(@"C:\src\SkypeTeams-SettingsStore\Source\SettingsStore\SettingsStore.sln");

//var analyzer = new CallGraphAnalyzer("Microsoft.Teams.MiddleTier.Mailhook.Controllers.MailhookController.ProvisionEmailAddress(string)");
//await analyzer.AnalyzeSolution(@"C:\src\Teamspace-MiddleTier\Source\MiddleTier.sln");
//analyzer.PrintJson();


var finder = CallGraphPathFinder.FromFile(@"C:\temp\mtcallgraph.json");
finder.PrintPathsTo("SetThreadPropertyOnBehalfOfUser");