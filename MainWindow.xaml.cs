using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ExamHelper;

public partial class MainWindow : Window
{
    private IReadOnlyList<NoteSection> sections = [];
    private ScanReport? report;
    private string? selectedFile;
    private CancellationTokenSource? scanCancellation;

    public MainWindow()
    {
        InitializeComponent();
        var loaded = new List<NoteSection>();
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".md")))
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            loaded.AddRange(Notes.Parse(reader.ReadToEnd(), resource.Contains("LIBRARY") ? "Library practice" : "General exam guide"));
        }
        sections = loaded.OrderBy(s => s.Source == "General exam guide" ? 0 : 1).ToList();
        SectionsList.ItemsSource = sections;
        SectionsList.SelectedIndex = 0;
        TemplatesList.ItemsSource = Templates.All;
        TemplatesList.SelectedIndex = 0;
        RepoPath.Text = AppContext.BaseDirectory;
        Status.Text = $"Ready • {sections.Count} embedded note sections • {Templates.All.Count} templates • offline";
        Closed += (_, _) => scanCancellation?.Cancel();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog { Title = "Select an ASP.NET Core repository", Multiselect = false };
        if (Directory.Exists(RepoPath.Text)) picker.InitialDirectory = RepoPath.Text;
        if (picker.ShowDialog(this) == true) RepoPath.Text = picker.FolderName;
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        if (scanCancellation is not null) { scanCancellation.Cancel(); return; }
        scanCancellation = new();
        ScanButton.Content = "Cancel scan";
        RepoPath.IsEnabled = false;
        Status.Text = "Scanning source only… no project code is being executed.";
        try
        {
            var result = await RepositoryScanner.ScanAsync(RepoPath.Text.Trim(), scanCancellation.Token);
            report = result;
            FindingsGrid.ItemsSource = result.Findings;
            InventoryGrid.ItemsSource = result.Inventory;
            ScanSummary.Text = result.Summary;
            Status.Text = $"Scan complete • {result.Findings.Count} review prompts • {result.Root}";
            if (result.Findings.Count > 0) FindingsGrid.SelectedIndex = 0;
            else { FindingDetail.Text = "No matching review patterns found. Inspect the inventory and actual exam requirements; this is not a compliance certificate."; SourcePreview.Clear(); selectedFile = null; }
        }
        catch (OperationCanceledException) { Status.Text = "Scan cancelled. Previous results, if any, are unchanged."; }
        catch (Exception ex) { Status.Text = "Scan failed: " + ex.Message; }
        finally
        {
            scanCancellation.Dispose(); scanCancellation = null;
            ScanButton.Content = "Scan folder";
            RepoPath.IsEnabled = true;
        }
    }

    private void Finding_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (FindingsGrid.SelectedItem is Finding f) ShowSource(f.RelativePath, f.Line, f.Title + " — " + f.Detail);
    }
    private void Inventory_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (InventoryGrid.SelectedItem is InventoryItem i) ShowSource(i.RelativePath, i.Line, i.Kind + ": " + i.Name);
    }
    private void ShowSource(string relative, int line, string detail)
    {
        if (report is null) return;
        try
        {
            selectedFile = Path.GetFullPath(Path.Combine(report.Root, relative));
            var lines = File.ReadAllLines(selectedFile);
            var first = Math.Max(0, line - 5);
            SourcePreview.Text = string.Join(Environment.NewLine, lines.Skip(first).Take(18)
                .Select((s, index) => $"{(first + index + 1 == line ? ">" : " ")} {first + index + 1,5}  {s}"));
            FindingDetail.Text = $"{relative}:{line}\n{detail}";
        }
        catch (Exception ex) { SourcePreview.Text = "Could not read source: " + ex.Message; }
    }
    private void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        if (selectedFile is null || !File.Exists(selectedFile)) { Status.Text = "Select a source item first."; return; }
        var start = new ProcessStartInfo("notepad.exe") { UseShellExecute = false };
        start.ArgumentList.Add(selectedFile);
        Process.Start(start);
    }
    private void ExportReport_Click(object sender, RoutedEventArgs e)
    {
        if (report is null) { Status.Text = "Scan a repository before exporting."; return; }
        var save = new SaveFileDialog { Title = "Export review report", FileName = "Repository-review.md", Filter = "Markdown report (*.md)|*.md", AddExtension = true, DefaultExt = ".md" };
        if (save.ShowDialog(this) == true)
        {
            File.WriteAllText(save.FileName, report.Markdown());
            Status.Text = "Report exported: " + save.FileName;
        }
    }
    private void NotesFilter_Changed(object sender, TextChangedEventArgs e)
    {
        if (SectionsList is null) return;
        var result = Notes.Search(sections, NotesSearch.Text);
        SectionsList.ItemsSource = result;
        NoteMatch.Text = $"{result.Count} matching sections • searches heading and full text";
        if (result.Count > 0) SectionsList.SelectedIndex = 0;
        else { NoteTitle.Text = "No matching notes"; NoteBody.Clear(); }
    }
    private void Note_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (SectionsList.SelectedItem is not NoteSection section) return;
        NoteTitle.Text = section.Title;
        NoteBody.Text = section.Body;
        NoteBody.ScrollToHome();
        var term = NotesSearch.Text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var index = section.Body.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                NoteBody.Select(index, term.Length);
                NoteBody.ScrollToLine(Math.Max(0, NoteBody.GetLineIndexFromCharacterIndex(index) - 2));
            }
        }
    }
    private void CopyNote_Click(object sender, RoutedEventArgs e)
    {
        if (SectionsList.SelectedItem is NoteSection s) Copy($"# {s.Title}\n\n{s.Body}");
    }
    private void Template_Selected(object sender, SelectionChangedEventArgs e) => RenderTemplate();
    private void TemplateInput_Changed(object sender, TextChangedEventArgs e) => RenderTemplate();
    private void RenderTemplate()
    {
        if (TemplateBody is null || TemplatesList.SelectedItem is not CodeTemplate template) return;
        var ns = NamespaceInput.Text.Trim(); var entity = EntityInput.Text.Trim();
        TemplateTitle.Text = template.Title;
        if (!Templates.ValidInputs(ns, entity))
        {
            TemplateHelp.Text = "Use a valid C# namespace (e.g. School.Api) and singular identifier (e.g. Course). No spaces, keywords or punctuation.";
            TemplateBody.Clear(); return;
        }
        TemplateHelp.Text = template.Help;
        TemplateBody.Text = template.Generate(ns, entity);
        TemplateBody.ScrollToHome();
    }
    private void CopyTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TemplateBody.Text)) Copy(TemplateBody.Text);
    }
    private void Copy(string text)
    {
        try { Clipboard.SetText(text); Status.Text = "Copied to clipboard. Review and adapt before pasting into your project."; }
        catch (Exception ex) { Status.Text = "Clipboard unavailable; select text and press Ctrl+C. " + ex.Message; }
    }
}
