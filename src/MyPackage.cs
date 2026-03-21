using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VS2017ExtensionTemplate.Services;
using VS2017ExtensionTemplate.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
// SampleOptionsPage is defined in VS2017ExtensionTemplate.Services (OptionsService.cs)

namespace VS2017ExtensionTemplate
{
    /// <summary>
    /// VS2017 Extension Template — Main Package Entry Point.
    ///
    /// This template demonstrates the AsyncPackage pattern for Visual Studio 2017.
    /// It includes:
    ///   - OutputWindowService  : custom Output pane
    ///   - StatusBarService     : status bar text and progress
    ///   - ConfigurationService : XML-based key-value config (persists to %AppData%)
    ///   - JsonConfigService    : JSON-based config with diff logging
    ///   - MainToolWindow       : a dockable tool window
    ///   - SettingsDialog       : a WPF modal settings dialog (Tools menu)
    ///   - JsonSettingsDialog   : a JSON editor settings dialog (Tools menu)
    ///
    /// HOW TO CUSTOMIZE:
    ///   1. Replace all GUIDs in PackageGuids.cs and VSCommandTable.vsct
    ///   2. Rename the namespace and assembly in csproj + AssemblyInfo.cs
    ///   3. Update source.extension.vsixmanifest (Id, DisplayName, Description)
    ///   4. Add your own services and commands following the existing patterns
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(
        "VS2017 Extension Template",
        "A ready-to-use template for building VS2017 extensions with AsyncPackage, settings and tool windows.",
        "1.0")]
    [ProvideToolWindow(typeof(MainToolWindow),
        Style = VsDockStyle.Tabbed, DockedWidth = 300,
        Window = "DocumentWell", Orientation = ToolWindowOrientation.Left)]
    [Guid(PackageGuids.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Services.SampleOptionsPage),
        "VS2017 Extension Template", "General",
        categoryResourceID: 0, pageNameResourceID: 0,
        supportsAutomation: true)]
    public sealed class MyPackage : AsyncPackage
    {
        // ── Public service properties ─────────────────────────────────────
        // Expose services so Commands can access them via (MyPackage)package cast.

        public OutputWindowService  OutputWindow { get; private set; }
        public StatusBarService     StatusBar    { get; private set; }
        public ConfigurationService Config       { get; private set; }
        public JsonConfigService    JsonConfig   { get; private set; }

        // ── Package initialization ────────────────────────────────────────

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            // 1. Construct services (safe on background thread)
            OutputWindow = new OutputWindowService(this);
            StatusBar    = new StatusBarService(this);
            Config       = new ConfigurationService(this, OutputWindow);
            JsonConfig   = new JsonConfigService(this, OutputWindow);

            // 2. Initialize services that need async setup
            await OutputWindow.InitializeAsync();
            await StatusBar.InitializeAsync();
            await Config.InitializeAsync();
            await JsonConfig.InitializeAsync();

            // 3. Switch to UI thread for command registration and UI work
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // 4. Register commands
            await ShowMainWindow.InitializeAsync(this);
            await ShowSettings.InitializeAsync(this);
            await ShowJsonSettings.InitializeAsync(this);

            // 5. Ready
            OutputWindow.Log("VS2017 Extension Template loaded successfully.");
            StatusBar.SetText("VS2017 Extension Template ready.");
        }

        // ── Async tool window factory ─────────────────────────────────────

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            if (toolWindowType.Equals(Guid.Parse(MainToolWindow.WindowGuidString)))
                return this;
            return null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            if (toolWindowType == typeof(MainToolWindow))
                return MainToolWindow.Title;
            return base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override async Task<object> InitializeToolWindowAsync(
            Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            var dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

            if (toolWindowType == typeof(MainToolWindow))
            {
                return new MainToolWindowState
                {
                    DTE          = dte,
                    OutputWindow = OutputWindow,
                    StatusBar    = StatusBar,
                    Config       = Config,
                    JsonConfig   = JsonConfig,
                };
            }

            return null;
        }
    }
}
