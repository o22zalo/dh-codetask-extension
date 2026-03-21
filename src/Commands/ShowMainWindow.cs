using System;
using System.ComponentModel.Design;
using VS2017ExtensionTemplate.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace VS2017ExtensionTemplate
{
    /// <summary>
    /// Command that opens the main tool window.
    /// Registered in VSCommandTable.vsct with ID ShowMainWindowId (0x0100).
    /// Appears under View > Other Windows > Extension Template.
    ///
    /// HOW TO CUSTOMIZE:
    ///   - Rename to match your tool window
    ///   - Change the target window type in Execute()
    /// </summary>
    internal sealed class ShowMainWindow
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
            var cmdId = new CommandID(PackageGuids.CommandSetGuid, PackageIds.ShowMainWindowId);
            var cmd   = new MenuCommand((s, e) => Execute(package), cmdId);
            commandService.AddCommand(cmd);
        }

        private static void Execute(AsyncPackage package)
        {
            package.JoinableTaskFactory.RunAsync(async () =>
            {
                await package.ShowToolWindowAsync(
                    typeof(MainToolWindow),
                    0,
                    create: true,
                    cancellationToken: package.DisposalToken);
            });
        }
    }
}
