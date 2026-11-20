using System.Text;
using System.Text.RegularExpressions;
namespace ExamHelper;

public sealed record NoteSection(string Title, string Source, string Body)
{
    public override string ToString() => Title;
}
public static class Notes
{
    public static IReadOnlyList<NoteSection> Parse(string text, string source)
    {
        var sections = new List<NoteSection>();
        string title = source;
        var body = new StringBuilder();
        bool code = false;
        void Flush()
        {
            if (body.ToString().Trim().Length > 0) sections.Add(new(title, source, body.ToString().Trim()));
            body.Clear();
        }
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("```")) code = !code;
            var heading = !code ? Regex.Match(line, @"^#{1,3}\s+(.+)$") : Match.Empty;
            if (heading.Success) { Flush(); title = heading.Groups[1].Value; }
            else if (!line.StartsWith("<a id=")) body.AppendLine(line);
        }
        Flush();
        return sections;
    }
    public static IReadOnlyList<NoteSection> Search(IEnumerable<NoteSection> sections, string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return sections.Where(s => terms.All(t => (s.Title + " " + s.Body + " " + s.Source).Contains(t, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(s => terms.Length > 0 && s.Title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
