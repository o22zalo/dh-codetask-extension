using System;
using System.ComponentModel.Design;
using DhCodetaskExtension.Services;
using DhCodetaskExtension.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension
{
    internal sealed class ShowJsonSettings
    {
        private readonly AsyncPackage _package;
        private ShowJsonSettings(AsyncPackage p) { _package = p; }
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs == null) return;
            cs.AddCommand(new MenuCommand(new ShowJsonSettings(package).Execute,
                new CommandID(PackageGuids.CommandSetGuid, PackageIds.CmdIdJsonSettings)));
        }
        private void Execute(object s, EventArgs e) { /* Opens legacy JSON editor — wired to TrackerVM settings */ }
    }
}
