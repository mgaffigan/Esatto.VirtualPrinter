using Esatto.VirtualPrinter;
using Esatto.VirtualPrinter.IPC;
using Esatto.Win32.Processes;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Esatto.VirtualPrinter.Redirector
{
    [ComVisible(true), ClassInterface(ClassInterfaceType.None), ProgId(IpcConstants.RedirectorProgId)]
    public sealed class PrintRedirector : IPrintRedirector, IDisposable
    {
        private CancellationTokenSource ctsShutdown = new CancellationTokenSource();
        private readonly object syncAddFindDispatcher = new object();
        private readonly object syncStartDispatcher = new object();

        private readonly string DispatcherPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DriverConstants.DispatcherFilename);
        private readonly ConnectedDispatcherCollection Dispatchers;

        public PrintRedirector()
        {
            if (!File.Exists(this.DispatcherPath))
            {
                throw new FileNotFoundException("Could not locate dispatcher", this.DispatcherPath);
            }
            this.Dispatchers = new ConnectedDispatcherCollection();
        }

        public void Dispose()
        {
            this.ctsShutdown.Cancel();
            lock (syncAddFindDispatcher)
            {
                Monitor.PulseAll(this.syncAddFindDispatcher);
            }
            this.Dispatchers.Dispose();
        }

        #region IPrintRedirector

        void IPrintRedirector.HandleJob(PrintJob job)
        {
            ThreadPool.QueueUserWorkItem((_1) =>
            {
                try
                {
                    this.HandleJobInternal(job);
                    Log.Debug($"Handled job {job.JobId} for {job.PrinterName} printed by {job.UserSid} to {job.SpoolFilePath}");
                }
                catch (Exception exception)
                {
                    Log.Error($"Failed to handle job {job.JobId} for {job.PrinterName} printed by {job.UserSid}:\r\n{exception}", 1014);
                }
            }, null);
        }

        void IPrintRedirector.Register(IPrintDispatcher dispatcher)
        {
            object syncAddFindDispatcher = this.syncAddFindDispatcher;
            lock (syncAddFindDispatcher)
            {
                this.Dispatchers.AddClient(dispatcher);
                Monitor.PulseAll(this.syncAddFindDispatcher);
            }
        }

        void IPrintRedirector.Unregister(IPrintDispatcher dispatcher)
        {
            this.Dispatchers.RemoveClient(dispatcher);
        }

        #endregion

        private void HandleJobInternal(PrintJob job)
        {
            var client = this.Dispatchers.GetClient(job) ?? this.StartDispatcher(job);
            Contract.Assert(client != null, "client != null");

            client.HandleJob(job);
        }

        private ConnectedDispatcher StartDispatcher(PrintJob job)
        {
            // only start 1 dispatcher at a time to avoid TOCTOU when calling GetClient
            // (otherwise get client 1, get client 1, both null, started twice)
            lock (syncStartDispatcher)
            {
                var stp = Stopwatch.StartNew();

                // create the remote process
                ProcessInterop.CreateProcessForSession(job.SessionId, this.DispatcherPath, "-embedding");

                // when the client connects, IPrintRedirector.Register will pulse syncAddFindDispatcher
                lock (syncAddFindDispatcher)
                {
                    while (stp.Elapsed < TimeSpan.FromSeconds(20.0))
                    {
                        this.ctsShutdown.Token.ThrowIfCancellationRequested();

                        var client = this.Dispatchers.GetClient(job);
                        if (client != null)
                        {
                            return client;
                        }

                        // wait for the dispatcher to connect back to us
                        Monitor.Wait(this.syncAddFindDispatcher, TimeSpan.FromSeconds(5.0));
                    }
                }
            }

            throw new TimeoutException("Timeout waiting for dispatcher to register");
        }

        #region Registration

        internal static string OurAppID => typeof(Program).GUID.ToString("B").ToUpper();

        internal static string OurExeName => Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        internal static byte[] AccessPermBlob = new byte[]
        {
            0x01, 0x00, 0x04, 0x80, 0x70, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00,
            0x00, 0x00, 0x02, 0x00, 0x5c, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x0b, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x07, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x0a, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x14, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00, 0x07,
            0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00,
            0x00
        };

        internal static byte[] LaunchPermBlob = new byte[]
        {
            0x01, 0x00, 0x04, 0x80, 0x70, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00,
            0x00, 0x00, 0x02, 0x00, 0x5c, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x1f, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x05, 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00, 0x1f, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00,
            0x20, 0x02, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x0b, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x0b, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x14, 0x00, 0x1f, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x04, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00,
            0x00
        };

        [ComRegisterFunction]
        internal static void RegasmRegisterLocalServer(string path)
        {
            // path is HKEY_CLASSES_ROOT\\CLSID\\{clsid}", we only want CLSID...
            path = path.Substring("HKEY_CLASSES_ROOT\\".Length);
            using (var keyCLSID = Registry.ClassesRoot.OpenSubKey(path, writable: true))
            {
                // Remove the auto-generated InprocServer32 key after registration
                // (REGASM puts it there but we are going out-of-proc).
                keyCLSID.DeleteSubKeyTree("InprocServer32");

                // add an appid
                keyCLSID.SetValue("AppID", OurAppID);
            }

            using (var rkAppID = Registry.ClassesRoot.CreateSubKey($"AppID\\{OurAppID}"))
            {
                rkAppID.SetValue("LocalService", "esVirtualPrinterRedirector");
                rkAppID.SetValue("", "Esatto Virtual Printer Document Redirector");
                rkAppID.SetValue("AccessPermission", AccessPermBlob);
                rkAppID.SetValue("LaunchPermission", LaunchPermBlob);
            }

            using (RegistryKey rkAppExe = Registry.ClassesRoot.CreateSubKey($"AppID\\{OurExeName}"))
            {
                rkAppExe.SetValue("AppID", OurAppID);
            }
        }

        [ComUnregisterFunction]
        internal static void RegasmUnregisterLocalServer(string path)
        {
            // path is HKEY_CLASSES_ROOT\\CLSID\\{clsid}", we only want CLSID...
            path = path.Substring(@"HKEY_CLASSES_ROOT\".Length);
            Registry.ClassesRoot.DeleteSubKeyTree(path, false);
            Registry.ClassesRoot.DeleteSubKeyTree($"AppID\\{OurAppID}", false);
            Registry.ClassesRoot.DeleteSubKeyTree($"AppID\\{OurExeName}", false);
        }

        #endregion
    }
}

