using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using DhCodetaskExtension.Core.Models;

namespace DhCodetaskExtension.Core.Services
{
    public static class CommitMessageGenerator
    {
        public static string Generate(TaskItem task, string businessLogic)
        {
            if (task == null) return string.Empty;

            string type = DetectType(task.Labels, task.Title);
            string scope = string.IsNullOrEmpty(task.Repo) ? "app" : task.Repo.ToLower();
            string slug = Slugify(task.Title);
            string line1 = $"{type}({scope}): {slug}";

            string business = string.Empty;
            if (!string.IsNullOrWhiteSpace(businessLogic))
            {
                var words = businessLogic.Trim().Split(new[] { ' ', '\n', '\r' },
                    StringSplitOptions.RemoveEmptyEntries);
                business = string.Join(" ", words.Take(15));
            }

            string refLine = string.IsNullOrEmpty(task.Id) ? string.Empty
                : $"Ref: #{task.Id}";

            var parts = new[] { line1, business, refLine }
                .Where(s => !string.IsNullOrEmpty(s));
            return string.Join("\n", parts);
        }

        private static string DetectType(string[] labels, string title)
        {
            var all = string.Join(" ", (labels ?? new string[0])).ToLower() + " " + (title ?? "").ToLower();
            if (all.Contains("bug") || all.Contains("fix") || all.Contains("hotfix")) return "fix";
            if (all.Contains("chore") || all.Contains("refactor") || all.Contains("clean")) return "chore";
            return "feat";
        }

        private static string Slugify(string title)
        {
            if (string.IsNullOrEmpty(title)) return "update";
            return Regex.Replace(title.ToLower().Trim(), @"[^a-z0-9]+", "-").Trim('-');
        }
    }

    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) =>
            _canExecute?.Invoke(parameter is T t ? t : default(T)) ?? true;

        public void Execute(object parameter) =>
            _execute(parameter is T t ? t : default(T));

        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
