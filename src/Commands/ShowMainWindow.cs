using System;
using System.ComponentModel.Design;
using DhCodetaskExtension.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension
{
    internal sealed class ShowMainWindow
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            var cs = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
            var cmd = new MenuCommand((s, e) => Execute(package),
                new CommandID(PackageGuids.CommandSetGuid, PackageIds.ShowMainWindowId));
            cs.AddCommand(cmd);
        }
        private static void Execute(AsyncPackage package)
        {
            package.JoinableTaskFactory.RunAsync(async () =>
                await package.ShowToolWindowAsync(typeof(MainToolWindow), 0, true, package.DisposalToken));
        }
    }
}
