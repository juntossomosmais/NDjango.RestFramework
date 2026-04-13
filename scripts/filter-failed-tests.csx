// Usage:
// dotnet test ... | dotnet dotnet-script ./scripts/filter-failed-tests.csx
//
// Reads dotnet test output from stdin and filters it to show only failed tests.
// Each failure is printed with its error message and stack trace. At the end, an
// aggregated summary line shows total passed/failed/skipped counts across all
// test projects.
//
// Exit code: 0 if all tests passed, 1 if any test failed.
//
// Examples:
// # Run all tests
// dotnet test NDjango.Admin.sln | dotnet dotnet-script ./scripts/filter-failed-tests.csx
//
// # Run filtered tests
// dotnet test NDjango.Admin.sln --filter "ForeignKeyTests" | dotnet dotnet-script ./scripts/filter-failed-tests.csx

using System;
using System.IO;
using System.Text.RegularExpressions;

var printing = false;
var failCount = 0;
var testSummary = "";
var totalFailed = 0;
var totalPassed = 0;
var totalSkipped = 0;
var totalTotal = 0;
var projectCount = 0;

var failedTestPattern = new Regex(@"^  Failed .+\[\d");
var passedOrFailedSummary = new Regex(@"^(Passed!|Failed!)");

var stopPrintingPatterns = new Regex[]
{
 new(@"^  Passed "),
 new(@"^Results File:"),
 new(@"^Test Run "),
 new(@"^Total tests:"),
 new(@"^ {4,}Passed: "),
 new(@"^ {4,}Failed: "),
 new(@"^ {4,}Total time: "),
 new(@"^\[xUnit\.net"),
};

string line;
while ((line = Console.ReadLine()) != null)
{
 if (line.StartsWith("Test summary:"))
 {
 testSummary = line;
 continue;
 }

 if (passedOrFailedSummary.IsMatch(line))
 {
 var cleaned = line.Replace(",", "");
 var fields = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
 for (var i = 0; i < fields.Length; i++)
 {
 if (fields[i] == "Failed:" && i + 1 < fields.Length)
 totalFailed += int.TryParse(fields[i + 1], out var v) ? v : 0;
 else if (fields[i] == "Passed:" && i + 1 < fields.Length)
 totalPassed += int.TryParse(fields[i + 1], out var v) ? v : 0;
 else if (fields[i] == "Skipped:" && i + 1 < fields.Length)
 totalSkipped += int.TryParse(fields[i + 1], out var v) ? v : 0;
 else if (fields[i] == "Total:" && i + 1 < fields.Length)
 totalTotal += int.TryParse(fields[i + 1], out var v) ? v : 0;
 }
 projectCount++;
 continue;
 }

 if (failedTestPattern.IsMatch(line))
 {
 if (failCount > 0) Console.WriteLine();
 Console.WriteLine("------------------------------------------------------------");
 failCount++;
 Console.WriteLine($"[FAILED #{failCount}] {line}");
 printing = true;
 continue;
 }

 if (printing)
 {
 var shouldStop = false;
 foreach (var pattern in stopPrintingPatterns)
 {
 if (pattern.IsMatch(line))
 {
 shouldStop = true;
 break;
 }
 }

 if (shouldStop)
 {
 printing = false;
 continue;
 }

 Console.WriteLine(line);
 }
}

Console.WriteLine();
Console.WriteLine("------------------------------------------------------------");

if (testSummary != "")
{
 Console.WriteLine(testSummary);
}
else if (projectCount > 0)
{
 var status = totalFailed > 0 ? "Failed!" : "Passed!";
 Console.WriteLine($"{status} - Failed: {totalFailed,5}, Passed: {totalPassed,5}, Skipped: {totalSkipped,5}, Total: {totalTotal,5}");
}

if (failCount == 0)
{
 Environment.Exit(0);
}
else
{
 Console.WriteLine($"FAILED: {failCount} test(s)");
 Environment.Exit(1);
}
