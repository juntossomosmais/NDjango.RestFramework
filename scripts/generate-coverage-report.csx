using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Usage:
// dotnet test ... | dotnet dotnet-script ./scripts/generate-coverage-report.csx [filter]
// docker compose run ... | dotnet dotnet-script ./scripts/generate-coverage-report.csx [filter]
//
// Reads test output from stdin, extracts coverage.cobertura.xml paths from the
// "Attachments:" section, merges all Cobertura files, and prints a per-file
// coverage report followed by per-file details (uncovered lines, partial branches).
//
// The optional [filter] argument does a case-insensitive substring match on the
// source file path. If multiple files match, all are included in the report.
//
// Examples:
// # Full report for all files
// dotnet test NDjango.Admin.sln --settings "./runsettings.xml" | dotnet dotnet-script ./scripts/generate-coverage-report.csx
//
// # Filter to a specific file
// dotnet test NDjango.Admin.sln --settings "./runsettings.xml" | dotnet dotnet-script ./scripts/generate-coverage-report.csx -- "Utils.cs"
//
// # Filter matches multiple files (e.g., all Dispatchers)
// dotnet test NDjango.Admin.sln --settings "./runsettings.xml" | dotnet dotnet-script ./scripts/generate-coverage-report.csx -- "Dispatcher"

var filter = Args.Count > 0 ? Args[0] : "";

// ── 1. Capture stdin and extract Cobertura XML paths ─────────────────────────
var input = Console.In.ReadToEnd();

var coberturaRegex = new Regex(@"[^ ]*coverage\.cobertura\.xml");
var coberturaFiles = coberturaRegex.Matches(input)
 .Select(m => m.Value.Trim())
 .Where(File.Exists)
 .ToList();

if (coberturaFiles.Count == 0)
{
 Console.WriteLine();
 Console.WriteLine("============================================================");
 Console.WriteLine("  COVERAGE: No coverage.cobertura.xml files found.");
 Console.WriteLine("  Make sure you run with: --settings \"./runsettings.xml\"");
 Console.WriteLine("============================================================");
 Environment.Exit(0);
}

// ── 2. Parse Cobertura XML files and produce the report ──────────────────────
// Per-line data: key=(file, lineNumber) -> max hits
var lineHits = new Dictionary<(string file, int line), int>();
// Branch data: key=(file, lineNumber) -> (covered, valid)
var branchData = new Dictionary<(string file, int line), (int covered, int valid)>();
// Track file order
var fileSet = new HashSet<string>();
var fileOrder = new List<string>();

string NormalizePath(string sourcePath, string filename)
{
 var full = sourcePath;
 if (full.Length > 0 && full[^1] != '/') full += "/";
 full += filename;

 var srcIdx = full.IndexOf("src/", StringComparison.Ordinal);
 if (srcIdx >= 0) full = full[(srcIdx + 4)..];

 return full;
}

foreach (var xmlPath in coberturaFiles)
{
 var doc = XDocument.Load(xmlPath);
 var packages = doc.Descendants("package");

 foreach (var package in packages)
 {
 // Each package may have its own <source> via the parent <sources> element,
 // but Cobertura typically has one <sources> at the top level.
 // We get source from the top-level <sources>/<source>.
 var sourcePath = doc.Descendants("source").FirstOrDefault()?.Value ?? "";

 foreach (var cls in package.Descendants("class"))
 {
 var filename = cls.Attribute("filename")?.Value ?? "";
 if (string.IsNullOrEmpty(filename)) continue;

 var canonicalPath = NormalizePath(sourcePath, filename);

 // Case-insensitive filter
 if (!string.IsNullOrEmpty(filter) &&
 !canonicalPath.Contains(filter, StringComparison.OrdinalIgnoreCase))
 continue;

 foreach (var line in cls.Descendants("line"))
 {
 var number = int.Parse(line.Attribute("number")?.Value ?? "0");
 var hits = int.Parse(line.Attribute("hits")?.Value ?? "0");

 var key = (canonicalPath, number);
 if (lineHits.TryGetValue(key, out var existing))
 {
 if (hits > existing) lineHits[key] = hits;
 }
 else
 {
 lineHits[key] = hits;
 }

 // Branch coverage: condition-coverage="50% (1/2)"
 var cc = line.Attribute("condition-coverage")?.Value;
 if (cc != null)
 {
 var parenMatch = Regex.Match(cc, @"\((\d+)/(\d+)\)");
 if (parenMatch.Success)
 {
 var bc = int.Parse(parenMatch.Groups[1].Value);
 var bv = int.Parse(parenMatch.Groups[2].Value);

 if (branchData.TryGetValue(key, out var existingBranch))
 {
 branchData[key] = (
 Math.Max(bc, existingBranch.covered),
 Math.Max(bv, existingBranch.valid)
 );
 }
 else
 {
 branchData[key] = (bc, bv);
 }
 }
 }

 if (fileSet.Add(canonicalPath))
 fileOrder.Add(canonicalPath);
 }
 }
 }
}

if (fileOrder.Count == 0)
{
 if (!string.IsNullOrEmpty(filter))
 {
 Console.WriteLine();
 Console.WriteLine("============================================================");
 Console.WriteLine($"  COVERAGE: No files matching \"{filter}\"");
 Console.WriteLine("============================================================");
 }
 Environment.Exit(0);
}

// Roll up per-line data into per-file totals
var fileLv = new Dictionary<string, int>();
var fileLc = new Dictionary<string, int>();
var fileBv = new Dictionary<string, int>();
var fileBc = new Dictionary<string, int>();

foreach (var kvp in lineHits)
{
 var f = kvp.Key.file;
 fileLv[f] = fileLv.GetValueOrDefault(f) + 1;
 if (kvp.Value > 0)
 fileLc[f] = fileLc.GetValueOrDefault(f) + 1;
}

foreach (var kvp in branchData)
{
 var f = kvp.Key.file;
 fileBc[f] = fileBc.GetValueOrDefault(f) + kvp.Value.covered;
 fileBv[f] = fileBv.GetValueOrDefault(f) + kvp.Value.valid;
}

// Sort files by path
fileOrder.Sort(StringComparer.OrdinalIgnoreCase);

int totalLv = 0, totalLc = 0, totalBv = 0, totalBc = 0;

Console.WriteLine();
Console.WriteLine("============================================================");
Console.WriteLine(" CODE COVERAGE REPORT ");
Console.WriteLine("============================================================");

if (!string.IsNullOrEmpty(filter))
{
 Console.WriteLine($"  Filter: {filter}");
 Console.WriteLine("------------------------------------------------------------");
}

Console.WriteLine();
Console.WriteLine($"{"File",-80}  {"Lines",7}  {"Branch",7}  {"Covered",7}");
Console.WriteLine($"{"----",-80}  {"-----",7}  {"------",7}  {"-------",7}");

foreach (var f in fileOrder)
{
 var lv = fileLv.GetValueOrDefault(f);
 var lc = fileLc.GetValueOrDefault(f);
 var bv = fileBv.GetValueOrDefault(f);
 var bc = fileBc.GetValueOrDefault(f);

 totalLv += lv; totalLc += lc;
 totalBv += bv; totalBc += bc;

 var linePct = lv > 0 ? (double)lc / lv * 100 : 0;
 var branchPct = bv > 0 ? (double)bc / bv * 100 : -1;
 var coveredStr = $"{lc}/{lv}";

 if (branchPct >= 0)
 Console.WriteLine($"{f,-80}  {linePct,6:F1}%  {branchPct,6:F1}%  {coveredStr}");
 else
 Console.WriteLine($"{f,-80}  {linePct,6:F1}%  {"-",7}  {coveredStr}");
}

Console.WriteLine();
Console.WriteLine("------------------------------------------------------------");

var totalLinePct = totalLv > 0 ? (double)totalLc / totalLv * 100 : 0;
var totalBranchPct = totalBv > 0 ? (double)totalBc / totalBv * 100 : -1;

if (totalBranchPct >= 0)
 Console.WriteLine($"{"TOTAL",-80}  {totalLinePct,6:F1}%  {totalBranchPct,6:F1}%  {totalLc}/{totalLv}");
else
 Console.WriteLine($"{"TOTAL",-80}  {totalLinePct,6:F1}%  {"-",7}  {totalLc}/{totalLv}");

Console.WriteLine("============================================================");
var summary = $"  Files: {fileOrder.Count} | Lines: {totalLc}/{totalLv} ({totalLinePct:F1}%)";
if (totalBranchPct >= 0)
 summary += $" | Branches: {totalBc}/{totalBv} ({totalBranchPct:F1}%)";
Console.WriteLine(summary);
Console.WriteLine("============================================================");

// ── 3. Per-file detail: uncovered lines and partial branches ─────────────────
foreach (var f in fileOrder)
{
 var uncoveredLines = lineHits
 .Where(kvp => kvp.Key.file == f && kvp.Value == 0)
 .Select(kvp => kvp.Key.line)
 .OrderBy(l => l)
 .ToList();

 var partialBranches = branchData
 .Where(kvp => kvp.Key.file == f && kvp.Value.covered < kvp.Value.valid)
 .Select(kvp => (line: kvp.Key.line, covered: kvp.Value.covered, valid: kvp.Value.valid))
 .OrderBy(x => x.line)
 .ToList();

 if (uncoveredLines.Count == 0 && partialBranches.Count == 0)
 continue;

 Console.WriteLine();
 Console.WriteLine(f);

 if (uncoveredLines.Count > 0)
 Console.WriteLine($"  Uncovered lines: {string.Join(", ", uncoveredLines)}");

 if (partialBranches.Count > 0)
 {
 Console.WriteLine("  Partial branches:");
 foreach (var pb in partialBranches)
 Console.WriteLine($" Line {pb.line}: {pb.covered}/{pb.valid} conditions covered");
 }
}
