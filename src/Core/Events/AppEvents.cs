using System;
using DhCodetaskExtension.Core.Models;

namespace DhCodetaskExtension.Core.Events
{
    public class TaskFetchedEvent : EventArgs
    {
        public TaskItem Task { get; set; }
        public string Url { get; set; }
    }

    public class TaskStartedEvent : EventArgs
    {
        public WorkLog Log { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class TaskPausedEvent : EventArgs
    {
        public WorkLog Log { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    public class TaskResumedEvent : EventArgs
    {
        public WorkLog Log { get; set; }
    }

    public class TaskCompletedEvent : EventArgs
    {
        public CompletionReport Report { get; set; }
    }

    public class CommitPushedEvent : EventArgs
    {
        public string CommitMessage { get; set; }
        public bool Success { get; set; }
        public string Hash { get; set; }
        public string Error { get; set; }
    }

    public class ReportSavedEvent : EventArgs
    {
        public string FilePath { get; set; }
        public CompletionReport Report { get; set; }
    }

    public class TodoStartedEvent : EventArgs
    {
        public TodoItem Todo { get; set; }
        public string TaskId { get; set; }
    }

    public class TodoPausedEvent : EventArgs
    {
        public TodoItem Todo { get; set; }
    }

    public class TodoCompletedEvent : EventArgs
    {
        public TodoItem Todo { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
