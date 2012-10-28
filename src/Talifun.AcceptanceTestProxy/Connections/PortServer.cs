using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Talifun.AcceptanceTestProxy.Connections
{
    public class PortServer
    {
        private readonly static Object PortListenersLock = new object();
        private readonly ConcurrentDictionary<string, PortListener> _portListeners = new ConcurrentDictionary<string, PortListener>();
        private readonly Func<TcpClient, CancellationToken, Task> _connectionAccepted;
        private readonly IPAddress _ipAddress;
        private readonly int _startOfPortRange;

        public PortServer(Func<TcpClient, CancellationToken, Task> connectionAccepted, IPAddress ipAddress, int startOfPortRange)
        {
            _connectionAccepted = connectionAccepted;
            _ipAddress = ipAddress;
            _startOfPortRange = startOfPortRange;
        }

        public int Start(string name)
        {
            PortListener portListener;
            if (!_portListeners.TryGetValue(name, out portListener))
            {
                lock (PortListenersLock)
                {
                    if (!_portListeners.TryGetValue(name, out portListener))
                    {
                        var listener = GetNextServerPort(_ipAddress, _startOfPortRange);
                        portListener = new PortListener(listener, _connectionAccepted);
                        _portListeners.TryAdd(name, portListener);
                        portListener.Start();
                    }
                }
            }

            return ((IPEndPoint) portListener.Listener.LocalEndpoint).Port;
        }

        public void Stop(string name)
        {
             PortListener portListener;
             if (_portListeners.TryRemove(name, out portListener))
             {
                 portListener.Stop();
             }
        }

        public void StopAll()
        {
            foreach (var portListenerKey in _portListeners.Keys)
            {
                Stop(portListenerKey);
            }
        }

        private TcpListener GetNextServerPort(IPAddress ipAddress, int startOfPortRange)
        {
            const int portInUse = 10061;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            var usedPorts = tcpConnInfoArray.Select(x => x.Port).Where(x => x >= startOfPortRange).OrderBy(x => x).ToList();
            var openPort = usedPorts.Any() && usedPorts.First() < startOfPortRange ? usedPorts.First() : startOfPortRange;

            TcpListener listener = null;
            do
            {
                if (usedPorts.Contains(openPort))
                {
                    openPort++;
                    continue;
                }

                try
                {
                    listener = new TcpListener(ipAddress, openPort);
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode != portInUse)
                    {
                        throw;
                    }
                    //Port is unused
                    openPort++;
                }
            } while (listener == null);

            return listener;
        }
    }
}
