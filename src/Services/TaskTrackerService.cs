using System;
using System.IO;
using System.Linq;
using System.Text;
using DhCodetaskExtension.Models;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace DhCodetaskExtension.Services
{
    public sealed class TaskTrackerService
    {
        private const string AppDataFolderName = "DhCodetaskExtension";
        private const string CurrentTaskFileName = "current-task.json";

        private readonly AsyncPackage _package;
        private readonly OutputWindowService _outputWindow;
        private string _storageFolder;
        private string _currentTaskFilePath;

        public TrackerTaskState Current { get; private set; }

        public TaskTrackerService(AsyncPackage package, OutputWindowService outputWindow)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _outputWindow = outputWindow ?? throw new ArgumentNullException(nameof(outputWindow));
        }

        public async Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _storageFolder = Path.Combine(appData, AppDataFolderName);
            Directory.CreateDirectory(_storageFolder);
            _currentTaskFilePath = Path.Combine(_storageFolder, CurrentTaskFileName);
            Current = LoadCurrentTask();
            _outputWindow.Log("[Tracker] Initialized.");
        }

        public string CurrentTaskFilePath => _currentTaskFilePath ?? "(not initialized)";

        public void UpdateTaskDetails(string taskUrl, string taskId, string title, string labels, string description, string workNotes, string businessLogic, string commitMessage)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureCurrent();
            Current.TaskUrl = taskUrl ?? string.Empty;
            Current.TaskId = taskId ?? string.Empty;
            Current.TaskTitle = title ?? string.Empty;
            Current.Labels = labels ?? string.Empty;
            Current.Description = description ?? string.Empty;
            Current.WorkNotes = workNotes ?? string.Empty;
            Current.BusinessLogic = businessLogic ?? string.Empty;
            Current.CommitMessage = commitMessage ?? string.Empty;
        }

        public void StartTask()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureCurrent();
            if (HasOpenSession(Current)) return;
            DateTime now = DateTime.UtcNow;
            if (!Current.StartedAtUtc.HasValue)
                Current.StartedAtUtc = now;
            Current.IsPaused = false;
            Current.IsStopped = false;
            Current.Sessions.Add(new TrackerSession { StartedAtUtc = now });
        }

        public void PauseTask()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            CloseOpenSession(Current);
            foreach (TrackerTodoItem todo in Current.Todos)
                CloseOpenSession(todo);
            Current.IsPaused = true;
        }

        public void ResumeTask()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Current.IsStopped) return;
            StartTask();
        }

        public void StopTask()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            PauseTask();
            Current.IsStopped = true;
        }

        public TrackerTodoItem AddTodo(string title)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureCurrent();
            var todo = new TrackerTodoItem { Id = Guid.NewGuid().ToString("N"), Title = title ?? string.Empty };
            Current.Todos.Add(todo);
            return todo;
        }

        public void ToggleTodoDone(string todoId, bool isDone)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            TrackerTodoItem todo = FindTodo(todoId);
            if (todo == null) return;
            if (isDone)
                CloseOpenSession(todo);
            todo.IsDone = isDone;
        }

        public void StartTodo(string todoId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            TrackerTodoItem todo = FindTodo(todoId);
            if (todo == null || todo.IsDone) return;
            foreach (TrackerTodoItem item in Current.Todos.Where(t => t.Id != todoId))
                CloseOpenSession(item);
            if (!HasOpenSession(Current))
                StartTask();
            if (!HasOpenSession(todo))
                todo.Sessions.Add(new TrackerSession { StartedAtUtc = DateTime.UtcNow });
        }

        public void PauseTodo(string todoId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            TrackerTodoItem todo = FindTodo(todoId);
            if (todo == null) return;
            CloseOpenSession(todo);
        }

        public string GenerateCommitMessage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureCurrent();
            string scope = string.IsNullOrWhiteSpace(Current.Labels)
                ? "tracker"
                : Current.Labels.Split(',').Select(x => x.Trim()).FirstOrDefault(x => x.Length > 0) ?? "tracker";
            string title = string.IsNullOrWhiteSpace(Current.TaskTitle) ? "update-task" : Slugify(Current.TaskTitle);
            string taskPrefix = string.IsNullOrWhiteSpace(Current.TaskId) ? string.Empty : "#" + Current.TaskId + " ";
            Current.CommitMessage = "feat(" + scope.ToLowerInvariant() + "): " + taskPrefix + title;
            return Current.CommitMessage;
        }

        public void SaveSnapshot()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureCurrent();
            WriteAtomically(_currentTaskFilePath, JsonConvert.SerializeObject(Current, Formatting.Indented));
            _outputWindow.Log("[Tracker] Saved current task snapshot.");
        }

        public string CompleteTask()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnsureCurrent();
            PauseTask();
            DateTime completedAt = DateTime.UtcNow;
            Current.CompletedAtUtc = completedAt;
            string reportFolder = Path.Combine(_storageFolder, "history", completedAt.ToString("yyyy"), completedAt.ToString("MM"));
            Directory.CreateDirectory(reportFolder);
            var report = new CompletionReport
            {
                ReportId = Current.ReportId,
                TaskId = Current.TaskId,
                TaskTitle = Current.TaskTitle,
                TaskUrl = Current.TaskUrl,
                Labels = Current.Labels,
                Description = Current.Description,
                WorkNotes = Current.WorkNotes,
                BusinessLogic = Current.BusinessLogic,
                StartedAtUtc = Current.StartedAtUtc ?? Current.CreatedAtUtc,
                CompletedAtUtc = completedAt,
                TotalElapsed = FormatElapsed(Current.GetTaskElapsed(completedAt)),
                Sessions = Current.Sessions,
                Todos = Current.Todos,
                CommitMessage = Current.CommitMessage,
                GitBranch = Current.GitBranch,
                GitCommitHash = Current.GitCommitHash,
                WasPushed = Current.WasPushed,
            };

            string fileSafeTaskId = string.IsNullOrWhiteSpace(Current.TaskId) ? "manual" : Slugify(Current.TaskId);
            string baseName = completedAt.ToString("yyyyMMdd-HHmmss") + "-" + fileSafeTaskId;
            string jsonPath = Path.Combine(reportFolder, baseName + ".json");
            string mdPath = Path.Combine(reportFolder, baseName + ".md");

            WriteAtomically(jsonPath, JsonConvert.SerializeObject(report, Formatting.Indented));
            WriteAtomically(mdPath, BuildMarkdownReport(report));
            if (File.Exists(_currentTaskFilePath))
                File.Delete(_currentTaskFilePath);

            _outputWindow.Log("[Tracker] Completion report saved: " + jsonPath);
            Current = new TrackerTaskState();
            return mdPath;
        }

        private TrackerTaskState LoadCurrentTask()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!File.Exists(_currentTaskFilePath))
                return new TrackerTaskState();
            try
            {
                return JsonConvert.DeserializeObject<TrackerTaskState>(File.ReadAllText(_currentTaskFilePath, Encoding.UTF8)) ?? new TrackerTaskState();
            }
            catch (Exception ex)
            {
                _outputWindow.Log("[Tracker] Failed to load snapshot: " + ex.Message);
                return new TrackerTaskState();
            }
        }

        private static void WriteAtomically(string path, string content)
        {
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(tempPath, path);
        }

        private static string BuildMarkdownReport(CompletionReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ✅ HOÀN THÀNH — #" + (report.TaskId ?? "manual") + ": " + (report.TaskTitle ?? "Untitled task"));
            sb.AppendLine();
            sb.AppendLine("## ⏱ Thời gian");
            sb.AppendLine("- Bắt đầu: " + report.StartedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("- Hoàn thành: " + report.CompletedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("- Tổng thời gian: " + report.TotalElapsed);
            sb.AppendLine();
            sb.AppendLine("## 📝 Nội dung");
            sb.AppendLine("- URL: " + (report.TaskUrl ?? string.Empty));
            sb.AppendLine("- Labels: " + (report.Labels ?? string.Empty));
            sb.AppendLine();
            sb.AppendLine("### Description");
            sb.AppendLine(report.Description ?? string.Empty);
            sb.AppendLine();
            sb.AppendLine("### Work Notes");
            sb.AppendLine(report.WorkNotes ?? string.Empty);
            sb.AppendLine();
            sb.AppendLine("### Business Logic");
            sb.AppendLine(report.BusinessLogic ?? string.Empty);
            sb.AppendLine();
            sb.AppendLine("## ✅ Danh sách TODO");
            foreach (TrackerTodoItem todo in report.Todos ?? new System.Collections.Generic.List<TrackerTodoItem>())
                sb.AppendLine("- [" + (todo.IsDone ? "x" : " ") + "] " + todo.Title + " (" + FormatElapsed(todo.GetElapsed(report.CompletedAtUtc)) + ")");
            sb.AppendLine();
            sb.AppendLine("## 🔀 Commit");
            sb.AppendLine(report.CommitMessage ?? string.Empty);
            sb.AppendLine("Branch: " + (report.GitBranch ?? string.Empty) + " | Hash: " + (report.GitCommitHash ?? string.Empty) + " | " + (report.WasPushed ? "✅ Pushed" : "⏸ Not pushed"));
            return sb.ToString();
        }

        private void EnsureCurrent()
        {
            if (Current == null)
                Current = new TrackerTaskState();
        }

        private TrackerTodoItem FindTodo(string todoId)
        {
            EnsureCurrent();
            return Current.Todos.FirstOrDefault(t => string.Equals(t.Id, todoId, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasOpenSession(TrackerTaskState state)
        {
            return state != null && state.Sessions.Any(s => !s.EndedAtUtc.HasValue);
        }

        private static bool HasOpenSession(TrackerTodoItem todo)
        {
            return todo != null && todo.Sessions.Any(s => !s.EndedAtUtc.HasValue);
        }

        private static void CloseOpenSession(TrackerTaskState state)
        {
            if (state == null) return;
            DateTime now = DateTime.UtcNow;
            foreach (TrackerSession session in state.Sessions.Where(s => !s.EndedAtUtc.HasValue))
                session.EndedAtUtc = now;
        }

        private static void CloseOpenSession(TrackerTodoItem todo)
        {
            if (todo == null) return;
            DateTime now = DateTime.UtcNow;
            foreach (TrackerSession session in todo.Sessions.Where(s => !s.EndedAtUtc.HasValue))
                session.EndedAtUtc = now;
        }

        public static string FormatElapsed(TimeSpan elapsed)
        {
            return elapsed.ToString(@"hh\:mm\:ss");
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "update-task";
            var builder = new StringBuilder();
            bool wasDash = false;
            foreach (char c in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                    wasDash = false;
                }
                else if (!wasDash)
                {
                    builder.Append('-');
                    wasDash = true;
                }
            }
            return builder.ToString().Trim('-');
        }
    }
}
