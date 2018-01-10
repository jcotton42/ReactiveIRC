using System;

namespace ReactiveIRC.Handlers {
    public interface IIRCMessageHandler {
        void HandleIncoming(IRCClient client, IRCMessage message);
        void HandleOutgoing(IRCClient client, IRCMessage message);
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class IRCMessageHandlerAttribute : Attribute {
        public string Verb { get; }

        public IRCMessageHandlerAttribute(string verb) {
            Verb = verb ?? throw new ArgumentNullException(nameof(verb));
        }
    }
}
