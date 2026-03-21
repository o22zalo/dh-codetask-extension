using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DhCodetaskExtension.ViewModels;
using Microsoft.Win32;

namespace DhCodetaskExtension.ToolWindows
{
    public partial class HistoryControl : UserControl
    {
        private readonly HistoryViewModel _vm;
        private Point _downPoint;

        public HistoryControl(HistoryViewModel vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            InitializeComponent();
            DataContext = _vm;
            Loaded += async (s, e) => await _vm.RefreshAsync();
        }

        private void Filter_Today(object sender, RoutedEventArgs e)
        { _vm.ViewMode = HistoryViewMode.Today; CustomRangePanel.Visibility = Visibility.Collapsed; }

        private void Filter_Week(object sender, RoutedEventArgs e)
        { _vm.ViewMode = HistoryViewMode.ThisWeek; CustomRangePanel.Visibility = Visibility.Collapsed; }

        private void Filter_Month(object sender, RoutedEventArgs e)
        { _vm.ViewMode = HistoryViewMode.ThisMonth; CustomRangePanel.Visibility = Visibility.Collapsed; }

        private void Filter_Custom(object sender, RoutedEventArgs e)
        { CustomRangePanel.Visibility = Visibility.Visible; }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
            => await _vm.RefreshAsync();

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
            => _downPoint = e.GetPosition(null);

        private void Item_Click(object sender, MouseButtonEventArgs e)
        {
            var diff = e.GetPosition(null) - _downPoint;
            if (Math.Abs(diff.X) > 4 || Math.Abs(diff.Y) > 4) return;

            if (sender is FrameworkElement fe && fe.DataContext is CompletionReportSummary s)
            {
                if (e.ClickCount == 2)
                    _vm.OpenFileAction?.Invoke(s.MarkdownFilePath);
                else
                    _vm.OpenDetailAction?.Invoke(s);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is CompletionReportSummary s)
            {
                var r = MessageBox.Show($"Xóa report #{s.TaskId} — {s.TaskTitle}?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                    await _vm.DeleteCommand.ExecuteAsync(s);
            }
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "CSV files|*.csv",
                FileName = $"history_{DateTime.Today:yyyyMMdd}.csv"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("TaskId,TaskTitle,StartedAt,CompletedAt,TotalElapsed,TodoDone,TodoTotal,WasPushed");
                foreach (var item in _vm.Items)
                    sb.AppendLine($"{item.TaskId},\"{item.TaskTitle}\",{item.StartedAt:yyyy-MM-dd HH:mm}," +
                        $"{item.CompletedAt:yyyy-MM-dd HH:mm},{item.ElapsedDisplay}," +
                        $"{item.TodoDone},{item.TodoTotal},{item.WasPushed}");
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Export thành công!", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Extension to allow async execute on RelayCommand<T>
    internal static class RelayCommandExtensions
    {
        public static System.Threading.Tasks.Task ExecuteAsync<T>(
            this DhCodetaskExtension.Core.Services.RelayCommand<T> cmd, T arg)
        {
            cmd.Execute(arg);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
