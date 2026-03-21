using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;
using Newtonsoft.Json.Linq;

namespace DhCodetaskExtension.Providers.TaskProviders
{
    public sealed class GiteaTaskProvider : ITaskProvider
    {
        private readonly Func<AppSettings> _settingsProvider;
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public GiteaTaskProvider(Func<AppSettings> settingsProvider)
        {
            _settingsProvider = settingsProvider
                ?? throw new ArgumentNullException(nameof(settingsProvider));
        }

        public bool CanHandle(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            var settings = _settingsProvider();
            if (string.IsNullOrEmpty(settings?.GiteaBaseUrl)) return false;
            return url.StartsWith(settings.GiteaBaseUrl, StringComparison.OrdinalIgnoreCase)
                && ParseIssueUrl(url, settings.GiteaBaseUrl, out _, out _, out _);
        }

        public async Task<TaskFetchResult> FetchAsync(string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var settings = _settingsProvider();
                if (!ParseIssueUrl(url, settings.GiteaBaseUrl, out var owner, out var repo, out var number))
                    return TaskFetchResult.Fail("Cannot parse issue URL from Gitea base URL.");

                string apiUrl = $"{settings.GiteaBaseUrl.TrimEnd('/')}/api/v1/repos/{owner}/{repo}/issues/{number}";

                using (var req = new HttpRequestMessage(HttpMethod.Get, apiUrl))
                {
                    if (!string.IsNullOrEmpty(settings.GiteaToken))
                        req.Headers.Authorization =
                            new AuthenticationHeaderValue("token", settings.GiteaToken);

                    var resp = await _http.SendAsync(req, cancellationToken);

                    if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        return TaskFetchResult.Fail("401 Unauthorized — kiểm tra lại Gitea Token.");
                    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return TaskFetchResult.Fail("404 Not Found — issue không tồn tại hoặc URL sai.");

                    resp.EnsureSuccessStatusCode();
                    var json = await resp.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);

                    var labels = new System.Collections.Generic.List<string>();
                    foreach (var lbl in obj["labels"] ?? new JArray())
                        labels.Add(lbl["name"]?.ToString() ?? string.Empty);

                    string desc = obj["body"]?.ToString() ?? string.Empty;
                    if (desc.Length > 500) desc = desc.Substring(0, 497) + "...";

                    var task = new TaskItem
                    {
                        Id = obj["number"]?.ToString() ?? number,
                        Title = obj["title"]?.ToString() ?? string.Empty,
                        Description = StripMarkdown(desc),
                        Labels = labels.ToArray(),
                        Url = obj["html_url"]?.ToString() ?? url,
                        Owner = owner,
                        Repo = repo
                    };
                    return TaskFetchResult.Ok(task);
                }
            }
            catch (OperationCanceledException)
            {
                return TaskFetchResult.Fail("Request timed out.");
            }
            catch (Exception ex)
            {
                return TaskFetchResult.Fail($"Error: {ex.Message}");
            }
        }

        private static bool ParseIssueUrl(string url, string baseUrl, out string owner, out string repo, out string number)
        {
            owner = repo = number = null;
            if (string.IsNullOrEmpty(url)) return false;

            // Pattern: {base}/{owner}/{repo}/issues/{number}
            var escaped = Regex.Escape(baseUrl.TrimEnd('/'));
            var pattern = $@"^{escaped}/([^/]+)/([^/]+)/issues/(\d+)";
            var m = Regex.Match(url, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) return false;
            owner = m.Groups[1].Value;
            repo = m.Groups[2].Value;
            number = m.Groups[3].Value;
            return true;
        }

        private static string StripMarkdown(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = Regex.Replace(s, @"```[\s\S]*?```", string.Empty);
            s = Regex.Replace(s, @"[*_`#>\[\]!]", string.Empty);
            return s.Trim();
        }
    }
}
