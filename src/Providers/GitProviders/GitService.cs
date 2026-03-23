using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.Core.Services;

namespace DhCodetaskExtension.Providers.GitProviders
{
    internal sealed class GitRunResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public bool TimedOut { get; set; }
    }

    public sealed class GitService : IGitService
    {
        private const int GitTimeoutMs = 30000;
        private readonly Func<AppSettings> _settingsProvider;

        public GitService(Func<AppSettings> settingsProvider)
        {
            _settingsProvider = settingsProvider
                ?? throw new ArgumentNullException(nameof(settingsProvider));
        }

        public bool IsAvailable()
        {
            try
            {
                var r = RunGit(".", "--version");
                if (r.TimedOut)
                    AppLogger.Instance.Warn("[Git] git --version timed out while checking availability.");
                return r.ExitCode == 0 && !r.TimedOut;
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Warn("[Git] IsAvailable failed: " + ex.Message);
                return false;
            }
        }

        public string FindRepoRoot(string startPath)
        {
            if (string.IsNullOrEmpty(startPath)) return null;
            var dir = Directory.Exists(startPath) ? startPath : Path.GetDirectoryName(startPath);
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(Path.Combine(dir, ".git"))) return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        public async Task<string> GetCurrentBranchAsync(string repoRoot)
        {
            if (string.IsNullOrEmpty(repoRoot)) return "unknown";
            var r = await Task.Run(() => RunGit(repoRoot, "rev-parse --abbrev-ref HEAD"));
            if (r.TimedOut)
            {
                AppLogger.Instance.Warn("[Git] GetCurrentBranch timed out.");
                return "unknown";
            }
            return r.ExitCode == 0 ? (r.Output ?? string.Empty).Trim() : "unknown";
        }

        public async Task<GitResult> PushAndCompleteAsync(string repoRoot, string commitMessage, bool autoPush)
        {
            if (string.IsNullOrEmpty(repoRoot))
                return new GitResult { Error = "Repository root not found." };

            try
            {
                AppLogger.Instance.Info(string.Format("[Git] Starting PushAndComplete. Repo={0}, AutoPush={1}", repoRoot, autoPush));
                var settings = _settingsProvider();

                if (!string.IsNullOrEmpty(settings.GitUserName))
                    await RunGitAndLogAsync(repoRoot,
                        string.Format("config user.name \"{0}\"", settings.GitUserName),
                        "git config user.name");
                if (!string.IsNullOrEmpty(settings.GitUserEmail))
                    await RunGitAndLogAsync(repoRoot,
                        string.Format("config user.email \"{0}\"", settings.GitUserEmail),
                        "git config user.email");

                var addResult = await RunGitAndLogAsync(repoRoot, "add -A", "git add -A");
                if (addResult.ExitCode != 0)
                    return new GitResult { Error = "git add failed: " + BuildError(addResult) };

                var safeMsg = (commitMessage ?? string.Empty).Replace("\"", "'").Trim();
                if (string.IsNullOrEmpty(safeMsg)) safeMsg = "Update task progress";

                var commitResult = await RunGitAndLogAsync(repoRoot,
                    string.Format("commit -m \"{0}\"", safeMsg),
                    "git commit");
                if (commitResult.ExitCode != 0)
                    return new GitResult { Error = "git commit failed: " + BuildError(commitResult) };

                if (autoPush)
                {
                    var pushResult = await RunGitAndLogAsync(repoRoot, "push", "git push");
                    if (pushResult.ExitCode != 0)
                        return new GitResult
                        {
                            Error = "git push failed: " + BuildError(pushResult),
                            Output = commitResult.Output
                        };
                }

                var hashResult = await RunGitAndLogAsync(repoRoot, "rev-parse HEAD", "git rev-parse HEAD");
                var hash = hashResult.ExitCode == 0 ? (hashResult.Output ?? string.Empty).Trim() : string.Empty;
                if (hash.Length > 7) hash = hash.Substring(0, 7);

                AppLogger.Instance.Info("[Git] PushAndComplete completed successfully. Commit=" + hash);
                return new GitResult
                {
                    Success = true,
                    Output = commitResult.Output,
                    CommitHash = hash
                };
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("GitService.PushAndCompleteAsync", ex);
                return new GitResult { Error = ex.Message };
            }
        }

        private static async Task<GitRunResult> RunGitAndLogAsync(string workDir, string args, string operation)
        {
            var result = await Task.Run(() => RunGit(workDir, args));
            if (result.TimedOut)
                AppLogger.Instance.Warn(string.Format("[Git] {0} timed out after {1}ms.", operation, GitTimeoutMs));
            else if (result.ExitCode == 0)
                AppLogger.Instance.Info(string.Format("[Git] {0} succeeded. {1}", operation, BuildSummary(result.Output)));
            else
                AppLogger.Instance.Warn(string.Format("[Git] {0} failed (exit {1}). {2}", operation, result.ExitCode, BuildSummary(result.Error)));
            return result;
        }

        private static GitRunResult RunGit(string workDir, string args)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = workDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var p = new Process { StartInfo = psi })
            {
                p.Start();

                var outputTask = p.StandardOutput.ReadToEndAsync();
                var errorTask = p.StandardError.ReadToEndAsync();

                if (!p.WaitForExit(GitTimeoutMs))
                {
                    try { p.Kill(); }
                    catch { }

                    Task.WaitAll(new Task[] { outputTask, errorTask }, 1000);
                    return new GitRunResult
                    {
                        ExitCode = -1,
                        Output = outputTask.IsCompleted ? outputTask.Result : string.Empty,
                        Error = errorTask.IsCompleted ? errorTask.Result : string.Empty,
                        TimedOut = true
                    };
                }

                Task.WaitAll(outputTask, errorTask);
                return new GitRunResult
                {
                    ExitCode = p.ExitCode,
                    Output = outputTask.Result,
                    Error = errorTask.Result,
                    TimedOut = false
                };
            }
        }

        private static string BuildError(GitRunResult result)
        {
            if (result == null) return "Unknown git error.";
            if (result.TimedOut) return string.Format("timed out after {0}ms.", GitTimeoutMs);
            return string.IsNullOrWhiteSpace(result.Error) ? "Unknown git error." : result.Error.Trim();
        }

        private static string BuildSummary(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "No output.";
            var normalized = text.Replace("\r\n", " ").Replace('\n', ' ').Trim();
            return normalized.Length <= 160 ? normalized : normalized.Substring(0, 160) + "...";
        }
    }
}
