using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Models;

namespace DhCodetaskExtension.Core.Interfaces
{
    public interface IReportGenerator
    {
        Task<string> GenerateAsync(CompletionReport report, string outputDirectory);
    }

    public interface IGitService
    {
        bool IsAvailable();
        string FindRepoRoot(string startPath);
        Task<string> GetCurrentBranchAsync(string repoRoot);
        Task<GitResult> PushAndCompleteAsync(string repoRoot, string commitMessage, bool autoPush);
    }

    public class GitResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string CommitHash { get; set; }
        public string Error { get; set; }
    }

    public interface IStorageService
    {
        string GetHistoryDirectory();
        Task<WorkLog> LoadCurrentTaskAsync();
        Task SaveCurrentTaskAsync(WorkLog log);
        Task ClearCurrentTaskAsync();
        Task ArchiveReportAsync(CompletionReport report);
        Task<AppSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(AppSettings settings);
    }

    public interface IHistoryRepository
    {
        Task<IEnumerable<CompletionReport>> GetAllAsync();
        Task<IEnumerable<CompletionReport>> GetByDateRangeAsync(DateTime from, DateTime to);
        Task<IEnumerable<CompletionReport>> GetTodayAsync();
        Task<IEnumerable<CompletionReport>> GetThisWeekAsync();
        Task<IEnumerable<CompletionReport>> GetThisMonthAsync();
        Task DeleteAsync(string reportId);
    }

    public interface INotificationProvider
    {
        void OnEvent<TEvent>(TEvent eventArgs) where TEvent : EventArgs;
    }
}
