using System;
using System.ComponentModel.Design;
using VS2017ExtensionTemplate.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace VS2017ExtensionTemplate
{
    /// <summary>
    /// Command that opens the simple Settings dialog from the Tools menu.
    /// Registered in VSCommandTable.vsct with ID CmdIdSettings (0x0400).
    /// Appears as: Tools > VS2017 Extension Template Settings...
    /// </summary>
    internal sealed class ShowSettings
    {
        private readonly AsyncPackage _package;

        private ShowSettings(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs == null) return;
            var cmdId = new CommandID(PackageGuids.CommandSetGuid, PackageIds.CmdIdSettings);
            var cmd   = new MenuCommand(new ShowSettings(package).Execute, cmdId);
            cs.AddCommand(cmd);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var pkg = (MyPackage)_package;
            var dlg = new SettingsDialog(pkg.Config, pkg.OutputWindow, pkg.StatusBar);
            dlg.ShowDialog();
        }
    }
}
