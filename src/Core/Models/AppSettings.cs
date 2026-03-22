using System.Collections.Generic;
using Newtonsoft.Json;

namespace DhCodetaskExtension.Core.Models
{
    public class AppSettings
    {
        // ── Gitea ────────────────────────────────────────────────────────
        public string GiteaBaseUrl  { get; set; } = "http://localhost:3000";
        public string GiteaToken    { get; set; } = string.Empty;
        public string GiteaUser     { get; set; } = string.Empty;

        // ── Git CLI ──────────────────────────────────────────────────────
        public bool   GitAutoPush   { get; set; } = false;
        public string GitUserName   { get; set; } = string.Empty;
        public string GitUserEmail  { get; set; } = string.Empty;

        // ── Storage & Report ─────────────────────────────────────────────
        public string StoragePath   { get; set; } = string.Empty;
        public string ReportFormat  { get; set; } = "json+markdown";
        public string TimeFormat    { get; set; } = "hh:mm";

        // ── History ──────────────────────────────────────────────────────
        public string HistoryDefaultView { get; set; } = "week";

        // ── Webhook ──────────────────────────────────────────────────────
        public bool   WebhookEnabled { get; set; } = false;
        public string WebhookUrl     { get; set; } = string.Empty;

        // ── Solution File Scanner (v3.3) ─────────────────────────────────
        public string DirectoryRootDhHosCodePath { get; set; } = string.Empty;
        public int SolutionFileCacheMinutes { get; set; } = 20;

        // ── Ripgrep (v3.5) ───────────────────────────────────────────────
        public string RipgrepPath { get; set; } = string.Empty;

        // ── Task Pause Reasons (v3.3) ────────────────────────────────────
        // ObjectCreationHandling.Replace: prevents Newtonsoft.Json from appending
        // to the default list on deserialization, which caused duplicates each
        // time the settings dialog was opened and saved.
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<string> TaskPauseReasons { get; set; } = new List<string>
        {
            "Hết giờ làm việc",
            "Chuyển việc khác",
            "Lý do khác"
        };

        // ── TODO Templates (v3.3) ────────────────────────────────────────
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<string> TodoTemplates { get; set; } = new List<string>();

        // ── Extensions (freeform key/value) ──────────────────────────────
        public Dictionary<string, string> Extensions { get; set; } = new Dictionary<string, string>();
    }
}
