using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DhCodetaskExtension.Models;
using DhCodetaskExtension.Services;
using Microsoft.VisualStudio.Shell;

namespace DhCodetaskExtension.ToolWindows
{
    public partial class MainToolWindowControl : UserControl
    {
        private readonly MainToolWindowState _state;
        private readonly DispatcherTimer _timer;

        private OutputWindowService OutputWindow => _state.OutputWindow;
        private StatusBarService StatusBar => _state.StatusBar;
        private TaskTrackerService Tracker => _state.Tracker;

        public MainToolWindowControl(MainToolWindowState state)
        {
            _state = state;
            InitializeComponent();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            LoadFromState();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            RefreshTimer();
            RefreshTodoList(false);
        }

        private void LoadFromState()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            TrackerTaskState current = Tracker.Current;
            TxtTaskUrl.Text = current.TaskUrl;
            TxtTaskId.Text = current.TaskId;
            TxtTaskTitle.Text = current.TaskTitle;
            TxtLabels.Text = current.Labels;
            TxtDescription.Text = current.Description;
            TxtWorkNotes.Text = current.WorkNotes;
            TxtBusinessLogic.Text = current.BusinessLogic;
            TxtCommitMessage.Text = current.CommitMessage;
            TxtStorageInfo.Text = "Snapshot: " + Tracker.CurrentTaskFilePath;
            RefreshTimer();
            RefreshTodoList(true);
        }

        private void SyncUiToState()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Tracker.UpdateTaskDetails(
                TxtTaskUrl.Text,
                TxtTaskId.Text,
                TxtTaskTitle.Text,
                TxtLabels.Text,
                TxtDescription.Text,
                TxtWorkNotes.Text,
                TxtBusinessLogic.Text,
                TxtCommitMessage.Text);
        }

        private void RefreshTimer()
        {
            TrackerTaskState current = Tracker.Current;
            TxtTaskTimer.Text = TaskTrackerService.FormatElapsed(current.GetTaskElapsed(DateTime.UtcNow));
        }

        private void RefreshTodoList(bool preserveSelection)
        {
            string selectedId = null;
            if (preserveSelection && LstTodos.SelectedItem is TodoListRow selected)
                selectedId = selected.Id;

            LstTodos.ItemsSource = Tracker.Current.Todos
                .Select(todo => new TodoListRow
                {
                    Id = todo.Id,
                    Title = string.Format("[{0}] {1} — {2}", todo.IsDone ? "x" : " ", todo.Title, TaskTrackerService.FormatElapsed(todo.GetElapsed(DateTime.UtcNow)))
                })
                .ToList();

            if (preserveSelection && selectedId != null)
                LstTodos.SelectedItem = LstTodos.Items.Cast<TodoListRow>().FirstOrDefault(x => x.Id == selectedId);
        }

        private string GetSelectedTodoId()
        {
            TodoListRow row = LstTodos.SelectedItem as TodoListRow;
            return row != null ? row.Id : null;
        }

        private void Button_StartTask_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SyncUiToState();
            Tracker.StartTask();
            Tracker.SaveSnapshot();
            OutputWindow.Log("[Tracker] Task started.");
            StatusBar.SetText("Task started.");
            RefreshTimer();
        }

        private void Button_PauseTask_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SyncUiToState();
            Tracker.PauseTask();
            Tracker.SaveSnapshot();
            OutputWindow.Log("[Tracker] Task paused.");
            StatusBar.SetText("Task paused.");
            RefreshTimer();
        }

        private void Button_ResumeTask_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SyncUiToState();
            Tracker.ResumeTask();
            Tracker.SaveSnapshot();
            OutputWindow.Log("[Tracker] Task resumed.");
            StatusBar.SetText("Task resumed.");
            RefreshTimer();
        }

        private void Button_StopTask_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SyncUiToState();
            Tracker.StopTask();
            Tracker.SaveSnapshot();
            OutputWindow.Log("[Tracker] Task stopped.");
            StatusBar.SetText("Task stopped.");
            RefreshTimer();
        }

        private void Button_AddTodo_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string title = (TxtNewTodo.Text ?? string.Empty).Trim();
            if (title.Length == 0)
            {
                MessageBox.Show("Nhập nội dung TODO trước khi thêm.", "DH Codetask", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            SyncUiToState();
            Tracker.AddTodo(title);
            Tracker.SaveSnapshot();
            TxtNewTodo.Clear();
            RefreshTodoList(false);
            OutputWindow.Log("[Tracker] Added TODO: " + title);
            StatusBar.SetText("TODO added.");
        }

        private void Button_StartTodo_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string todoId = GetSelectedTodoId();
            if (string.IsNullOrEmpty(todoId)) return;
            SyncUiToState();
            Tracker.StartTodo(todoId);
            Tracker.SaveSnapshot();
            RefreshTodoList(true);
            OutputWindow.Log("[Tracker] TODO timer started.");
            StatusBar.SetText("TODO timer started.");
        }

        private void Button_PauseTodo_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string todoId = GetSelectedTodoId();
            if (string.IsNullOrEmpty(todoId)) return;
            SyncUiToState();
            Tracker.PauseTodo(todoId);
            Tracker.SaveSnapshot();
            RefreshTodoList(true);
            OutputWindow.Log("[Tracker] TODO timer paused.");
            StatusBar.SetText("TODO timer paused.");
        }

        private void Button_ToggleTodoDone_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string todoId = GetSelectedTodoId();
            if (string.IsNullOrEmpty(todoId)) return;
            TrackerTodoItem todo = Tracker.Current.Todos.FirstOrDefault(t => t.Id == todoId);
            if (todo == null) return;
            SyncUiToState();
            Tracker.ToggleTodoDone(todoId, !todo.IsDone);
            Tracker.SaveSnapshot();
            RefreshTodoList(true);
            OutputWindow.Log("[Tracker] TODO status updated.");
            StatusBar.SetText("TODO status updated.");
        }

        private void Button_GenerateCommitMessage_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SyncUiToState();
            TxtCommitMessage.Text = Tracker.GenerateCommitMessage();
            Tracker.SaveSnapshot();
            OutputWindow.Log("[Tracker] Commit message regenerated.");
            StatusBar.SetText("Commit message regenerated.");
        }

        private void Button_SaveSnapshot_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SyncUiToState();
            Tracker.SaveSnapshot();
            OutputWindow.Log("[Tracker] Snapshot saved.");
            StatusBar.SetText("Snapshot saved.");
        }

        private void Button_CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SyncUiToState();
            string reportPath = Tracker.CompleteTask();
            LoadFromState();
            OutputWindow.Log("[Tracker] Task completed and archived.");
            StatusBar.SetText("Task completed and archived.");
            MessageBox.Show("Đã lưu báo cáo hoàn thành tại:\n" + reportPath, "DH Codetask", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LstTodos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private sealed class TodoListRow
        {
            public string Id { get; set; }
            public string Title { get; set; }
        }
    }
}
