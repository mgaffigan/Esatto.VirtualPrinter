using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Printing;
using System.Runtime.CompilerServices;

namespace Esatto.VirtualPrinter
{
    using static DriverConstants;

    public sealed class VirtualPrinterSystemConfiguration : IDisposable
    {
        private const string PrintersKeyName = "Printers";
        private const string SystemConfigKeyPath = @"SOFTWARE\In Touch Technologies\Esatto\Virtual Printer";

        private bool IsDisposed;
        internal readonly PrintSystemDesiredAccess AccessLevel;

        internal readonly RegistryKey? RegistryRoot;
        internal readonly RegistryKey? PrintersKey;
        internal readonly LocalPrintServer PrintServer;

        public VirtualPrinterConfigurationCollection Printers { get; }

        public VirtualPrinterSystemConfiguration(PrintSystemDesiredAccess accessLevel)
        {
            this.AccessLevel = accessLevel;
            this.PrintServer = new LocalPrintServer(accessLevel);

            var hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            if (accessLevel == PrintSystemDesiredAccess.EnumerateServer)
            {
                this.RegistryRoot = hklm64.OpenSubKey(SystemConfigKeyPath, false);
                this.PrintersKey = this.RegistryRoot?.OpenSubKey("Printers", false);
            }
            else if (accessLevel != PrintSystemDesiredAccess.EnumerateServer)
            {
                this.RegistryRoot = hklm64.CreateSubKey(SystemConfigKeyPath, true);
                this.PrintersKey = this.RegistryRoot.CreateSubKey("Printers", true);
            }
            else throw new NotSupportedException();

            this.Printers = new VirtualPrinterConfigurationCollection(this);
            foreach (var queue in this.PrintServer.GetPrintQueues())
            {
                if (queue.QueueDriver.Name != DriverModelName
                    || queue.QueuePort.Name != PortName)
                {
                    continue;
                }

                var configKey = this.PrintersKey?.OpenSubKey(queue.Name);
                if (configKey == null)
                {
                    throw new InvalidOperationException($"No configuration exists for printer '{queue.Name}'");
                }
                this.Printers.Add(new VirtualPrinterConfiguration(this, queue, configKey));
            }

            this.Printers.IsInitialized = true;
        }

        internal void AssertAlive()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException("VirtualPrinterSystemConfiguration");
            }
        }

        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }
            this.IsDisposed = true;

            foreach (var configuration in this.Printers)
            {
                configuration.Dispose();
            }

            this.RegistryRoot?.Dispose();
            this.PrintersKey?.Dispose();
            this.PrintServer.Dispose();
        }
    }
}

