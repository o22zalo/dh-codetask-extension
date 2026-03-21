using System;
using System.Collections.Generic;

namespace DhCodetaskExtension.Models
{
    public sealed class TrackerSession
    {
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }

        public TimeSpan GetElapsed(DateTime utcNow)
        {
            DateTime end = EndedAtUtc ?? utcNow;
            return end > StartedAtUtc ? end - StartedAtUtc : TimeSpan.Zero;
        }
    }

    public sealed class TrackerTodoItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public bool IsDone { get; set; }
        public List<TrackerSession> Sessions { get; set; }

        public TrackerTodoItem()
        {
            Sessions = new List<TrackerSession>();
        }

        public TimeSpan GetElapsed(DateTime utcNow)
        {
            TimeSpan total = TimeSpan.Zero;
            foreach (TrackerSession session in Sessions)
                total += session.GetElapsed(utcNow);
            return total;
        }
    }

    public sealed class TrackerTaskState
    {
        public string ReportId { get; set; }
        public string TaskId { get; set; }
        public string TaskUrl { get; set; }
        public string TaskTitle { get; set; }
        public string Labels { get; set; }
        public string Description { get; set; }
        public string WorkNotes { get; set; }
        public string BusinessLogic { get; set; }
        public string CommitMessage { get; set; }
        public string GitBranch { get; set; }
        public string GitCommitHash { get; set; }
        public bool WasPushed { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public bool IsPaused { get; set; }
        public bool IsStopped { get; set; }
        public List<TrackerSession> Sessions { get; set; }
        public List<TrackerTodoItem> Todos { get; set; }

        public TrackerTaskState()
        {
            ReportId = Guid.NewGuid().ToString("N");
            CreatedAtUtc = DateTime.UtcNow;
            Sessions = new List<TrackerSession>();
            Todos = new List<TrackerTodoItem>();
            WorkNotes = string.Empty;
            BusinessLogic = string.Empty;
            Description = string.Empty;
            Labels = string.Empty;
            TaskTitle = string.Empty;
            TaskId = string.Empty;
            TaskUrl = string.Empty;
            CommitMessage = string.Empty;
            GitBranch = string.Empty;
            GitCommitHash = string.Empty;
        }

        public TimeSpan GetTaskElapsed(DateTime utcNow)
        {
            TimeSpan total = TimeSpan.Zero;
            foreach (TrackerSession session in Sessions)
                total += session.GetElapsed(utcNow);
            return total;
        }
    }

    public sealed class CompletionReport
    {
        public string ReportId { get; set; }
        public string TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskUrl { get; set; }
        public string Labels { get; set; }
        public string Description { get; set; }
        public string WorkNotes { get; set; }
        public string BusinessLogic { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public string TotalElapsed { get; set; }
        public List<TrackerSession> Sessions { get; set; }
        public List<TrackerTodoItem> Todos { get; set; }
        public string CommitMessage { get; set; }
        public string GitBranch { get; set; }
        public string GitCommitHash { get; set; }
        public bool WasPushed { get; set; }
    }
}
