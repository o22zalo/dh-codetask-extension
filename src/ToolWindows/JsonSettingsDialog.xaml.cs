using System;
using System.Windows;
using System.Windows.Media;
using DhCodetaskExtension.Services;
using Microsoft.VisualStudio.Shell;

namespace DhCodetaskExtension.ToolWindows
{
    public partial class JsonSettingsDialog : Window
    {
        private readonly JsonConfigService _jsonConfig;
        private readonly OutputWindowService _outputWindow;
        private readonly StatusBarService _statusBar;
        private bool _suppress;

        public JsonSettingsDialog(JsonConfigService jsonConfig, OutputWindowService outputWindow, StatusBarService statusBar)
        {
            _jsonConfig = jsonConfig; _outputWindow = outputWindow; _statusBar = statusBar;
            InitializeComponent();
            Loaded += (s, e) => { Load(_jsonConfig.GetCurrentJson()); TxtFilePath.Text = _jsonConfig.ConfigFilePath; };
        }

        private void Load(string json) { _suppress = true; TxtJson.Text = json; _suppress = false; UpdateCounter(); }
        private void UpdateCounter() { TxtCounter.Text = (TxtJson.Text?.Length ?? 0).ToString("N0") + " chars"; }

        private void BtnFormat_Click(object s, RoutedEventArgs e) { ThreadHelper.ThrowIfNotOnUIThread(); var r = _jsonConfig.FormatJson(TxtJson.Text); if (r.IsOk) { Load(r.Result); SetOk("✔  Formatted."); } else SetErr("✘  " + r.Error); }
        private void BtnValidate_Click(object s, RoutedEventArgs e) { ThreadHelper.ThrowIfNotOnUIThread(); var r = _jsonConfig.TryParse(TxtJson.Text); if (r.Valid) SetOk("✔  Valid JSON."); else SetErr("✘  " + r.Error); }
        private void BtnReset_Click(object s, RoutedEventArgs e) { ThreadHelper.ThrowIfNotOnUIThread(); if (MessageBox.Show("Reset về mặc định?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { Load(JsonConfigService.DefaultJson); SetInfo("↩  Default loaded. Bấm Save."); } }
        private void BtnReload_Click(object s, RoutedEventArgs e) { ThreadHelper.ThrowIfNotOnUIThread(); Load(_jsonConfig.GetCurrentJson()); SetInfo("🔄  Reloaded."); }
        private void TxtJson_TextChanged(object s, System.Windows.Controls.TextChangedEventArgs e) { if (_suppress) return; UpdateCounter(); }
        private void BtnSave_Click(object s, RoutedEventArgs e) { ThreadHelper.ThrowIfNotOnUIThread(); _outputWindow?.Activate(); var r = _jsonConfig.Save(TxtJson.Text); if (r.Success) { _statusBar?.SetText("JsonConfig saved."); DialogResult = true; Close(); } else SetErr("✘  " + r.Error); }
        private void BtnCancel_Click(object s, RoutedEventArgs e) { DialogResult = false; Close(); }

        private void SetOk(string m) => Set(m, Color.FromRgb(0xE8,0xF5,0xE9), Color.FromRgb(0x4C,0xAF,0x50));
        private void SetErr(string m) => Set(m, Color.FromRgb(0xFF,0xF3,0xE0), Color.FromRgb(0xFF,0x98,0x00));
        private void SetInfo(string m) => Set(m, Color.FromRgb(0xE3,0xF2,0xFD), Color.FromRgb(0x21,0x96,0xF3));
        private void Set(string msg, Color bg, Color bd) { TxtStatus.Text=msg; StatusBorder.Background=new SolidColorBrush(bg); StatusBorder.BorderBrush=new SolidColorBrush(bd); }
    }
}
