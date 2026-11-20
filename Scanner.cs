using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ExamHelper;

public sealed record Finding(string Level, string Title, string RelativePath, int Line, string Detail);
public sealed record InventoryItem(string Kind, string Name, string RelativePath, int Line);
public sealed record ScanReport(string Root, IReadOnlyList<Finding> Findings,
    IReadOnlyList<InventoryItem> Inventory, int FileCount, IReadOnlyList<string> Warnings)
{
    public string Summary => $"{FileCount} source/project files • {Inventory.Count(i => i.Kind == "Project")} projects • " +
        $"{Inventory.Count(i => i.Kind == "Type")} type declarations • {Inventory.Count(i => i.Kind == "Route")} route/verb attributes • {Findings.Count} review prompts" +
        (Warnings.Count > 0 ? $"\n{Warnings.Count} scan limits/read warnings (included in report)." : "\nNo repository files were changed. Source patterns need human review.");

    public string Markdown()
    {
        static string Safe(string s) => s.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        var b = new StringBuilder("# Repository review report\n\n");
        b.AppendLine($"Folder: `{Root}`\n\n{Summary}\n");
        b.AppendLine("Heuristic source inspection only. A finding is not a proven bug; absence is not proof of compliance. Comments are masked, but full C# semantic/control-flow analysis is not performed. No repository code was executed.\n");
        b.AppendLine("## Findings\n\n| Level | Review | Source | Why / next step |\n|---|---|---|---|");
        foreach (var f in Findings) b.AppendLine($"| {f.Level} | {Safe(f.Title)} | {Safe(f.RelativePath)}:{f.Line} | {Safe(f.Detail)} |");
        b.AppendLine("\n## Inventory\n\n| Kind | Declaration | Source |\n|---|---|---|");
        foreach (var i in Inventory) b.AppendLine($"| {i.Kind} | {Safe(i.Name)} | {Safe(i.RelativePath)}:{i.Line} |");
        if (Warnings.Count > 0) b.AppendLine("\n## Scan warnings\n\n" + string.Join("\n", Warnings.Select(w => "- " + w)));
        return b.ToString();
    }
}

public static class RepositoryScanner
{
    private static readonly HashSet<string> Skipped = new(StringComparer.OrdinalIgnoreCase)
    { "bin", "obj", ".git", ".vs", "node_modules", "packages", "TestResults", ".idea", ".codex", ".agents" };
    private static MatchCollection Matches(string s, string pattern) => Regex.Matches(s, pattern,
        RegexOptions.Multiline, TimeSpan.FromSeconds(2));
    public static string WithoutComments(string source) => Regex.Replace(source,
        "@\"(?:\"\"|[^\"])*\"|\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*'|//[^\\r\\n]*|/\\*[\\s\\S]*?\\*/",
        m => m.Value.StartsWith("//") || m.Value.StartsWith("/*")
            ? Regex.Replace(m.Value, "[^\\r\\n]", " ") : m.Value,
        RegexOptions.None, TimeSpan.FromSeconds(2));

    public static Task<ScanReport> ScanAsync(string root, CancellationToken token = default) =>
        Task.Run(() => Scan(root, token), token);

    public static ScanReport Scan(string root, CancellationToken token = default)
    {
        root = Path.GetFullPath(root);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Choose an existing repository folder.");
        var findings = new List<Finding>();
        var inventory = new List<InventoryItem>();
        var warnings = new List<string>();
        var paths = new List<string>();
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.Count > 0 && paths.Count < 5000)
        {
            token.ThrowIfCancellationRequested();
            var dir = pending.Pop();
            try
            {
                foreach (var child in Directory.EnumerateDirectories(dir))
                {
                    if (Skipped.Contains(Path.GetFileName(child))) continue;
                    try { if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) pending.Push(child); }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException) { warnings.Add($"Skipped folder: {Path.GetRelativePath(root, child)} ({e.GetType().Name})."); }
                }
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    if (paths.Count >= 5000) break;
                    if (Path.GetExtension(file) is not (".cs" or ".csproj")) continue;
                    if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0) paths.Add(file);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            { warnings.Add($"Could not enumerate {Path.GetRelativePath(root, dir)} ({e.GetType().Name})."); }
        }
        if (paths.Count >= 5000) warnings.Add("Reached the 5,000 source/project file limit. Select a smaller repository subtree for a complete review.");
        int read = 0;
        foreach (var file in paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            token.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, file);
            try
            {
                if (new FileInfo(file).Length > 2_000_000) { warnings.Add($"Skipped file larger than 2 MB: {relative}"); continue; }
                var raw = File.ReadAllText(file); read++;
                if (file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    var x = XDocument.Parse(raw);
                    string Values(string key) => string.Join(", ", x.Descendants().Where(n => n.Name.LocalName == key).Select(n => n.Value));
                    inventory.Add(new("Project", $"{Path.GetFileNameWithoutExtension(file)} | {Values("TargetFramework")}{Values("TargetFrameworks")} | {x.Root?.Attribute("Sdk")?.Value}", relative, 1));
                    foreach (var package in x.Descendants().Where(n => n.Name.LocalName == "PackageReference"))
                        inventory.Add(new("Package", $"{package.Attribute("Include")?.Value} {package.Attribute("Version")?.Value ?? "(central/inherited version)"}", relative, 1));
                    continue;
                }
                if (file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) continue;
                var code = WithoutComments(raw);
                int Line(int index) => 1 + code.AsSpan(0, Math.Clamp(index, 0, code.Length)).Count('\n');
                foreach (Match m in Matches(code, @"\b(?:class|record(?:\s+class)?|interface|enum)\s+(?<name>@?\w+)"))
                    inventory.Add(new("Type", m.Groups["name"].Value, relative, Line(m.Index)));
                foreach (Match m in Matches(code, @"\[(?:Route|HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)\b[^\]\r\n]*\]"))
                    inventory.Add(new("Route", m.Value, relative, Line(m.Index)));
                foreach (Match m in Matches(code, "\\bMap(?:Get|Post|Put|Delete|Patch|Group)\\s*\\(\\s*\"[^\"]*\""))
                    inventory.Add(new("Route", m.Value, relative, Line(m.Index)));
                foreach (Match m in Matches(code, @"\b(?:AddScoped|AddSingleton|AddTransient)\s*<[^;\r\n]+"))
                    inventory.Add(new("DI", m.Value.Trim(), relative, Line(m.Index)));

                void Rule(string pattern, string title, string detail, string level = "Review")
                {
                    var matches = Matches(code, pattern);
                    if (matches.Count > 0)
                        findings.Add(new(level, title + (matches.Count > 1 ? $" ({matches.Count} matches)" : ""), relative, Line(matches[0].Index), detail));
                }
                var controller = relative.Contains("Controller", StringComparison.OrdinalIgnoreCase) || code.Contains(": ControllerBase");
                var repository = relative.Contains("Repositor", StringComparison.OrdinalIgnoreCase);
                if (controller)
                {
                    Rule(@"\b\w*DbContext\b", "Controller mentions DbContext", "If services are required, move EF access to repositories and workflow decisions to services. A type mention is not proof of injected usage.");
                    Rule(@"\bI(?:UnitOfWork|\w*Repository)\b", "Controller mentions persistence abstraction", "Inspect constructor dependencies. A required thin controller should call services, not repositories or UoW.");
                    Rule(@"\bSaveChanges(?:Async)?\s*\(", "Controller commits directly", "For the course architecture, move orchestration/commit to service -> UoW. Preserve original transaction behavior.");
                    Rule(@"\[Authorize(?:\([^\]]*\))?\][\s\S]{0,350}\bclass\b", "Possible class-level authorization", "Check whether public reads and protected writes share this controller. Class-wide authorization may be intentional; inspect the route/scope contract.");
                }
                if (repository)
                {
                    Rule(@"\bSaveChanges(?:Async)?\s*\(", "Repository file contains a save", "Inspect the containing class. Repositories should stage changes; UoW can legitimately share a Repositories folder and save there.");
                    Rule(@"\bpublic\s+(?:virtual\s+)?IQueryable\s*<", "Public IQueryable return", "If the rubric prohibits query leakage, execute inside repository and return a materialized list/DTO.");
                }
                Rule(@"\bEnsureCreated(?:Async)?\s*\(", "EnsureCreated used", "If migrations are required, use committed migrations/Migrate. Do not reset a real database blindly.");
                Rule(@"\b(?:db|context|Db|Context|Set|set|_db|_context)\s*\.\s*Update\s*\(|\bEntityState\s*\.\s*Modified\b", "Broad update pattern", "Check that only allowed fields are marked modified. The audit/tracker state comparison can also match this pattern; review context before changing it.");
                Rule(@"\bToList(?:Async)?\s*\([^)]*\)[\s\S]{0,650}?(?:\.Where\s*\(|\.GroupBy\s*\(|\.Sum\s*\()", "Materialization followed by LINQ nearby", "Check whether the same result is filtered/grouped/summed in memory. Nearby methods or change-tracker lists can be legitimate; this heuristic has no data-flow analysis.");
                Rule(@"\bAsEnumerable\s*\(", "AsEnumerable query boundary", "Subsequent LINQ uses memory rather than EF translation. Inspect required filters/groups/aggregates after this call.");
                Rule(@"\bSkip\s*\(", "Paging: verify stable ordering", "Confirm OrderBy plus a unique tie-breaker occurs on the same query before Skip/Take. Verify bounds from the actual exam sheet.", "Check");
                Rule(@"\bAddSingleton\s*<[^>]*(?:Repository|UnitOfWork|DbContext|Service)", "Singleton persistence/service registration", "Singleton is wrong when it captures a scoped DbContext. Stateless singleton services may be valid; inspect actual dependencies.");
                Rule(@"\bLog(?:Information|Warning|Error|Debug)\s*\(\s*\$", "Interpolated log message", "Use constant templates and named properties for required structured logs. Exclude tokens, secrets, bodies and personal data.");
                Rule(@"\bUseAuthorization\s*\([\s\S]{0,2000}\bUseAuthentication\s*\(", "Authorization before authentication nearby", "Inspect the actual middleware pipeline; authentication should populate the user before authorization checks policies.");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or RegexMatchTimeoutException or System.Xml.XmlException)
            { warnings.Add($"Skipped/reduced inspection of {relative}: {e.GetType().Name}."); }
        }
        return new(root, findings, inventory, read, warnings);
    }
}
