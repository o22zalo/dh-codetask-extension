using System;

namespace DhCodetaskExtension.Core.Interfaces
{
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent eventArgs) where TEvent : EventArgs;
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs;
    }
}
