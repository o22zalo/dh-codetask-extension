using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension.Services
{
    public sealed class OutputWindowService
    {
        public static readonly Guid PaneGuid = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF0123456789");
        private const string PaneTitle = "DH Codetask Extension";
        private IVsOutputWindowPane _pane;
        private readonly AsyncPackage _package;

        public OutputWindowService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var outputWindow = await _package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null) return;
            var guid = PaneGuid;
            outputWindow.GetPane(ref guid, out _pane);
            if (_pane == null)
            {
                outputWindow.CreatePane(ref guid, PaneTitle, fInitVisible: 1, fClearWithSolution: 0);
                outputWindow.GetPane(ref guid, out _pane);
            }
        }

        public void WriteLine(string message) { ThreadHelper.ThrowIfNotOnUIThread(); _pane?.OutputString(message + "\n"); }
        public void Log(string message) { ThreadHelper.ThrowIfNotOnUIThread(); _pane?.OutputString($"[{DateTime.Now:HH:mm:ss}] {message}\n"); }
        public void Activate() { ThreadHelper.ThrowIfNotOnUIThread(); _pane?.Activate(); }
        public void Clear() { ThreadHelper.ThrowIfNotOnUIThread(); _pane?.Clear(); }
    }
}
