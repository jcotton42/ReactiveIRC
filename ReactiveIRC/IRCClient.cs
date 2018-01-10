using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using ReactiveIRC.Handlers;

namespace ReactiveIRC {
    public class IRCClient {
        private readonly IObservable<IRCMessage> incoming;
        private readonly IObserver<IRCMessage> outgoing;
        private static readonly Dictionary<string, List<IIRCMessageHandler>> messageHandlers;

        static IRCClient() {
            messageHandlers = new Dictionary<string, List<IIRCMessageHandler>>();
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetExportedTypes())
                .Where(typeof(IIRCMessageHandler).IsAssignableFrom);
            foreach(var type in types) {
                var attributes = type.GetCustomAttributes<IRCMessageHandlerAttribute>();
                if(attributes is null) {
                    throw new InvalidOperationException("TODO: need to log here.");
                }
                var instance = (IIRCMessageHandler)Activator.CreateInstance(type);
                foreach(var verb in attributes.Select(a => a.Verb)) {
                    if(!messageHandlers.ContainsKey(verb)) {
                        messageHandlers[verb] = new List<IIRCMessageHandler>();
                    }
                    messageHandlers[verb].Add(instance);
                }
            }
        }

        public IRCClient(IObservable<IRCMessage> incoming, IObserver<IRCMessage> outgoing) {
            this.incoming = incoming ?? throw new ArgumentNullException(nameof(incoming));
            this.outgoing = outgoing ?? throw new ArgumentNullException(nameof(outgoing));
        }

        public async Task RegisterAsync(string password, string nick, string username, string realname) {
            SendMessage("CAP", "LS", "302");
            if(!string.IsNullOrWhiteSpace(password)) {
                SendMessage("PASS", password);
            }
            SendMessage("NICK", nick);
            SendMessage("USER", username, realname);
        }

        public void SendMessage(string verb, params string[] parameters) {
            SendMessage(IRCMessage.Create(verb, parameters));
        }

        public void SendMessage(ImmutableDictionary<string, string> tags, string verb, params string[] parameters) {
            SendMessage(IRCMessage.Create(tags, verb, parameters));
        }

        public void SendMessage(IRCMessage message) {
            if(messageHandlers.TryGetValue(message.Verb, out var handlers)) {
                foreach(var handler in handlers) {
                    handler.HandleOutgoing(this, message);
                }
            }
            outgoing.OnNext(message);
        }
    }
}
