using System;
using System.Windows;
using System.Windows.Controls;
using DhCodetaskExtension.Core.Models;

namespace DhCodetaskExtension.ToolWindows
{
    public partial class TaskSettingsDialog : Window
    {
        public AppSettings Result { get; private set; }
        private readonly AppSettings _current;

        public TaskSettingsDialog(AppSettings current)
        {
            _current = current ?? new AppSettings();
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            TxtGiteaUrl.Text = _current.GiteaBaseUrl;
            PwdToken.Password = _current.GiteaToken;
            TxtGiteaUser.Text = _current.GiteaUser;
            ChkAutoPush.IsChecked = _current.GitAutoPush;
            TxtGitName.Text = _current.GitUserName;
            TxtGitEmail.Text = _current.GitUserEmail;
            TxtStoragePath.Text = _current.StoragePath;
            SelectCombo(CmbReportFormat, _current.ReportFormat);
            SelectCombo(CmbHistoryView, _current.HistoryDefaultView);
            ChkWebhook.IsChecked = _current.WebhookEnabled;
            TxtWebhookUrl.Text = _current.WebhookUrl;
        }

        private static void SelectCombo(ComboBox c, string text)
        {
            foreach (ComboBoxItem item in c.Items)
                if (string.Equals(item.Content?.ToString(), text, StringComparison.OrdinalIgnoreCase))
                { c.SelectedItem = item; return; }
            if (c.Items.Count > 0) c.SelectedIndex = 0;
        }

        private bool Validate(out string error)
        {
            var url = TxtGiteaUrl.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(url) &&
                !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            { error = "Gitea Base URL phải bắt đầu bằng http:// hoặc https://"; return false; }

            var webhookEnabled = ChkWebhook.IsChecked == true;
            var webhookUrl = TxtWebhookUrl.Text?.Trim() ?? "";
            if (webhookEnabled && string.IsNullOrWhiteSpace(webhookUrl))
            { error = "Webhook URL không được để trống khi Enable Webhook."; return false; }

            error = null;
            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate(out string error))
            {
                TxtValidation.Text = "⚠  " + error;
                ValidationBorder.Visibility = Visibility.Visible;
                return;
            }

            Result = new AppSettings
            {
                GiteaBaseUrl = TxtGiteaUrl.Text?.Trim() ?? string.Empty,
                GiteaToken = PwdToken.Password,
                GiteaUser = TxtGiteaUser.Text?.Trim() ?? string.Empty,
                GitAutoPush = ChkAutoPush.IsChecked == true,
                GitUserName = TxtGitName.Text?.Trim() ?? string.Empty,
                GitUserEmail = TxtGitEmail.Text?.Trim() ?? string.Empty,
                StoragePath = TxtStoragePath.Text?.Trim() ?? string.Empty,
                ReportFormat = (CmbReportFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "json+markdown",
                HistoryDefaultView = (CmbHistoryView.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "week",
                WebhookEnabled = ChkWebhook.IsChecked == true,
                WebhookUrl = TxtWebhookUrl.Text?.Trim() ?? string.Empty,
                Extensions = _current.Extensions
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
