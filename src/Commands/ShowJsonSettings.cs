using System;
using System.ComponentModel.Design;
using VS2017ExtensionTemplate.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace VS2017ExtensionTemplate
{
    /// <summary>
    /// Command that opens the JSON Settings dialog from the Tools menu.
    /// Registered in VSCommandTable.vsct with ID CmdIdJsonSettings (0x0500).
    /// Appears as: Tools > VS2017 Extension Template Settings (JSON)...
    /// </summary>
    internal sealed class ShowJsonSettings
    {
        private readonly AsyncPackage _package;

        private ShowJsonSettings(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs == null) return;
            var cmdId = new CommandID(PackageGuids.CommandSetGuid, PackageIds.CmdIdJsonSettings);
            var cmd   = new MenuCommand(new ShowJsonSettings(package).Execute, cmdId);
            cs.AddCommand(cmd);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var pkg = (MyPackage)_package;
            var dlg = new JsonSettingsDialog(pkg.JsonConfig, pkg.OutputWindow, pkg.StatusBar);
            dlg.ShowDialog();
        }
    }
}
