using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Events;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;
using Newtonsoft.Json;

namespace DhCodetaskExtension.Providers.NotificationProviders
{
    public sealed class WebhookNotificationProvider : INotificationProvider
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private readonly Func<AppSettings> _settingsProvider;

        public WebhookNotificationProvider(Func<AppSettings> settingsProvider)
        {
            _settingsProvider = settingsProvider
                ?? throw new ArgumentNullException(nameof(settingsProvider));
        }

        public void OnEvent<TEvent>(TEvent eventArgs) where TEvent : EventArgs
        {
            if (eventArgs is TaskCompletedEvent completed)
                _ = PostWithRetryAsync(BuildPayload(completed));
        }

        private object BuildPayload(TaskCompletedEvent e)
        {
            var r = e.Report;
            return new
            {
                @event = "task.completed",
                timestamp = r.CompletedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                task = new { id = r.TaskId, title = r.TaskTitle, url = r.TaskUrl },
                elapsed_seconds = (long)r.TotalElapsed.TotalSeconds,
                elapsed_display = FormatSpan(r.TotalElapsed),
                started_at = r.StartedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                completed_at = r.CompletedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                todos = new
                {
                    total = r.TodoTotal,
                    done = r.TodoDone,
                    elapsed_seconds = (long)r.TotalTodoElapsed.TotalSeconds
                },
                commit = new
                {
                    message = r.CommitMessage,
                    hash = r.GitCommitHash,
                    pushed = r.WasPushed
                },
                report_file = r.MarkdownFilePath
            };
        }

        private async Task PostWithRetryAsync(object payload)
        {
            var settings = _settingsProvider();
            if (!settings.WebhookEnabled || string.IsNullOrEmpty(settings.WebhookUrl)) return;

            var json = JsonConvert.SerializeObject(payload);
            int[] delays = { 1000, 2000, 4000 };

            for (int i = 0; i <= delays.Length; i++)
            {
                try
                {
                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        var resp = await _http.PostAsync(settings.WebhookUrl, content);
                        if (resp.IsSuccessStatusCode) return;
                    }
                }
                catch { }

                if (i < delays.Length)
                    await Task.Delay(delays[i]);
            }
            // Fail silently — never block UI
        }

        private static string FormatSpan(TimeSpan ts) =>
            $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
    }
}
