using Esatto.VirtualPrinter;
using Esatto.VirtualPrinter.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Esatto.VirtualPrinter.Redirector
{
    using static Win32.Com.NativeMethods;

    internal sealed class ConnectedDispatcherCollection : IDisposable
    {
        private readonly object syncList = new object();
        private readonly List<ConnectedDispatcher> Dispatchers;

        private bool isInCleanUp;
        private readonly Timer tmrCleanUp;

        public ConnectedDispatcherCollection()
        {
            this.Dispatchers = new List<ConnectedDispatcher>();
            this.tmrCleanUp = new Timer(this.tmrCleanUp_Tick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void AddClient(IPrintDispatcher dispatcher)
        {
            var newClient = new ConnectedDispatcher(dispatcher);
            lock (syncList)
            {
                if (this.Dispatchers.Contains(newClient))
                {
                    throw new InvalidOperationException($"Duplicate registration for {newClient.UserSid}:{newClient.SessionId}");
                }

                this.Dispatchers.Add(newClient);
                this.ReevaluateCleanupTimer();
            }
        }

        public void Dispose()
        {
            this.tmrCleanUp.Dispose();
        }

        public ConnectedDispatcher GetClient(PrintJob job)
        {
            lock (syncList)
            {
                var client = this.Dispatchers.FirstOrDefault(d => d.Matches(job));
                if (client == null)
                {
                    return null;
                }

                try
                {
                    client.Ping();
                }
                catch (COMException)
                {
                    this.Dispatchers.Remove(client);
                    this.ReevaluateCleanupTimer();

                    return null;
                }

                return client;
            }
        }

        private void ReevaluateCleanupTimer()
        {
            lock (syncList)
            {
                if (this.isInCleanUp)
                {
                    return;
                }

                if (this.Dispatchers.Any())
                {
                    this.tmrCleanUp.Change(IpcConstants.DispatcherPingInterval, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    this.tmrCleanUp.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        public void RemoveClient(IPrintDispatcher dispatcher)
        {
            lock (syncList)
            {
                var client = this.Dispatchers.FirstOrDefault(d => d.Matches(dispatcher));
                if (client == null)
                {
                    throw new InvalidOperationException("Double free for dispatcher");
                }

                this.Dispatchers.Remove(client);
                this.ReevaluateCleanupTimer();
            }
        }

        private void tmrCleanUp_Tick(object state)
        {
            IEnumerable<ConnectedDispatcher> connected;
            lock (syncList)
            {
                if (this.isInCleanUp)
                {
                    return;
                }
                this.isInCleanUp = true;

                connected = this.Dispatchers.ToArray();
            }

            try
            {
                foreach (var client in connected)
                {
                    try
                    {
                        client.Ping();
                    }
                    catch (COMException ex) when ((ex.HResult == RPC_S_SERVER_UNAVAILABLE))
                    {
                        Log.Debug("Client disconnected without unregistering, collecting...");

                        lock (syncList)
                        {
                            this.Dispatchers.Remove(client);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception while cleaning up connected dispatchers\r\n{ex}", 195);
            }
            finally
            {
                lock (syncList)
                {
                    this.isInCleanUp = false;
                    this.ReevaluateCleanupTimer();
                }
            }
        }
    }
}

