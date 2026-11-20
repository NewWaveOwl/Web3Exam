using ExamHelper;

if (args.Length == 3 && args[0] == "--scan")
{
    var actual = RepositoryScanner.Scan(args[1]);
    File.WriteAllText(args[2], actual.Markdown());
    Console.WriteLine(actual.Summary);
    return;
}

int checks = 0;
void Check(bool good, string message) { if (!good) throw new Exception("FAILED: " + message); checks++; Console.WriteLine("PASS " + message); }
var root = Path.Combine(Path.GetTempPath(), "web3-helper-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    Directory.CreateDirectory(Path.Combine(root, "Controllers"));
    Directory.CreateDirectory(Path.Combine(root, "Repositories"));
    Directory.CreateDirectory(Path.Combine(root, "bin"));
    File.WriteAllText(Path.Combine(root, "Test.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
    var source = """
// EnsureCreated(); this comment must not trigger a finding
[Route("api/items")]
public class ItemsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<object> Get() {
        var rows = await db.Items.ToListAsync();
        return rows.Where(x => x.IsActive);
    }
}
""";
    var file = Path.Combine(root, "Controllers", "ItemsController.cs");
    File.WriteAllText(file, source);
    File.WriteAllText(Path.Combine(root, "Repositories", "Repository.cs"), "public class Repository { public IQueryable<Item> Query() => db.Items; public void Commit() => db.SaveChanges(); }");
    File.WriteAllText(Path.Combine(root, "bin", "Ignored.cs"), "db.EnsureCreated();");
    var report = RepositoryScanner.Scan(root);
    Check(report.FileCount == 3, "ignores bin and scans source/project files");
    Check(report.Inventory.Any(i => i.Kind == "Project" && i.Name.Contains("net8.0")), "reads project framework");
    Check(report.Inventory.Count(i => i.Kind == "Route") == 2, "finds attribute route and verb");
    Check(report.Findings.Any(f => f.Title.Contains("DbContext") && f.Line == 3), "reports controller context and exact line");
    Check(report.Findings.Any(f => f.Title.Contains("Materialization")), "finds early-materialization review pattern");
    Check(report.Findings.Any(f => f.Title.Contains("IQueryable")), "finds public IQueryable return");
    Check(report.Findings.Any(f => f.Title.Contains("Repository file contains a save")), "finds repository save");
    Check(!report.Findings.Any(f => f.Title.Contains("EnsureCreated")), "ignores commented and excluded-folder patterns");
    Check(File.ReadAllText(file) == source, "scanner does not mutate inspected source");
    Check(report.Markdown().Contains("Controllers") && report.Markdown().Contains("Heuristic"), "report contains findings and limitations");
    var cts = new CancellationTokenSource(); cts.Cancel();
    try { RepositoryScanner.Scan(root, cts.Token); Check(false, "cancellation"); }
    catch (OperationCanceledException) { Check(true, "scan supports cancellation"); }

    var sections = Notes.Parse("# Heading\nIntro\n## DTOs\nRequest annotations\n```csharp\n## not a heading\n```\n## Services\nRules", "General");
    Check(sections.Count == 3, "notes preserve fenced headings as code");
    Check(Notes.Search(sections, "request annotations").Single().Title == "DTOs", "search finds full-body terms");
    Check(Notes.Search(sections, "not-present").Count == 0, "search handles no results");
    Check(Templates.ValidInputs("School.Api", "Course"), "accepts valid namespace/entity");
    Check(!Templates.ValidInputs("School;evil", "Course") && !Templates.ValidInputs("Api", "class"), "rejects malformed names and reserved keywords");
    Check(Templates.All.Count >= 12, "includes all template categories");
    foreach (var template in Templates.All)
    {
        var text = template.Generate("School.Api", "Course");
        Check(!string.IsNullOrWhiteSpace(text) && !text.Contains("{{n}}") && !text.Contains("{{e}}"), "generates " + template.Title);
    }
    Console.WriteLine($"SUCCESS: {checks} checks passed.");
}
finally
{
    // The exact GUID-created fixture directory is owned exclusively by this check.
    if (Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
        && Path.GetFileName(root).StartsWith("web3-helper-checks-")) Directory.Delete(root, true);
}
