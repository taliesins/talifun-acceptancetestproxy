using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Talifun.AcceptanceTestProxy.Connections
{
    public class PortListener
    {
        public PortListener(TcpListener listener, Func<TcpClient, CancellationToken, Task> connectionAccepted)
        {
            _listener = listener;
            _connectionAccepted = connectionAccepted;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly TcpListener _listener;
        private readonly Func<TcpClient, CancellationToken, Task> _connectionAccepted;

        public TcpListener Listener
        {
            get { return _listener; }
        }

        public void Start()
        {
            _listener.Start();
            Accept(_listener);
        }

        public void Stop()
        { 
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }

        private Task Accept(TcpListener listener)
        {
            var cancellationToken = _cancellationTokenSource.Token;

            return Task.Factory
                .FromAsync<TcpClient>(listener.BeginAcceptTcpClient, listener.EndAcceptTcpClient, listener)
                .ContinueWith(tc =>
                                  {
                                      if (!cancellationToken.IsCancellationRequested)
                                      {
                                          // Restart task by calling AcceptConnection "recursively".
                                          // Note, this is called from the thread pool. So no stack overflows.
                                          Accept((TcpListener)tc.AsyncState);
                                      }
                                      return _connectionAccepted(tc.Result, cancellationToken);
                                  }, cancellationToken)
                .Unwrap();
        }
    }
}
