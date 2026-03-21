using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DhCodetaskExtension.Core.Models
{
    public enum TodoStatus { Idle, Running, Paused, Done }

    public class TodoItem : INotifyPropertyChanged
    {
        private string _text;
        private bool _isDone;
        private TodoStatus _status;

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(nameof(Text)); }
        }

        public bool IsDone
        {
            get => _isDone;
            set { _isDone = value; OnPropertyChanged(nameof(IsDone)); }
        }

        public TodoStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public List<TimeSession> Sessions { get; set; } = new List<TimeSession>();

        public TimeSpan TotalElapsed => TimeSpan.FromSeconds(Sessions.Sum(s => s.ElapsedSeconds));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
