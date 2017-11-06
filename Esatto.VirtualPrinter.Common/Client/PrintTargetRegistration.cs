using Esatto.VirtualPrinter.IPC;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Esatto.VirtualPrinter
{
    public sealed class PrintTargetRegistration : IDisposable
    {
        private readonly IPrintDispatcher Dispatcher;
        private readonly Action<PrintJob> Handler;
        private readonly PrintTargetProxy Proxy;
        private readonly SynchronizationContext SyncCtx;

        public PrintTargetRegistration(string queueName, Action<PrintJob> handler, SynchronizationContext syncCtx = null)
        {
            Contract.Requires(handler != null, "handler != null");

            this.SyncCtx = syncCtx ?? new SynchronizationContext();
            this.Handler = handler;

            this.Dispatcher = SessionPrintDispatcher.GetCurrent();
            Contract.Assert(this.Dispatcher != null, "Dispatcher != null");

            this.Proxy = new PrintTargetProxy(this);
            this.Dispatcher.SetTarget(queueName, this.Proxy);
        }

        public void Dispose()
        {
            this.Proxy.Disconnect();
        }

        private void OnHandleJob(PrintJob job)
        {
            this.SyncCtx.Post(_1 =>
            {
                try
                {
                    this.Handler(job);
                }
                catch (Exception exception)
                {
                    Log.Error($"Exception while handling job:\r\n\r\n{exception}", 124);
                }
            }, null);
        }

        private sealed class PrintTargetProxy : IPrintTarget
        {
            private PrintTargetRegistration Parent;
            private readonly object syncDisconnect = new object();

            public PrintTargetProxy(PrintTargetRegistration parent)
            {
                Contract.Requires(parent != null, "parent != null");

                this.Parent = parent;
            }

            public void Disconnect()
            {
                lock (syncDisconnect)
                {
                    if (this.Parent != null)
                    {
                        try
                        {
                            this.Parent.Dispatcher.UnsetTarget(this);
                        }
                        catch (Exception exception)
                        {
                            Log.Warn($"Failed to unset target:\r\n\r\n{exception}", 125);
                        }
                        this.Parent = null;
                    }
                }
            }

            public void HandleJob(PrintJob job)
            {
                lock (syncDisconnect)
                {
                    var parent = this.Parent;
                    if (parent == null)
                    {
                        throw new ObjectDisposedException("Target is disconnected");
                    }
                    parent.OnHandleJob(job);
                }
            }

            public void Ping()
            {
                // no-op
            }
        }
    }
}

