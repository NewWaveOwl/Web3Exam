# Web 3 Exam Helper

Portable C# / WPF helper for inspecting unfamiliar ASP.NET Core repositories and reviewing course patterns offline.

## Start

Double-click **`Finaltest\Web3ExamHelper.exe`**. This is the standalone Windows x64 build: you can copy that one file to another Windows x64 PC. No .NET installation, internet, account, or API key is needed to run it. First startup may take a little longer while bundled native libraries extract to the normal .NET temporary cache.

The source project is in this `ExamHelper` folder. The two Markdown guides are embedded at build time, so the executable does not need the Notes folders or external files.

## Use

1. **Repository scanner:** Browse to the new repository root, then Scan folder. Select a finding for its explanation and numbered source context. Inventory lists project frameworks/packages, type declarations, DI registrations, and route/verb attributes. Open source opens the selected file in Notepad. Export report saves Markdown to the location you choose.
2. **Offline notes:** search a heading or words from the full section. Try `REVIEW ANSWER 6`, `DTO`, `Swagger`, `factory`, or `SaveChanges`. The general guide and Library practice reference are labeled separately. Copy section copies the text as Markdown.
3. **Code templates:** set your namespace and singular entity name, select one of 14 patterns, then Copy template. DTO/service/controller patterns fit together around an example int Id and string Name. Replace AppDbContext, limits, routes, fields, validation, scope names and business checks to match the real starter. Templates include snippets/design recipes, not only complete standalone classes.
4. **Exam checklist:** session-only checkboxes for requirements and verification. State resets when the app closes.

The app does not execute dotnet commands or apply templates to your repository. Commands are copyable references. Scanning reads only C# and project files; it does not run project code or send anything over the network. Export is the only report-writing action, and clipboard copying is explicit.

## Scanner limits

Findings are heuristic review prompts, not a compiler, semantic analyzer, proof of a bug, or an exam score. Inspect the containing method and actual sheet before changing anything. The scanner masks ordinary comments but does not fully parse every modern C# syntax form or trace data flow. Route inventory lists attribute/minimal-API declarations; it does not calculate all effective combined/inherited routes.

It skips bin, obj, .git, .vs, node_modules and other common generated directories; does not follow reparse-point directories/files; caps source/project files at 5,000 and each file at 2 MB; reports access/limit warnings. It looks for controller persistence access/commits, repository saves/public IQueryable, EnsureCreated, broad update patterns, materialization followed by nearby LINQ, AsEnumerable, paging checks, suspicious singleton registrations, interpolated logging and reversed auth middleware nearby. These checks can have false positives and false negatives.

## Build / publish

The helper targets .NET 8 WPF. Its target framework is independent of the .NET version in the repository you inspect. Build machine needs a suitable .NET SDK and WindowsDesktop targeting pack. This checkout pins installed 8.0.26 targeting/runtime packs in Directory.Build.targets for reproducible offline packaging. NuGet.Config clears external sources because this app has no third-party package references. On another build machine, install the matching targeting/runtime packs or deliberately update those pins/configuration; the supplied EXE needs neither SDK nor restore.

From this folder:

```powershell
dotnet build ExamHelper.csproj -c Release
dotnet run --project Tests/HelperChecks.csproj -c Release
dotnet publish ExamHelper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableSingleFileAnalyzer=false -p:EnableTrimAnalyzer=false -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o Standalone
```

Single-file/trim analyzers are disabled for offline packaging; code is not trimmed. No external libraries beyond the bundled Microsoft desktop runtime.

## Verification

- Release WPF build with zero errors/warnings.
- 31 checks: scanner detection/line numbers/comment exclusions, skipped generated folders, no source mutation, report generation, cancellation, fenced note parsing, search, identifier validation, and all 14 template generation paths.
- Read-only scan of the provided Example: 20 source/project files and 17 review prompts. Example-scan.md contains that report.
- Interactive Windows checks: standalone startup, source scan/context, search for review answer 6, namespace/entity substitution in a service template, and copy-to-clipboard status.

The helper does not certify that an adapted project will compile or satisfy the exam rubric; use your actual build/tests and requirements.
