using Esatto.VirtualPrinter.IPC;
using Esatto.Win32.Com;
using Esatto.Win32.Processes;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Esatto.VirtualPrinter.Redirector
{
    internal sealed class ConnectedDispatcher
    {
        private readonly IPrintDispatcher Dispatcher;

        public ConnectedDispatcher(IPrintDispatcher dispatcher)
        {
            Contract.Requires(dispatcher != null, nameof(dispatcher));

            this.Dispatcher = dispatcher;

            this.InitializeFromClient();
        }

        internal void HandleJob(PrintJob job)
        {
            this.Dispatcher.HandleJob(job);
        }

        private void InitializeFromClient()
        {
            ComInterop.RunImpersonated(() =>
            {
                var ident = WindowsIdentity.GetCurrent(true);

                this.UserSid = ident.User.ToString();
                this.SessionId = ident.GetSessionId();
            });
        }

        internal bool Matches(IPrintDispatcher dispatcher) => this.Dispatcher == dispatcher;

        internal bool Matches(PrintJob job) => this.UserSid == job.UserSid && this.SessionId == job.SessionId;

        internal void Ping() => this.Dispatcher.Ping();

        public int SessionId { get; private set; }

        public string UserSid { get; private set; }
    }
}

