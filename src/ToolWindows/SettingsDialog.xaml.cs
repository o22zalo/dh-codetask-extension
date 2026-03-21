using System;
using System.Windows;
using DhCodetaskExtension.Services;
using Microsoft.VisualStudio.Shell;

namespace DhCodetaskExtension.ToolWindows
{
    public partial class SettingsDialog : Window
    {
        private readonly ConfigurationService _config;
        private readonly OutputWindowService _outputWindow;
        private readonly StatusBarService _statusBar;

        public SettingsDialog(ConfigurationService config, OutputWindowService outputWindow, StatusBarService statusBar)
        {
            _config = config; _outputWindow = outputWindow; _statusBar = statusBar;
            InitializeComponent();
            Loaded += (s, e) => { TxtServerUrl.Text = _config?.ServerUrl; ValidationBorder.Visibility = Visibility.Collapsed; };
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var url = TxtServerUrl.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(url)) { TxtValidation.Text = "⚠  Server URL không được để trống."; ValidationBorder.Visibility = Visibility.Visible; return; }
            _config?.Set(ConfigurationService.KeyServerUrl, url);
            _config?.Set(ConfigurationService.KeyApiKey, PwdApiKey.Password);
            _outputWindow?.Log("[Settings] Saved."); _statusBar?.SetText("Settings saved.");
            DialogResult = true; Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}
