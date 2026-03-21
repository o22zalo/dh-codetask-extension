using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension
{
    internal sealed class ShowHistoryWindow
    {
        private readonly DevTaskTrackerPackage _package;
        private ShowHistoryWindow(DevTaskTrackerPackage p) { _package = p; }

        public static async Task InitializeAsync(DevTaskTrackerPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs == null) return;
            cs.AddCommand(new MenuCommand(
                new ShowHistoryWindow(package).Execute,
                new CommandID(PackageGuids.CommandSetGuid, PackageIds.ShowHistoryWindowId)));
        }

        private void Execute(object s, EventArgs e)
            => _package.JoinableTaskFactory.RunAsync(_package.ShowHistoryWindowAsync);
    }

    internal sealed class ShowTaskSettings
    {
        private readonly DevTaskTrackerPackage _package;
        private ShowTaskSettings(DevTaskTrackerPackage p) { _package = p; }

        public static async Task InitializeAsync(DevTaskTrackerPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs == null) return;
            cs.AddCommand(new MenuCommand(
                new ShowTaskSettings(package).Execute,
                new CommandID(PackageGuids.CommandSetGuid, PackageIds.ShowTaskSettingsId)));
        }

        private void Execute(object s, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _package.OpenSettings();
        }
    }
}
