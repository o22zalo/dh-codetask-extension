using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DhCodetaskExtension.Providers.StorageProviders
{
    public sealed class JsonStorageService : IStorageService
    {
        private readonly Func<string> _rootProvider;
        private AppSettings _cachedSettings;

        public JsonStorageService(Func<string> rootDirectoryProvider)
        {
            _rootProvider = rootDirectoryProvider
                ?? throw new ArgumentNullException(nameof(rootDirectoryProvider));
        }

        private string Root            => _rootProvider();
        private string SettingsPath    => Path.Combine(Root, "settings.json");
        private string CurrentTaskPath => Path.Combine(Root, "current-task.json");
        public  string GetHistoryDirectory() => Path.Combine(Root, "history");
        public  string GetSettingsFilePath() => SettingsPath;

        private void EnsureRoot()
        {
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
                AppLogger.Instance.Info("[Storage] Created root directory: " + Root);
            }
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (_cachedSettings != null)
            {
                AppLogger.Instance.Info("[Storage] Returning cached settings.");
                return _cachedSettings;
            }

            EnsureRoot();
            if (!File.Exists(SettingsPath))
            {
                AppLogger.Instance.Warn("[Storage] settings.json not found. Creating default settings.");
                _cachedSettings = new AppSettings();
                await SaveSettingsAsync(_cachedSettings);
                return _cachedSettings;
            }

            try
            {
                var json = await Task.Run(() => File.ReadAllText(SettingsPath, Encoding.UTF8));
                _cachedSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                AppLogger.Instance.Info("[Storage] Loaded settings from: " + SettingsPath);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("JsonStorageService.LoadSettingsAsync", ex);
                _cachedSettings = new AppSettings();
            }
            return _cachedSettings;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            _cachedSettings = settings ?? new AppSettings();
            EnsureRoot();
            var json = JsonConvert.SerializeObject(_cachedSettings, Formatting.Indented);
            await AtomicFile.WriteAllTextAsync(SettingsPath, json);
            AppLogger.Instance.Info("[Storage] Saved settings to: " + SettingsPath);
        }

        public async Task<WorkLog> LoadCurrentTaskAsync()
        {
            if (!File.Exists(CurrentTaskPath))
            {
                AppLogger.Instance.Info("[Storage] No current task snapshot found.");
                return null;
            }

            try
            {
                var json = await Task.Run(() => File.ReadAllText(CurrentTaskPath, Encoding.UTF8));
                var workLog = JsonConvert.DeserializeObject<WorkLog>(json);
                AppLogger.Instance.Info("[Storage] Loaded current task snapshot: " + CurrentTaskPath);
                return workLog;
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("JsonStorageService.LoadCurrentTaskAsync", ex);
                return null;
            }
        }

        public async Task SaveCurrentTaskAsync(WorkLog log)
        {
            EnsureRoot();
            var json = JsonConvert.SerializeObject(log, Formatting.Indented);
            await AtomicFile.WriteAllTextAsync(CurrentTaskPath, json);
            AppLogger.Instance.Info("[Storage] Saved current task snapshot: " + CurrentTaskPath);
        }

        public Task ClearCurrentTaskAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (File.Exists(CurrentTaskPath))
                    {
                        File.Delete(CurrentTaskPath);
                        AppLogger.Instance.Info("[Storage] Deleted current task snapshot: " + CurrentTaskPath);
                    }

                    var bak = CurrentTaskPath + ".bak";
                    if (File.Exists(bak))
                    {
                        File.Delete(bak);
                        AppLogger.Instance.Info("[Storage] Deleted current task backup: " + bak);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Instance.Error("JsonStorageService.ClearCurrentTaskAsync", ex);
                }
            });
        }

        public async Task ArchiveReportAsync(CompletionReport report)
        {
            var histDir = GetHistoryDirectory();
            var year    = report.CompletedAt.Year.ToString("D4");
            var month   = report.CompletedAt.Month.ToString("D2");
            var dir     = Path.Combine(histDir, year, month);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AppLogger.Instance.Info("[Storage] Created history directory: " + dir);
            }

            string slug     = Slugify(report.TaskTitle);
            string stamp    = report.CompletedAt.ToString("yyyyMMdd_HHmmss");
            string baseName = string.Format("{0}_{1}_{2}", stamp, report.TaskId, slug);

            var jsonPath = Path.Combine(dir, baseName + ".json");
            var mdPath   = Path.Combine(dir, baseName + ".md");

            report.SetFilePaths(jsonPath, mdPath);

            var tempJson = JsonConvert.SerializeObject(report, Formatting.Indented);
            var jobjTemp = JObject.Parse(tempJson);
            jobjTemp.Remove("Checksum");
            var canonicalJson = jobjTemp.ToString(Formatting.Indented);
            var checksum = ChecksumHelper.Compute(canonicalJson);
            report.SetChecksum(checksum);
            var finalJson = JsonConvert.SerializeObject(report, Formatting.Indented);

            await AtomicFile.WriteAllTextAsync(jsonPath, finalJson);
            AppLogger.Instance.Info("[Storage] Archived report JSON: " + jsonPath);
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
