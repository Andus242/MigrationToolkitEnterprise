using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MTE.Core.Models;

namespace MTE.Reporting;

public sealed class MigrationReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull
    };

    public string SaveJson(
        MigrationReport report,
        string migrationRoot)
    {
        if (report is null)
            throw new ArgumentNullException(nameof(report));

        if (string.IsNullOrWhiteSpace(migrationRoot))
            throw new ArgumentException(
                "Migration root is required.",
                nameof(migrationRoot));

        var reportsDirectory =
            Path.Combine(migrationRoot, "Reports");

        Directory.CreateDirectory(reportsDirectory);

        var path =
            Path.Combine(
                reportsDirectory,
                "MigrationReport.json");

        var json =
            JsonSerializer.Serialize(
                report,
                JsonOptions);

        File.WriteAllText(path, json);

        return path;
    }

    public string SaveHtml(
        MigrationReport report,
        string migrationRoot)
    {
        if (report is null)
            throw new ArgumentNullException(nameof(report));

        if (string.IsNullOrWhiteSpace(migrationRoot))
            throw new ArgumentException(
                "Migration root is required.",
                nameof(migrationRoot));

        var reportsDirectory =
            Path.Combine(migrationRoot, "Reports");

        Directory.CreateDirectory(reportsDirectory);

        var path =
            Path.Combine(
                reportsDirectory,
                "MigrationReport.html");

        var completed =
            report.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss")
            ?? "In progress";

        var duration =
            report.CompletedAt.HasValue
                ? report.CompletedAt.Value - report.StartedAt
                : TimeSpan.Zero;

        var successSections =
            report.Sections.Count(section => section.Success);

        var failedSections =
            report.Sections.Count(section => !section.Success);

        var verificationRate =
            report.CopiedFiles > 0
                ? (double)report.VerifiedFiles / report.CopiedFiles * 100
                : 0;

        var copyRate =
            report.TotalFiles > 0
                ? (double)report.CopiedFiles / report.TotalFiles * 100
                : 0;

        var html = $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>MTE Migration Report - {{Escape(report.MigrationId)}}</title>

<style>

* {
    box-sizing: border-box;
}

body {
    margin: 0;
    padding: 32px;
    font-family: "Segoe UI", Arial, sans-serif;
    background: #f1f4f8;
    color: #1f2937;
}

.container {
    max-width: 1400px;
    margin: 0 auto;
}

.header {
    background: #1f2937;
    color: white;
    padding: 30px 34px;
    border-radius: 12px;
    margin-bottom: 24px;
}

.header h1 {
    margin: 0 0 8px 0;
    font-size: 30px;
}

.header p {
    margin: 4px 0;
    color: #d1d5db;
}

.status {
    display: inline-block;
    margin-top: 16px;
    padding: 7px 14px;
    border-radius: 20px;
    background: #374151;
    color: white;
    font-weight: 700;
}

.section {
    background: white;
    border-radius: 12px;
    padding: 24px;
    margin-bottom: 24px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.06);
}

.section h2 {
    margin-top: 0;
    color: #111827;
    border-bottom: 2px solid #e5e7eb;
    padding-bottom: 10px;
}

.info-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 16px;
}

.info-card {
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    padding: 16px;
    background: #fafafa;
}

.info-label {
    font-size: 12px;
    text-transform: uppercase;
    color: #6b7280;
    font-weight: 700;
    margin-bottom: 6px;
}

.info-value {
    font-size: 16px;
    font-weight: 600;
    word-break: break-word;
}

.metrics {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
    gap: 14px;
}

.metric {
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    padding: 18px;
    background: #fafafa;
}

.metric-value {
    font-size: 25px;
    font-weight: 700;
    color: #111827;
}

.metric-label {
    margin-top: 5px;
    color: #6b7280;
    font-size: 13px;
}

.progress-container {
    margin-top: 20px;
}

.progress-label {
    display: flex;
    justify-content: space-between;
    margin-bottom: 6px;
    font-size: 13px;
    font-weight: 600;
}

.progress {
    width: 100%;
    height: 12px;
    background: #e5e7eb;
    border-radius: 8px;
    overflow: hidden;
}

.progress-bar {
    height: 100%;
    background: #2563eb;
    border-radius: 8px;
}

table {
    width: 100%;
    border-collapse: separate;
    border-spacing: 0;
    margin-top: 12px;
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    overflow: hidden;
}

th,
td {
    padding: 11px 13px;
    text-align: left;
    vertical-align: top;
    border-bottom: 1px solid #e5e7eb;
}

th {
    background: #f3f4f6;
    color: #374151;
    font-size: 13px;
    font-weight: 700;
}

tr:last-child td {
    border-bottom: none;
}

td {
    font-size: 13px;
}

.success {
    color: #166534;
    font-weight: 700;
}

.failure {
    color: #991b1b;
    font-weight: 700;
}

.neutral {
    color: #6b7280;
    font-weight: 600;
}

ul {
    margin: 0;
    padding-left: 22px;
}

li {
    margin-bottom: 6px;
}

.path {
    word-break: break-all;
    font-family: Consolas, "Courier New", monospace;
    font-size: 12px;
}

.hash {
    word-break: break-all;
    font-family: Consolas, "Courier New", monospace;
    font-size: 11px;
}

.empty {
    padding: 18px;
    background: #f9fafb;
    border: 1px dashed #d1d5db;
    border-radius: 8px;
    color: #6b7280;
}

.footer {
    text-align: center;
    color: #6b7280;
    font-size: 12px;
    margin-top: 30px;
}

@media print {

    body {
        background: white;
        padding: 0;
    }

    .section,
    .header {
        box-shadow: none;
    }

}

</style>
</head>

<body>

<div class="container">

<div class="header">
    <h1>Migration Toolkit Enterprise</h1>
    <p>Enterprise Migration Report</p>
    <p>Migration ID: {{Escape(report.MigrationId)}}</p>
    <div class="status">
        Status: {{Escape(report.Status)}}
    </div>
</div>

<div class="section">

<h2>Migration Details</h2>

<div class="info-grid">

<div class="info-card">
<div class="info-label">Computer</div>
<div class="info-value">{{Escape(report.ComputerName)}}</div>
</div>

<div class="info-card">
<div class="info-label">Destination</div>
<div class="info-value path">{{Escape(report.Destination)}}</div>
</div>

<div class="info-card">
<div class="info-label">Migration Mode</div>
<div class="info-value">{{Escape(report.MigrationMode)}}</div>
</div>

<div class="info-card">
<div class="info-label">Started</div>
<div class="info-value">{{Escape(report.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"))}}</div>
</div>

<div class="info-card">
<div class="info-label">Completed</div>
<div class="info-value">{{Escape(completed)}}</div>
</div>

<div class="info-card">
<div class="info-label">Duration</div>
<div class="info-value">{{Escape(duration.ToString())}}</div>
</div>

<div class="info-card">
<div class="info-label">Report Version</div>
<div class="info-value">{{Escape(report.ReportVersion)}}</div>
</div>

</div>

</div>

<div class="section">

<h2>Migration Summary</h2>

<div class="metrics">

<div class="metric">
<div class="metric-value">{{report.TotalFiles:N0}}</div>
<div class="metric-label">Total Files</div>
</div>

<div class="metric">
<div class="metric-value">{{FormatBytes(report.TotalBytes)}}</div>
<div class="metric-label">Total Data</div>
</div>

<div class="metric">
<div class="metric-value">{{report.CopiedFiles:N0}}</div>
<div class="metric-label">Copied Files</div>
</div>

<div class="metric">
<div class="metric-value">{{FormatBytes(report.CopiedBytes)}}</div>
<div class="metric-label">Copied Data</div>
</div>

<div class="metric">
<div class="metric-value">{{report.VerifiedFiles:N0}}</div>
<div class="metric-label">Verified Files</div>
</div>

<div class="metric">
<div class="metric-value">{{report.FailedFiles:N0}}</div>
<div class="metric-label">Failed Files</div>
</div>

<div class="metric">
<div class="metric-value">{{report.SkippedFiles:N0}}</div>
<div class="metric-label">Skipped Files</div>
</div>

</div>

<div class="progress-container">

<div class="progress-label">
<span>Files Copied</span>
<span>{{copyRate:F1}}%</span>
</div>

<div class="progress">
<div class="progress-bar" style="width: {{copyRate:F1}}%;"></div>
</div>

</div>

<div class="progress-container">

<div class="progress-label">
<span>Files Verified</span>
<span>{{verificationRate:F1}}%</span>
</div>

<div class="progress">
<div class="progress-bar" style="width: {{verificationRate:F1}}%;"></div>
</div>

</div>

</div>

<div class="section">

<h2>Selected Profiles</h2>

{{BuildListSection(report.SelectedProfiles, "No profiles were recorded.")}}

</div>

<div class="section">

<h2>Selected Folders</h2>

{{BuildListSection(report.SelectedFolders, "No specific folders were recorded.")}}

</div>

<div class="section">

<h2>Migration Sections</h2>

{{BuildSections(report.Sections)}}

</div>

<div class="section">

<h2>File Audit</h2>

{{BuildFileTable(report.Files)}}

</div>

<div class="footer">
Generated by Migration Toolkit Enterprise &bull;
Report Version {{Escape(report.ReportVersion)}}
</div>

</div>

</body>
</html>
""";

        File.WriteAllText(
            path,
            html,
            Encoding.UTF8);

        return path;
    }

    private static string BuildListSection(
        IEnumerable<string> items,
        string emptyMessage)
    {
        var list = items?.ToList() ?? new List<string>();

        if (list.Count == 0)
        {
            return $"<div class=\"empty\">{Escape(emptyMessage)}</div>";
        }

        var builder = new StringBuilder();
        builder.Append("<ul>");

        foreach (var item in list)
        {
            builder.Append("<li>");
            builder.Append(Escape(item));
            builder.Append("</li>");
        }

        builder.Append("</ul>");

        return builder.ToString();
    }

    private static string BuildSections(
        IEnumerable<MigrationSectionResult> sections)
    {
        var list = sections?.ToList()
            ?? new List<MigrationSectionResult>();

        if (list.Count == 0)
        {
            return "<div class=\"empty\">No section results were recorded.</div>";
        }

        var builder = new StringBuilder();

        builder.Append("""
<table>
<tr>
    <th>Section</th>
    <th>Result</th>
    <th>Message</th>
    <th>Timestamp</th>
</tr>
""");

        foreach (var section in list)
        {
            var resultClass =
                section.Success
                    ? "success"
                    : "failure";

            var resultText =
                section.Success
                    ? "Successful"
                    : "Failed";

            builder.Append("<tr>");

            builder.Append("<td>");
            builder.Append(Escape(section.Section));
            builder.Append("</td>");

            builder.Append("<td class=\"");
            builder.Append(resultClass);
            builder.Append("\">");
            builder.Append(resultText);
            builder.Append("</td>");

            builder.Append("<td>");
            builder.Append(Escape(section.Message));
            builder.Append("</td>");

            builder.Append("<td>");
            builder.Append(
                Escape(
                    section.Timestamp
                        .ToString("yyyy-MM-dd HH:mm:ss")));
            builder.Append("</td>");

            builder.Append("</tr>");
        }

        builder.Append("</table>");

        return builder.ToString();
    }

    private static string BuildFileTable(
        IEnumerable<MigrationManifestEntry> files)
    {
        var list = files?.ToList()
            ?? new List<MigrationManifestEntry>();

        if (list.Count == 0)
        {
            return "<div class=\"empty\">No file-level audit entries were recorded.</div>";
        }

        var builder = new StringBuilder();

        builder.Append("""
<table>
<tr>
    <th>Source</th>
    <th>Destination</th>
    <th>Size</th>
    <th>SHA-256</th>
    <th>Copied</th>
    <th>Verified</th>
    <th>Status</th>
</tr>
""");

        foreach (var file in list)
        {
            builder.Append("<tr>");

            builder.Append("<td class=\"path\">");
            builder.Append(Escape(file.SourcePath));
            builder.Append("</td>");

            builder.Append("<td class=\"path\">");
            builder.Append(Escape(file.DestinationPath));
            builder.Append("</td>");

            builder.Append("<td>");
            builder.Append(FormatBytes(file.FileSize));
            builder.Append("</td>");

            builder.Append("<td class=\"hash\">");
            builder.Append(Escape(file.SHA256));
            builder.Append("</td>");

            builder.Append("<td class=\"");
            builder.Append("success");
            builder.Append("\">");
            builder.Append("Yes");
            builder.Append("</td>");

            builder.Append("<td class=\"");
            builder.Append(file.Verified ? "success" : "failure");
            builder.Append("\">");
            builder.Append(file.Verified ? "Yes" : "No");
            builder.Append("</td>");

            builder.Append("<td>");
            builder.Append(Escape(file.VerificationStatus));
            builder.Append("</td>");

            builder.Append("</tr>");
        }

        builder.Append("</table>");

        return builder.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0)
            return "0 B";

        const double unit = 1024;

        if (bytes < unit)
            return $"{bytes:N0} B";

        if (bytes < unit * unit)
            return $"{bytes / unit:N2} KB";

        if (bytes < unit * unit * unit)
            return $"{bytes / (unit * unit):N2} MB";

        if (bytes < unit * unit * unit * unit)
            return $"{bytes / (unit * unit * unit):N2} GB";

        return $"{bytes / (unit * unit * unit * unit):N2} TB";
    }

    private static string Escape(string? value)
    {
        return WebUtility.HtmlEncode(
            value ?? string.Empty);
    }
}

