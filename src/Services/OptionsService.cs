using System;
using System.ComponentModel;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace VS2017ExtensionTemplate.Services
{
    // ====================================================================== //
    //  Options Page                                                           //
    // ====================================================================== //

    /// <summary>
    /// Appears under Tools › Options › VS2017 Extension Template › General.
    /// Properties are automatically persisted to VS registry by DialogPage.
    ///
    /// HOW TO CUSTOMIZE:
    ///   - Add/remove/rename properties here
    ///   - Use [Category], [DisplayName], [Description] attributes for UI grouping
    ///   - Update the ProvideOptionPage attribute on MyPackage to match category name
    /// </summary>
    public class SampleOptionsPage : DialogPage
    {
        [Category("Connection")]
        [DisplayName("Server URL")]
        [Description("URL of the backend server used by this extension.")]
        public string ServerUrl { get; set; } = "https://api.example.com";

        [Category("Behavior")]
        [DisplayName("Auto Format on Save")]
        [Description("Automatically format the active document when it is saved.")]
        public bool AutoFormat { get; set; } = false;

        [Category("Behavior")]
        [DisplayName("Max Log Items")]
        [Description("Maximum number of items shown in the Output pane log.")]
        public int MaxLogItems { get; set; } = 200;

        [Category("Behavior")]
        [DisplayName("Enable Debug Logging")]
        [Description("Log verbose debug information to the Output pane.")]
        public bool EnableDebugLog { get; set; } = false;
    }

    // ====================================================================== //
    //  Service wrapper                                                        //
    // ====================================================================== //

    /// <summary>
    /// Thin wrapper around <see cref="SampleOptionsPage"/>.
    /// Provides typed access to settings and a helper to open the Options dialog.
    /// All public methods must be called on the UI thread.
    /// </summary>
    public sealed class OptionsService
    {
        private readonly AsyncPackage     _package;
        private readonly IServiceProvider _serviceProvider;

        public OptionsService(AsyncPackage package)
        {
            _package         = package ?? throw new ArgumentNullException(nameof(package));
            _serviceProvider = package;
        }

        public SampleOptionsPage GetPage()
            => (SampleOptionsPage)_package.GetDialogPage(typeof(SampleOptionsPage));

        // Typed accessors
        public string ServerUrl      => GetPage().ServerUrl;
        public bool   AutoFormat     => GetPage().AutoFormat;
        public int    MaxLogItems    => GetPage().MaxLogItems;
        public bool   EnableDebugLog => GetPage().EnableDebugLog;

        /// <summary>Opens the Tools > Options dialog at the extension's settings page.</summary>
        public void OpenOptionsDialog()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE2;
            // TODO: Update category name to match your ProvideOptionPage attribute
            dte?.ExecuteCommand("Tools.Options", "VS2017 Extension Template.General");
        }
    }
}
