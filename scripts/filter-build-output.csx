// Usage:
// dotnet build ... | dotnet dotnet-script ./scripts/filter-build-output.csx
//
// Reads dotnet build output from stdin and produces a condensed report:
// - Errors are shown in full
// - Warnings are grouped by warning code with counts
// - A per-project summary table is shown (when available)
// - The original build summary line is preserved
//
// Exit code: 0 if build succeeded, 1 if build failed.
//
// Examples:
// # Via docker (recommended — saves to temp file first to avoid dotnet concurrency issues):
// docker compose run --volume "$(PWD):/app" --rm --remove-orphans integration-tests bash -c \
// 'dotnet build NDjango.Admin.sln > /tmp/build-output.txt 2>&1; cat /tmp/build-output.txt | dotnet dotnet-script ./scripts/filter-build-output.csx'
//
// ──────────────────────────────────────────────────────────────────
// TWO OUTPUT FORMATS
// ──────────────────────────────────────────────────────────────────
//
// `dotnet build` can produce two distinct output formats depending on
// terminal capabilities and SDK version. This script handles both:
//
// FORMAT 1 — Terminal UI (when stdout is a TTY or using `dotnet` CLI rich output)
// - Project header lines:  "  ProjectName net8.0 succeeded with N warning(s) (Xs) → path"
// - Warnings indented: " /path/file.cs(L,C): warning CS8625: message"
// - MSB multi-line: " /path: warning MSB3277:" then continuation lines with 6+ space indent
// - Summary line: "Build succeeded with 1081 warning(s) in 30.8s"
// - Restore line: "Restore complete (9.5s)"
//
// FORMAT 2 — MSBuild (when stdout is piped / redirected, typical in CI and docker)
// - Warnings at column 0:  "/path/file.cs(L,C): warning CS8625: message [/path/project.csproj]"
// - MSB multi-line: Each continuation is its own "warning MSB3277:" line (same source location)
// - "Build succeeded." appears mid-stream (between compile + summary passes)
// - Summary lines at end:  " 1081 Warning(s)" / " 0 Error(s)" / "Time Elapsed 00:00:18.84"
// - Restore lines: "  Determining projects to restore..." / "  Restored ..."
// - Warnings may be emitted TWICE (once per MSBuild pass) — the script deduplicates them
// using (source_location, code, message) as the identity key.
//
// DEDUPLICATION STRATEGY:
// - ALL warnings are deduplicated by (source_location, code, message_stripped_of_project).
// This handles both MSB continuation lines and Format 2's double-emission of CS warnings.
// - The Format 2 summary ("N Warning(s)" / "N Error(s)") is authoritative and is preferred
// over the mid-stream "Build succeeded." line when both are present.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// --- Patterns ---

// Format 1: project summary line
var projectLinePattern = new Regex(@"^\s{2}(\S+)\s+\S+\s+(succeeded|failed)(?:\s+with\s+(\d+)\s+(warning|error)\(s\))?\s+\(([^)]+)\)");

// Warning line — works for both formats:
// Format 1: " /path: warning CS8625: message"
// Format 2: "/path: warning CS8625: message [/project.csproj]"
// Captures: (1)=source location, (2)=code, (3)=message (may include trailing [project])
var warningPattern = new Regex(@"^(\s*/[^:]+\(\d+,\d+\)):\s+warning\s+(\w+\d+):\s*(.*)$");

// Error line — works for both formats
var errorPattern = new Regex(@"^\s*(/[^:]+\(\d+,\d+\):\s+error\s+\w+\d+:\s+.+)$");

// Format 1: multi-line warning continuation (6+ space indent)
var continuationPattern = new Regex(@"^\s{6}");

// "Build succeeded/failed" — appears in both formats (mid-stream in Format 2)
var buildSummaryPattern = new Regex(@"^Build (succeeded|failed)");

// Format 2 summary (at end of output): " 1081 Warning(s)" / " 5 Error(s)"
var warningCountFormat2 = new Regex(@"^\s+(\d+)\s+Warning\(s\)");
var errorCountFormat2 = new Regex(@"^\s+(\d+)\s+Error\(s\)");
var timeElapsedFormat2 = new Regex(@"^Time Elapsed\s+(.+)$");

// Restore lines (both formats — skip them)
var restorePattern = new Regex(@"^(Restore complete|\s+Determining projects|\s+Restored |\s+\d+ of \d+ projects are up-to-date|\s+All projects are up-to-date)");

// --- State ---

var projects = new List<(string Name, string Status, int Count, string Kind, string Time)>();
var warningsByCode = new Dictionary<string, (string Message, int Count)>();
var seenWarnings = new HashSet<string>(); // dedup key: "sourceLocation|code|message"
var errorLines = new List<string>();
var seenErrors = new HashSet<string>(); // dedup for Format 2 double-emission
var buildSummaryLine = "";
var hasBuildFailed = false;
var insideMultiLineWarning = false;
var multiLineWarningCode = "";

// Format 2 summary parts
var format2WarningCount = -1;
var format2ErrorCount = -1;
var format2TimeElapsed = "";

string line;
while ((line = Console.ReadLine()) != null)
{
 // Format 1: multi-line warning continuation (e.g., MSB3277 details)
 if (insideMultiLineWarning)
 {
 if (continuationPattern.IsMatch(line))
 {
 // Use the first continuation line as the message if we only have the placeholder
 if (warningsByCode.TryGetValue(multiLineWarningCode, out var cur)
 && cur.Message.StartsWith("(multi-line"))
 {
 warningsByCode[multiLineWarningCode] = (line.Trim(), cur.Count);
 }
 continue;
 }
 insideMultiLineWarning = false;
 }

 if (restorePattern.IsMatch(line))
 continue;

 // Project summary line (Format 1 only)
 var projectMatch = projectLinePattern.Match(line);
 if (projectMatch.Success)
 {
 var name = projectMatch.Groups[1].Value;
 var status = projectMatch.Groups[2].Value;
 var count = projectMatch.Groups[3].Success ? int.Parse(projectMatch.Groups[3].Value) : 0;
 var kind = projectMatch.Groups[4].Success ? projectMatch.Groups[4].Value : "";
 var time = projectMatch.Groups[5].Value;
 projects.Add((name, status, count, kind, time));

 if (status == "failed")
 hasBuildFailed = true;

 continue;
 }

 // Warning line (both formats)
 var warningMatch = warningPattern.Match(line);
 if (warningMatch.Success)
 {
 var sourceLocation = warningMatch.Groups[1].Value.Trim();
 var code = warningMatch.Groups[2].Value;
 var rawMessage = warningMatch.Groups[3].Value.Trim();

 // Strip trailing [/project.csproj] from Format 2
 var message = Regex.Replace(rawMessage, @"\s*\[/[^\]]+\]\s*$", "").Trim();

 // Deduplicate: Format 2 emits each warning twice (two MSBuild passes), and MSB
 // warnings emit multiple continuation lines with the same source location.
 // For MSB warnings: dedup by (source, code) only — each continuation line has a
 // different message but represents the same single warning.
 // For CS warnings: dedup by (source, code, message) — same location can have
 // different messages (e.g., CS8618 for different properties at the same constructor).
 var isMsb = code.StartsWith("MSB");
 var dedupKey = isMsb ? $"{sourceLocation}|{code}" : $"{sourceLocation}|{code}|{message}";
 if (!seenWarnings.Add(dedupKey))
 {
 // Already seen — but update the display message if current one is better
 if (warningsByCode.TryGetValue(code, out var cur)
 && (cur.Message.StartsWith("(multi-line") || string.IsNullOrEmpty(cur.Message))
 && !string.IsNullOrEmpty(message))
 {
 warningsByCode[code] = (message, cur.Count);
 }
 continue;
 }

 // Format 1: MSB warning with empty message starts multi-line block
 if (isMsb && string.IsNullOrEmpty(message))
 {
 insideMultiLineWarning = true;
 multiLineWarningCode = code;
 }

 if (warningsByCode.TryGetValue(code, out var existing))
 warningsByCode[code] = (existing.Message, existing.Count + 1);
 else
 warningsByCode[code] = (string.IsNullOrEmpty(message) ? $"(multi-line {code} warning)" : message, 1);

 continue;
 }

 // Error line (both formats)
 var errorMatch = errorPattern.Match(line);
 if (errorMatch.Success)
 {
 var rawError = errorMatch.Groups[1].Value.Trim();
 // Strip trailing [/project.csproj] from Format 2
 var cleanError = Regex.Replace(rawError, @"\s*\[/[^\]]+\]\s*$", "").Trim();
 if (seenErrors.Add(cleanError))
 errorLines.Add(cleanError);
 hasBuildFailed = true;
 continue;
 }

 // "Build succeeded/failed" — keep updating (Format 2 may have this mid-stream)
 if (buildSummaryPattern.IsMatch(line))
 {
 buildSummaryLine = line;
 if (line.Contains("failed"))
 hasBuildFailed = true;
 continue;
 }

 // Format 2 summary parts (at end of output — these are authoritative)
 var warnCountMatch = warningCountFormat2.Match(line);
 if (warnCountMatch.Success)
 {
 format2WarningCount = int.Parse(warnCountMatch.Groups[1].Value);
 continue;
 }

 var errCountMatch = errorCountFormat2.Match(line);
 if (errCountMatch.Success)
 {
 format2ErrorCount = int.Parse(errCountMatch.Groups[1].Value);
 if (format2ErrorCount > 0)
 hasBuildFailed = true;
 continue;
 }

 var timeMatch = timeElapsedFormat2.Match(line);
 if (timeMatch.Success)
 {
 format2TimeElapsed = timeMatch.Groups[1].Value;
 continue;
 }
}

// When Format 2 summary is present, it is authoritative — override any mid-stream "Build succeeded."
if (format2ErrorCount >= 0)
{
 var status = format2ErrorCount > 0 ? "failed" : "succeeded";
 var parts = new List<string>();
 if (format2ErrorCount > 0) parts.Add($"{format2ErrorCount} error(s)");
 if (format2WarningCount > 0) parts.Add($"{format2WarningCount} warning(s)");
 var detail = parts.Count > 0 ? $" with {string.Join(" and ", parts)}" : "";
 var time = !string.IsNullOrEmpty(format2TimeElapsed) ? $" in {format2TimeElapsed}" : "";
 buildSummaryLine = $"Build {status}{detail}{time}";
}

// --- Output ---

var separator = new string('=', 70);
var thinSeparator = new string('-', 70);

// Errors section
if (errorLines.Count > 0)
{
 Console.WriteLine();
 Console.WriteLine(separator);
 Console.WriteLine($"  ERRORS ({errorLines.Count})");
 Console.WriteLine(separator);
 Console.WriteLine();

 foreach (var err in errorLines)
 {
 Console.WriteLine($"  {err}");
 }

 Console.WriteLine();
}

// Warnings section
if (warningsByCode.Count > 0)
{
 var totalWarnings = warningsByCode.Values.Sum(w => w.Count);

 Console.WriteLine();
 Console.WriteLine(separator);
 Console.WriteLine($"  WARNINGS — {totalWarnings} total, {warningsByCode.Count} distinct codes");
 Console.WriteLine(separator);
 Console.WriteLine();

 foreach (var kv in warningsByCode.OrderByDescending(x => x.Value.Count))
 {
 var truncatedMessage = kv.Value.Message.Length > 80
 ? kv.Value.Message.Substring(0, 77) + "..."
 : kv.Value.Message;
 Console.WriteLine($"  {kv.Key,-10} {kv.Value.Count,5}x {truncatedMessage}");
 }

 Console.WriteLine();
}

// Per-project summary (Format 1 only)
if (projects.Count > 0)
{
 Console.WriteLine(thinSeparator);
 Console.WriteLine("  Project Summary");
 Console.WriteLine(thinSeparator);
 Console.WriteLine();

 var maxNameLen = projects.Max(p => p.Name.Length);

 foreach (var p in projects)
 {
 var icon = p.Status == "failed" ? "X" : "ok";
 var detail = p.Count > 0 ? $"{p.Count} {p.Kind}(s)" : "";
 Console.WriteLine($"  [{icon,-2}] {p.Name.PadRight(maxNameLen)} {p.Time,7} {detail}");
 }

 Console.WriteLine();
}

// Final summary
Console.WriteLine(separator);
if (!string.IsNullOrEmpty(buildSummaryLine))
 Console.WriteLine(buildSummaryLine);
else if (hasBuildFailed)
 Console.WriteLine("Build failed.");
else
 Console.WriteLine("Build succeeded.");
Console.WriteLine(separator);

Environment.Exit(hasBuildFailed ? 1 : 0);
