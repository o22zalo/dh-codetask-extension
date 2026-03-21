using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VS2017ExtensionTemplate.Services
{
    /// <summary>
    /// Manages a custom Output Window pane for this extension.
    /// Wraps IVsOutputWindow / IVsOutputWindowPane so callers never
    /// touch raw COM interfaces directly.
    ///
    /// HOW TO CUSTOMIZE:
    ///   - Change PaneGuid to a newly generated GUID (must be unique per extension)
    ///   - Change PaneTitle to your extension's name
    /// </summary>
    public sealed class OutputWindowService
    {
        // TODO: Generate a new GUID for your extension's output pane
        public static readonly Guid PaneGuid =
            new Guid("A1B2C3D4-E5F6-7890-ABCD-EF0123456789");

        private const string PaneTitle = "VS2017 Extension Template";

        private IVsOutputWindowPane _pane;
        private readonly AsyncPackage _package;

        public OutputWindowService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        // ------------------------------------------------------------------ //
        //  Initialization (call once, on background thread is fine)           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates (or retrieves) the custom Output pane.
        /// Safe to call from a background thread – switches to UI thread internally.
        /// </summary>
        public async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var outputWindow =
                await _package.GetServiceAsync(typeof(SVsOutputWindow))
                as IVsOutputWindow;

            if (outputWindow == null) return;

            var guid = PaneGuid;
            outputWindow.GetPane(ref guid, out _pane);

            if (_pane == null)
            {
                outputWindow.CreatePane(ref guid, PaneTitle,
                    fInitVisible: 1, fClearWithSolution: 0);
                outputWindow.GetPane(ref guid, out _pane);
            }
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Writes a line of text (appends \n automatically).</summary>
        public void WriteLine(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _pane?.OutputString(message + "\n");
        }

        /// <summary>Writes a timestamped line – useful for log-style output.</summary>
        public void Log(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _pane?.OutputString($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        /// <summary>Brings this pane to the foreground in the Output window.</summary>
        public void Activate()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _pane?.Activate();
        }

        /// <summary>Removes all text from the pane.</summary>
        public void Clear()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _pane?.Clear();
        }
    }
}
