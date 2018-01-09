using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveIRC {
    public sealed class SocketMessageSource : ISubject<IRCMessage>, IDisposable {
        private readonly TcpClient tcpClient;
        private StreamReader reader;
        private StreamWriter writer;
        private IObservable<IRCMessage> incoming;
        private IObserver<IRCMessage> outgoing;
        private int disposed;

        public SocketMessageSource() {
            tcpClient = new TcpClient();
        }

        public async Task ConnectAsync(string hostname, int port, bool useSsl) {
            EnsureNotDisposed();

            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            await tcpClient.ConnectAsync(hostname, port);

            Stream stream = tcpClient.GetStream();
            if(useSsl) {
                var sslStream = new SslStream(stream);
                await sslStream.AuthenticateAsClientAsync(hostname);
                stream = sslStream;
            }
            reader = new StreamReader(stream, encoding);
            writer = new StreamWriter(stream, encoding) { AutoFlush = true };

            incoming = Observable.Create<IRCMessage>(observer => {
                return TaskPoolScheduler.Default.ScheduleAsync(async (_, cancellationToken) => {
                    while(!cancellationToken.IsCancellationRequested) {
                        observer.OnNext(IRCMessage.Parse(await reader.ReadLineAsync()));
                    }

                    observer.OnCompleted();
                });
            }).Publish().RefCount();

            outgoing = Observer
                .Create<IRCMessage>(msg => writer.Write(msg.ToString() + "\r\n"))
                .NotifyOn(TaskPoolScheduler.Default);
        }

        public void OnCompleted() {
            EnsureNotDisposed();
            outgoing.OnCompleted();
        }

        public void OnError(Exception error) {
            EnsureNotDisposed();
            outgoing.OnError(error);
        }

        public void OnNext(IRCMessage value) {
            EnsureNotDisposed();
            outgoing.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<IRCMessage> observer) {
            EnsureNotDisposed();
            return incoming.Subscribe(observer);
        }

        public void Dispose() {
            if(Interlocked.Exchange(ref disposed, 1) == 0) {
                tcpClient.Dispose();
                reader?.Dispose();
                writer?.Dispose();
            }
        }

        private void EnsureNotDisposed() {
            if(disposed == 1) {
                throw new ObjectDisposedException(null);
            }
        }
    }
}
