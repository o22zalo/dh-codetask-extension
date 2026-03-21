using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using DhCodetaskExtension.Core.Events;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;
using DhCodetaskExtension.Core.Services;

namespace DhCodetaskExtension.ViewModels
{
    public sealed class TodoItemViewModel : INotifyPropertyChanged
    {
        private readonly IEventBus _eventBus;
        private readonly string _taskId;
        private DispatcherTimer _timer;
        private TimeSession _currentSession;

        public TodoItem Model { get; }

        public string Text
        {
            get => Model.Text;
            set { Model.Text = value; OnPropertyChanged(nameof(Text)); }
        }

        public bool IsDone
        {
            get => Model.IsDone;
            set { Model.IsDone = value; OnPropertyChanged(nameof(IsDone)); }
        }

        public TodoStatus Status => Model.Status;

        private string _elapsedDisplay = "00:00:00";
        public string ElapsedDisplay
        {
            get => _elapsedDisplay;
            private set { _elapsedDisplay = value; OnPropertyChanged(nameof(ElapsedDisplay)); }
        }

        public bool IsRunning => Model.Status == TodoStatus.Running;
        public bool IsPaused => Model.Status == TodoStatus.Paused;
        public bool IsIdle => Model.Status == TodoStatus.Idle;
        public bool IsComplete => Model.Status == TodoStatus.Done;
        public bool CanStart => Model.Status == TodoStatus.Idle || Model.Status == TodoStatus.Paused;
        public bool CanPause => Model.Status == TodoStatus.Running;

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand DeleteCommand { get; }

        public event Action<TodoItemViewModel> DeleteRequested;

        public TodoItemViewModel(TodoItem model, IEventBus eventBus, string taskId)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _eventBus = eventBus;
            _taskId = taskId;

            StartCommand = new RelayCommand(StartTodo, () => CanStart);
            PauseCommand = new RelayCommand(PauseTodo, () => CanPause);
            CompleteCommand = new RelayCommand(CompleteTodo, () => !IsComplete);
            DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this));

            UpdateElapsed();
        }

        public void StartTodo()
        {
            if (!CanStart) return;
            _currentSession = new TimeSession { StartTime = DateTime.UtcNow };
            Model.Sessions.Add(_currentSession);
            Model.Status = TodoStatus.Running;
            RaiseStateChanged();
            StartTimer();
            _eventBus?.Publish(new TodoStartedEvent { Todo = Model, TaskId = _taskId });
        }

        public void PauseTodo()
        {
            if (!CanPause) return;
            FinalizeSession();
            Model.Status = TodoStatus.Paused;
            StopTimer();
            RaiseStateChanged();
            _eventBus?.Publish(new TodoPausedEvent { Todo = Model });
        }

        public void CompleteTodo()
        {
            if (Model.Status == TodoStatus.Running) FinalizeSession();
            Model.IsDone = true;
            Model.Status = TodoStatus.Done;
            StopTimer();
            RaiseStateChanged();
            _eventBus?.Publish(new TodoCompletedEvent { Todo = Model, Elapsed = Model.TotalElapsed });
        }

        public void AutoPause()
        {
            if (Model.Status == TodoStatus.Running) PauseTodo();
        }

        private void FinalizeSession()
        {
            if (_currentSession != null)
            {
                _currentSession.EndTime = DateTime.UtcNow;
                _currentSession = null;
            }
        }

        private void StartTimer()
        {
            StopTimer();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => UpdateElapsed();
            _timer.Start();
        }

        private void StopTimer() { _timer?.Stop(); _timer = null; }

        private void UpdateElapsed()
        {
            var ts = Model.TotalElapsed;
            if (Model.Status == TodoStatus.Running && _currentSession != null)
                ts += DateTime.UtcNow - _currentSession.StartTime;
            ElapsedDisplay = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private void RaiseStateChanged()
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(IsComplete));
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanPause));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
