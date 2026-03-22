using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension
{
    internal sealed class ShowProjectHelperWindow
    {
        private readonly DevTaskTrackerPackage _package;
        private ShowProjectHelperWindow(DevTaskTrackerPackage p) { _package = p; }

        public static async Task InitializeAsync(DevTaskTrackerPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs == null) return;
            cs.AddCommand(new MenuCommand(
                new ShowProjectHelperWindow(package).Execute,
                new CommandID(PackageGuids.CommandSetGuid, PackageIds.ShowProjectHelperWindowId)));
        }

        private void Execute(object s, EventArgs e)
            => _package.JoinableTaskFactory.RunAsync(_package.ShowProjectHelperWindowAsync);
    }
}
