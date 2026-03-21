using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.Core.Services;

namespace DhCodetaskExtension.Providers.ReportProviders
{
    public sealed class MarkdownReportGenerator : IReportGenerator
    {
        public async Task<string> GenerateAsync(CompletionReport report, string outputDirectory)
        {
            var year = report.CompletedAt.Year.ToString("D4");
            var month = report.CompletedAt.Month.ToString("D2");
            var dir = Path.Combine(outputDirectory, year, month);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string slug = Slugify(report.TaskTitle);
            string stamp = report.CompletedAt.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine(dir, $"{stamp}_{report.TaskId}_{slug}.md");

            var md = BuildMarkdown(report);
            await AtomicFile.WriteAllTextAsync(path, md);
            return path;
        }

        private static string BuildMarkdown(CompletionReport r)
        {
            var sb = new StringBuilder();
            bool paused = !r.WasPushed && r.CompletedAt != default(DateTime);
            string status = paused ? "⏸ TẠM NGƯNG" : "✅ HOÀN THÀNH";

            sb.AppendLine($"# {status} — #{r.TaskId}: {r.TaskTitle}");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(r.TaskUrl))
                sb.AppendLine($"> **Repo/URL:** {r.TaskUrl}");
            if (r.Labels?.Length > 0)
                sb.AppendLine($"> **Labels:** {string.Join(", ", r.Labels)}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // Time section
            sb.AppendLine("## ⏱ Thời gian");
            sb.AppendLine();
            sb.AppendLine("| Mốc | Thời điểm |");
            sb.AppendLine("| --- | --------- |");
            sb.AppendLine($"| 🟢 Bắt đầu | {r.StartedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} |");
            sb.AppendLine($"| 🔴 Kết thúc | {r.CompletedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} |");
            sb.AppendLine($"| ⏳ Tổng làm việc | {FormatSpan(r.TotalElapsed)} |");
            sb.AppendLine();

            if (r.Sessions?.Count > 0)
            {
                sb.AppendLine("### Chi tiết các phiên làm việc");
                sb.AppendLine();
                sb.AppendLine("| # | Bắt đầu | Kết thúc | Thời lượng |");
                sb.AppendLine("| - | ------- | -------- | ---------- |");
                int idx = 1;
                foreach (var s in r.Sessions)
                {
                    var end = s.EndTime.HasValue
                        ? s.EndTime.Value.ToLocalTime().ToString("HH:mm:ss")
                        : "đang chạy";
                    sb.AppendLine($"| {idx++} | {s.StartTime.ToLocalTime():HH:mm:ss} | {end} | {FormatSpan(s.Duration)} |");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();

            // Work notes
            sb.AppendLine("## 📝 Ghi chú thực hiện");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(r.WorkNotes) ? "_Không có ghi chú._" : r.WorkNotes);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // Business logic
            sb.AppendLine("## 💼 Mô tả nghiệp vụ");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(r.BusinessLogic) ? "_Không có mô tả._" : r.BusinessLogic);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // TODO list
            sb.AppendLine("## ✅ Danh sách TODO");
            sb.AppendLine();
            if (r.Todos?.Count > 0)
            {
                sb.AppendLine("| # | Nội dung | Trạng thái | Thời gian làm |");
                sb.AppendLine("| - | -------- | ---------- | ------------- |");
                int i = 1;
                foreach (var t in r.Todos)
                {
                    string done = t.IsDone ? "✅ Xong" : "⬜ Chưa xong";
                    string elapsed = t.TotalElapsed.TotalSeconds > 0
                        ? FormatSpan(t.TotalElapsed) : "—";
                    sb.AppendLine($"| {i++} | {t.Text} | {done} | {elapsed} |");
                }
                sb.AppendLine();
                sb.AppendLine($"**Tổng TODO:** {r.TodoTotal} | **Hoàn thành:** {r.TodoDone}/{r.TodoTotal} | **Tổng giờ TODO:** {FormatSpan(r.TotalTodoElapsed)}");
            }
            else
            {
                sb.AppendLine("_Không có TODO item._");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // Commit
            sb.AppendLine("## 🔀 Commit");
            sb.AppendLine();
            sb.AppendLine($"```");
            sb.AppendLine(r.CommitMessage ?? string.Empty);
            sb.AppendLine($"```");
            sb.AppendLine();
            sb.AppendLine($"**Branch:** {r.GitBranch ?? "—"}  ");
            string hashLine = string.IsNullOrEmpty(r.GitCommitHash) ? "— chưa push" : r.GitCommitHash;
            string pushedIcon = r.WasPushed ? "✅ Pushed" : "⬜ Not pushed";
            sb.AppendLine($"**Commit Hash:** {hashLine} | {pushedIcon}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine($"_Sinh tự động bởi DevTaskTracker — {r.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}_");

            return sb.ToString();
        }

        private static string FormatSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }

        private static string Slugify(string s)
        {
            if (string.IsNullOrEmpty(s)) return "task";
            s = s.ToLower().Trim();
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-') sb.Append('-');
            }
            return sb.ToString().Trim('-');
        }
    }
}
