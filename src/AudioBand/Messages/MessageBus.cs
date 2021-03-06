using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AudioBand.Logging;
using NLog;

namespace AudioBand.Messages
{
    /// <summary>
    /// Implementation for the message bus.
    /// </summary>
    public class MessageBus : IMessageBus
    {
        private static readonly ILogger Logger = AudioBandLogManager.GetLogger<MessageBus>();
        private readonly Dictionary<Type, List<object>> _handlers = new Dictionary<Type, List<object>>();

        /// <inheritdoc />
        [DebuggerStepThrough]
        public void Subscribe<TMessage>(Action<TMessage> handler)
        {
            Logger.Trace("{class} registered new handler for {message}", handler.Target.GetType(), typeof(TMessage));

            if (_handlers.ContainsKey(typeof(TMessage)))
            {
                _handlers[typeof(TMessage)].Add(handler);
            }
            else
            {
                _handlers.Add(typeof(TMessage), new List<object> { handler });
            }
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public void Unsubscribe<TMessage>(Action<TMessage> handler)
        {
            Logger.Trace("{class} unsubscribed for {message}", handler.Target.GetType(), typeof(TMessage));

            if (_handlers.TryGetValue(typeof(TMessage), out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public void Publish<TMessage>(TMessage message, [CallerMemberName] string caller = "")
        {
            if (!_handlers.TryGetValue(typeof(TMessage), out var handlers))
            {
                return;
            }

            foreach (var handler in handlers.Cast<Action<TMessage>>())
            {
                Logger.Trace("Message published {caller} -> {info}", caller, new { Target = handler.Target.GetType(), Handler = handler.Method.Name, Message = message });
                handler(message);
            }
        }
    }
}
