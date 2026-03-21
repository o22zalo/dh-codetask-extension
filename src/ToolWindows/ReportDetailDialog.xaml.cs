using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.ViewModels;

namespace DhCodetaskExtension.ToolWindows
{
    public partial class ReportDetailDialog : Window
    {
        private readonly CompletionReportSummary _summary;

        public ReportDetailDialog(CompletionReportSummary summary)
        {
            _summary = summary ?? throw new ArgumentNullException(nameof(summary));
            InitializeComponent();
            Populate();
        }

        private void Populate()
        {
            var r = _summary.FullReport;
            if (r == null) { Title = "Report không tồn tại"; return; }

            Title = $"📋 Chi tiết: #{r.TaskId}: {r.TaskTitle}";
            TxtTitle.Text = $"#{r.TaskId}: {r.TaskTitle}";
            TxtStarted.Text = $"🟢 Bắt đầu: {r.StartedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
            TxtEnded.Text = $"🔴 Kết thúc: {r.CompletedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
            TxtElapsed.Text = $"⏳ Thời gian: {FormatSpanFull(r.TotalElapsed)}";
            TxtNotes.Text = r.WorkNotes;
            TxtBiz.Text = r.BusinessLogic;
            TxtCommit.Text = r.CommitMessage;
            TxtGit.Text = $"Branch: {r.GitBranch}  |  Hash: {(string.IsNullOrEmpty(r.GitCommitHash) ? "— chưa push" : r.GitCommitHash)}  |  {(r.WasPushed ? "✅ Pushed" : "⬜ Not pushed")}";
            TxtTodoHeader.Text = $"✅ TODO ({r.TodoDone}/{r.TodoTotal} xong, tổng {FormatSpan(r.TotalTodoElapsed)}):";

            var sessions = r.Sessions?.Select((s, i) => new
            {
                Index = i + 1,
                Start = s.StartTime.ToLocalTime().ToString("HH:mm:ss"),
                End = s.EndTime.HasValue ? s.EndTime.Value.ToLocalTime().ToString("HH:mm:ss") : "đang chạy",
                Duration = FormatSpan(s.Duration)
            }).ToList();
            GridSessions.ItemsSource = sessions;

            var todos = r.Todos?.Select((t, i) => new
            {
                Index = i + 1,
                t.Text,
                Status = t.IsDone ? "✅ Xong" : "⬜ Chưa",
                Elapsed = t.TotalElapsed.TotalSeconds > 0 ? FormatSpan(t.TotalElapsed) : "—"
            }).ToList();
            GridTodos.ItemsSource = todos;
        }

        private void BtnOpenMd_Click(object sender, RoutedEventArgs e)
            => OpenFile(_summary.MarkdownFilePath);

        private void BtnOpenJson_Click(object sender, RoutedEventArgs e)
            => OpenFile(_summary.JsonFilePath);

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private static void OpenFile(string path)
        {
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                try { Process.Start(path); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            else
            {
                MessageBox.Show("File không tồn tại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static string FormatSpanFull(TimeSpan ts) =>
            $"{(int)ts.TotalHours} giờ {ts.Minutes} phút {ts.Seconds} giây";

        private static string FormatSpan(TimeSpan ts) =>
            ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s"
                               : $"{ts.Minutes}m {ts.Seconds}s";
    }
}
