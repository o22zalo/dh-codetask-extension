using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VS2017ExtensionTemplate.Services
{
    /// <summary>
    /// Thin wrapper around IVsStatusbar.
    /// Lets callers set text, show/hide the progress animation,
    /// and display a progress bar without touching raw COM.
    /// </summary>
    public sealed class StatusBarService
    {
        private IVsStatusbar _statusBar;
        private readonly AsyncPackage _package;

        public StatusBarService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        // ------------------------------------------------------------------ //
        //  Initialization                                                      //
        // ------------------------------------------------------------------ //

        public async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _statusBar =
                await _package.GetServiceAsync(typeof(SVsStatusbar))
                as IVsStatusbar;
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Displays <paramref name="text"/> in the status bar.</summary>
        public void SetText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            int frozen = 0;
            _statusBar?.IsFrozen(out frozen);
            if (frozen != 0)
                _statusBar?.FreezeOutput(0);
            _statusBar?.SetText(text);
        }

        /// <summary>Shows the spinning animation icon with an optional label.</summary>
        public void StartAnimation(string text = "Working…")
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SetText(text);
            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;
            _statusBar?.Animation(fOnOff: 1, pvIcon: ref icon);
        }

        /// <summary>Stops the spinning animation.</summary>
        public void StopAnimation()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;
            _statusBar?.Animation(fOnOff: 0, pvIcon: ref icon);
        }

        /// <summary>Shows a progress bar in the status bar.</summary>
        public void ReportProgress(ref uint cookie, string label, uint current, uint total)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _statusBar?.Progress(ref cookie, fInProgress: 1,
                pwszLabel: label, nComplete: current, nTotal: total);
        }

        /// <summary>Clears/hides the progress bar.</summary>
        public void ClearProgress(ref uint cookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _statusBar?.Progress(ref cookie, fInProgress: 0,
                pwszLabel: string.Empty, nComplete: 0, nTotal: 0);
        }

        /// <summary>
        /// Convenience: runs <paramref name="work"/> on a background thread while
        /// showing a status-bar animation, then clears it on the UI thread.
        /// </summary>
        public async Task RunWithAnimationAsync(Func<Task> work, string statusText = "Working…")
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            StartAnimation(statusText);
            try
            {
                await System.Threading.Tasks.Task.Run(work);
            }
            finally
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                StopAnimation();
                SetText("Ready");
            }
        }
    }
}
