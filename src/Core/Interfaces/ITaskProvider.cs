using System.Threading;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Models;

namespace DhCodetaskExtension.Core.Interfaces
{
    public interface ITaskProvider
    {
        bool CanHandle(string url);
        Task<TaskFetchResult> FetchAsync(string url, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class TaskFetchResult
    {
        public TaskItem Task { get; set; }
        public string ErrorMessage { get; set; }
        public bool Success => Task != null && string.IsNullOrEmpty(ErrorMessage);

        public static TaskFetchResult Ok(TaskItem task) => new TaskFetchResult { Task = task };
        public static TaskFetchResult Fail(string error) => new TaskFetchResult { ErrorMessage = error };
    }
}
