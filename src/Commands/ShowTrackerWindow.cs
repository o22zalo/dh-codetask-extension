using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension
{
    internal sealed class ShowTrackerWindow
    {
        private readonly DevTaskTrackerPackage _package;

        private ShowTrackerWindow(DevTaskTrackerPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public static async Task InitializeAsync(DevTaskTrackerPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs == null) return;
            var cmdId = new CommandID(PackageGuids.CommandSetGuid, PackageIds.ShowTrackerWindowId);
            cs.AddCommand(new MenuCommand(new ShowTrackerWindow(package).Execute, cmdId));
        }

        private void Execute(object sender, EventArgs e)
        {
            _package.JoinableTaskFactory.RunAsync(_package.ShowTrackerWindowAsync);
        }
    }
}
