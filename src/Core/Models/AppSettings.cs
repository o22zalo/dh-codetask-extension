using System.Collections.Generic;

namespace DhCodetaskExtension.Core.Models
{
    public class AppSettings
    {
        public string GiteaBaseUrl { get; set; } = "http://localhost:3000";
        public string GiteaToken { get; set; } = string.Empty;
        public string GiteaUser { get; set; } = string.Empty;
        public bool GitAutoPush { get; set; } = false;
        public string GitUserName { get; set; } = string.Empty;
        public string GitUserEmail { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string ReportFormat { get; set; } = "json+markdown";
        public string TimeFormat { get; set; } = "hh:mm";
        public string HistoryDefaultView { get; set; } = "week";
        public bool WebhookEnabled { get; set; } = false;
        public string WebhookUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Extensions { get; set; } = new Dictionary<string, string>();
    }
}
