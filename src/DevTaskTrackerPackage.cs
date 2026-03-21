using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Events;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.Core.Services;
using DhCodetaskExtension.Providers.GitProviders;
using DhCodetaskExtension.Providers.NotificationProviders;
using DhCodetaskExtension.Providers.ReportProviders;
using DhCodetaskExtension.Providers.StorageProviders;
using DhCodetaskExtension.Providers.TaskProviders;
using DhCodetaskExtension.Services;
using DhCodetaskExtension.ToolWindows;
using DhCodetaskExtension.ViewModels;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(
        "DH Codetask Extension",
        "DevTask Tracker — Gitea, Time Tracking, TODO, Reports.", "3.0")]
    [ProvideToolWindow(typeof(TrackerToolWindow),
        Style = VsDockStyle.Tabbed, DockedWidth = 360,
        Window = "DocumentWell", Orientation = ToolWindowOrientation.Left)]
    [ProvideToolWindow(typeof(HistoryToolWindow),
        Style = VsDockStyle.Tabbed, DockedWidth = 500,
        Window = "DocumentWell", Orientation = ToolWindowOrientation.Right)]
    [Guid(PackageGuids.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(SampleOptionsPage),
        "DH Codetask Extension", "General", 0, 0, supportsAutomation: true)]
    public sealed class DevTaskTrackerPackage : AsyncPackage
    {
        public OutputWindowService OutputWindow { get; private set; }
        public StatusBarService    StatusBar    { get; private set; }
        public AppSettings         Settings     { get; set; }

        private EventBus               _eventBus;
        private JsonStorageService     _storage;
        private HistoryQueryService    _historyRepo;
        private GitService             _gitService;
        private TaskProviderFactory    _taskFactory;
        private CompositeReportGenerator _reportGen;
        private WebhookNotificationProvider _webhook;
        private TimeTrackingService    _taskTimer;
        private TrackerViewModel       _trackerVm;
        private HistoryViewModel       _historyVm;

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            OutputWindow = new OutputWindowService(this);
            StatusBar    = new StatusBarService(this);
            await OutputWindow.InitializeAsync();
            await StatusBar.InitializeAsync();

            _storage = new JsonStorageService(GetStorageRoot);
            Settings = await _storage.LoadSettingsAsync();

            _eventBus    = new EventBus();
            _historyRepo = new HistoryQueryService(() => Path.Combine(GetStorageRoot(), "history"));
            _historyRepo.StartWatcher();
            _gitService  = new GitService(() => Settings);
            _taskTimer   = new TimeTrackingService();

            _taskFactory = new TaskProviderFactory();
            _taskFactory.Register(new ManualTaskProvider());
            _taskFactory.Register(new GiteaTaskProvider(() => Settings));

            _reportGen = new CompositeReportGenerator();
            _reportGen.Register(new JsonReportGenerator());
            _reportGen.Register(new MarkdownReportGenerator());

            _webhook = new WebhookNotificationProvider(() => Settings);
            _eventBus.Subscribe<TaskCompletedEvent>(e => _webhook.OnEvent(e));

            _trackerVm = new TrackerViewModel(
                _eventBus, _storage, _gitService, _reportGen, _taskTimer,
                () => Settings, msg => OutputWindow.Log(msg));
            _trackerVm.FetchTaskFunc      = url => _taskFactory.FetchAsync(url);
            _trackerVm.OpenSettingsAction = OpenSettings;
            _trackerVm.OpenHistoryAction  = () => JoinableTaskFactory.RunAsync(ShowHistoryWindowAsync);

            _historyVm = new HistoryViewModel(_historyRepo, msg => OutputWindow.Log(msg));
            _historyVm.OpenDetailAction = OpenReportDetail;
            _historyVm.OpenFileAction   = OpenFileInShell;

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await ShowTrackerWindow.InitializeAsync(this);
            await ShowHistoryWindow.InitializeAsync(this);
            await ShowTaskSettings.InitializeAsync(this);
            await ShowMainWindow.InitializeAsync(this);
            await ShowSettings.InitializeAsync(this);
            await ShowJsonSettings.InitializeAsync(this);

            try
            {
                var existing = await _storage.LoadCurrentTaskAsync();
                if (existing != null)
                {
                    await _trackerVm.RestoreFromLogAsync(existing);
                    OutputWindow.Log($"[Restore] Task: \"{existing.Task?.Title}\"");
                    StatusBar.SetText($"Task dở: {existing.Task?.Title} — Resume trong Tracker.");
                }
            }
            catch (Exception ex) { OutputWindow.Log($"[Restore] {ex.Message}"); }

            OutputWindow.Log("[DevTaskTracker] v3.0 loaded.");
            StatusBar.SetText("DevTask Tracker ready.");
        }

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid t)
        {
            if (t.Equals(Guid.Parse(TrackerToolWindow.WindowGuidString)) ||
                t.Equals(Guid.Parse(HistoryToolWindow.WindowGuidString)) ||
                t.Equals(Guid.Parse(MainToolWindow.WindowGuidString)))
                return this;
            return null;
        }

        protected override string GetToolWindowTitle(Type t, int id)
        {
            if (t == typeof(TrackerToolWindow)) return TrackerToolWindow.Title;
            if (t == typeof(HistoryToolWindow)) return HistoryToolWindow.Title;
            if (t == typeof(MainToolWindow))    return MainToolWindow.Title;
            return base.GetToolWindowTitle(t, id);
        }

        protected override Task<object> InitializeToolWindowAsync(
            Type t, int id, CancellationToken ct) => Task.FromResult<object>(null);

        public async Task ShowTrackerWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var win = await ShowToolWindowAsync(typeof(TrackerToolWindow), 0, true,
                DisposalToken) as TrackerToolWindow;
            if (win?.Content == null) win?.SetContent(new TrackerControl(_trackerVm));
        }

        public async Task ShowHistoryWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var win = await ShowToolWindowAsync(typeof(HistoryToolWindow), 0, true,
                DisposalToken) as HistoryToolWindow;
            if (win?.Content == null) win?.SetContent(new HistoryControl(_historyVm));
        }

        public void OpenSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dlg = new TaskSettingsDialog(Settings);
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                Settings = dlg.Result;
                JoinableTaskFactory.RunAsync(() => _storage.SaveSettingsAsync(dlg.Result));
                OutputWindow.Log("[Settings] Saved.");
                StatusBar.SetText("Settings saved.");
            }
        }

        private void OpenReportDetail(CompletionReportSummary s)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            new ReportDetailDialog(s).ShowDialog();
        }

        private static void OpenFileInShell(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                try { System.Diagnostics.Process.Start(path); } catch { }
        }

        private string GetStorageRoot()
        {
            if (!string.IsNullOrWhiteSpace(Settings?.StoragePath) &&
                Directory.Exists(Settings.StoragePath))
                return Settings.StoragePath;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DhCodetaskExtension");
        }
    }
}
