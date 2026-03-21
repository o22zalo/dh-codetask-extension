using System.IO;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.Core.Services;
using Newtonsoft.Json;

namespace DhCodetaskExtension.Providers.ReportProviders
{
    public sealed class JsonReportGenerator : IReportGenerator
    {
        public async Task<string> GenerateAsync(CompletionReport report, string outputDirectory)
        {
            var year = report.CompletedAt.Year.ToString("D4");
            var month = report.CompletedAt.Month.ToString("D2");
            var dir = Path.Combine(outputDirectory, year, month);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string slug = Slugify(report.TaskTitle);
            string stamp = report.CompletedAt.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine(dir, $"{stamp}_{report.TaskId}_{slug}.json");

            var json = JsonConvert.SerializeObject(report, Formatting.Indented);
            await AtomicFile.WriteAllTextAsync(path, json);
            return path;
        }

        private static string Slugify(string s)
        {
            if (string.IsNullOrEmpty(s)) return "task";
            s = s.ToLower().Trim();
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-') sb.Append('-');
            }
            return sb.ToString().Trim('-');
        }
    }
}
