using System;
using System.ComponentModel.Design;
using DhCodetaskExtension.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension
{
    internal sealed class ShowSettings
    {
        private readonly AsyncPackage _package;
        private ShowSettings(AsyncPackage p) { _package = p; }
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs == null) return;
            cs.AddCommand(new MenuCommand(new ShowSettings(package).Execute,
                new CommandID(PackageGuids.CommandSetGuid, PackageIds.CmdIdSettings)));
        }
        private void Execute(object s, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var pkg = (DevTaskTrackerPackage)_package;
            pkg.OpenSettings();
        }
    }
}
