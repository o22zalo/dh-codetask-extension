using System;
using System.Collections.Generic;

namespace DhCodetaskExtension.Core.Models
{
    public class WorkLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TaskItem Task { get; set; }
        public List<TimeSession> Sessions { get; set; } = new List<TimeSession>();
        public List<TodoItem> Todos { get; set; } = new List<TodoItem>();
        public string WorkNotes { get; set; } = string.Empty;
        public string BusinessLogic { get; set; } = string.Empty;
        public string CommitMessage { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public string GitBranch { get; set; } = string.Empty;
        public string RepoRoot { get; set; } = string.Empty;

        // State for restore
        public string TimerState { get; set; } = "Idle";
    }
}
