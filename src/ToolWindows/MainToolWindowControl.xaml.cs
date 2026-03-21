using System;
using System.Windows;
using System.Windows.Controls;
using DhCodetaskExtension.Services;
using Microsoft.VisualStudio.Shell;

namespace DhCodetaskExtension.ToolWindows
{
    public partial class MainToolWindowControl : UserControl
    {
        private readonly MainToolWindowState _state;
        private OutputWindowService OutputWindow => _state.OutputWindow;
        private ConfigurationService Config => _state.Config;

        public MainToolWindowControl(MainToolWindowState state)
        {
            _state = state;
            InitializeComponent();
        }

        private void Button_WriteOutput_Click(object s, RoutedEventArgs e)
        { ThreadHelper.ThrowIfNotOnUIThread(); OutputWindow?.Activate(); OutputWindow?.Log("Hello from DH Codetask Extension!"); }

        private void Button_ConfigDump_Click(object s, RoutedEventArgs e)
        { ThreadHelper.ThrowIfNotOnUIThread(); OutputWindow?.Activate(); Config?.DumpToOutput(); }

        private void BtnOpenTracker_Click(object s, RoutedEventArgs e)
        {
            var pkg = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider)) as DevTaskTrackerPackage;
            pkg?.JoinableTaskFactory?.RunAsync(pkg.ShowTrackerWindowAsync);
        }

        private void BtnOpenHistory_Click(object s, RoutedEventArgs e)
        {
            var pkg = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider)) as DevTaskTrackerPackage;
            pkg?.JoinableTaskFactory?.RunAsync(pkg.ShowHistoryWindowAsync);
        }
    }
}
