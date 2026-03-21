using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Interfaces;

namespace DhCodetaskExtension.Core.Services
{
    /// <summary>
    /// In-process pub/sub bus. Publish is non-blocking — each subscriber runs on Task.Run.
    /// Subscriber exceptions do NOT propagate back to publisher.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers =
            new Dictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            lock (_lock)
            {
                var key = typeof(TEvent);
                if (!_handlers.ContainsKey(key))
                    _handlers[key] = new List<Delegate>();
                if (!_handlers[key].Contains(handler))
                    _handlers[key].Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs
        {
            if (handler == null) return;
            lock (_lock)
            {
                var key = typeof(TEvent);
                if (_handlers.ContainsKey(key))
                    _handlers[key].Remove(handler);
            }
        }

        public void Publish<TEvent>(TEvent eventArgs) where TEvent : EventArgs
        {
            List<Delegate> snapshot;
            lock (_lock)
            {
                var key = typeof(TEvent);
                if (!_handlers.ContainsKey(key) || _handlers[key].Count == 0) return;
                snapshot = new List<Delegate>(_handlers[key]);
            }

            foreach (var h in snapshot)
            {
                var typed = h as Action<TEvent>;
                if (typed == null) continue;
                var capture = typed;
                var args = eventArgs;
                Task.Run(() =>
                {
                    try { capture(args); }
                    catch { /* never crash publisher */ }
                });
            }
        }
    }
}
