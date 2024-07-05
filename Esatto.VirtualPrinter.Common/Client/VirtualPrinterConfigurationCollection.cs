using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Printing.IndexedProperties;

namespace Esatto.VirtualPrinter
{
    using static DriverConstants;

    public sealed class VirtualPrinterConfigurationCollection : ObservableCollection<VirtualPrinterConfiguration>
    {
        internal bool IsInitialized;
        private readonly VirtualPrinterSystemConfiguration Parent;

        internal VirtualPrinterConfigurationCollection(VirtualPrinterSystemConfiguration parent)
        {
            this.Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public VirtualPrinterConfiguration Add(string queueName, string exePath, string? args = null, string? location = null, string? comment = null)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }
            if (string.IsNullOrEmpty(exePath))
            {
                throw new ArgumentNullException(nameof(exePath));
            }
            this.AssertWrite();

            // Setup print queue
            var initialParameters = new PrintPropertyDictionary();
            if (!string.IsNullOrWhiteSpace(location))
            {
                initialParameters.SetProperty("Location", new PrintStringProperty("Location", location));
            }
            if (!string.IsNullOrWhiteSpace(comment))
            {
                initialParameters.SetProperty("Comment", new PrintStringProperty("Comment", comment));
            }
            var printQueue = this.Parent.PrintServer.InstallPrintQueue(queueName,
                DriverModelName, [PortName], "WinPrint", initialParameters);

            // Add to config registry
            var configKey = this.Parent.PrintersKey!.CreateSubKey(printQueue.Name, true);
            configKey.SetValue("TargetExe", exePath);
            if (!string.IsNullOrWhiteSpace(args))
            {
                configKey.SetValue("TargetArgs", args);
            }
            else
            {
                configKey.DeleteValue("TargetArgs", false);
            }

            var newConfig = new VirtualPrinterConfiguration(this.Parent, printQueue, configKey);
            base.InsertItem(0, newConfig);
            return newConfig;
        }

        private void AssertWrite()
        {
            if (this.Parent.AccessLevel == PrintSystemDesiredAccess.EnumerateServer)
            {
                throw new InvalidOperationException("Configuration was opened as read-only");
            }
        }

        protected override void ClearItems()
        {
            throw new NotSupportedException();
        }

        protected override void InsertItem(int index, VirtualPrinterConfiguration item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (this.IsInitialized)
            {
                throw new InvalidOperationException("Use Add(string, string)");
            }
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            if (!this.IsInitialized)
            {
                throw new InvalidOperationException();
            }
            this.AssertWrite();
            VirtualPrinterConfiguration configuration = base[index];
            PrintServer.DeletePrintQueue(configuration.PrintQueue);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, VirtualPrinterConfiguration item)
        {
            throw new NotSupportedException();
        }

        public VirtualPrinterConfiguration this[string queueName]
        {
            get
            {
                var c = this.SingleOrDefault(s => s.Name.Equals(queueName, StringComparison.InvariantCultureIgnoreCase));
                if (c == null)
                {
                    throw new KeyNotFoundException("Unknown print queue");
                }
                return c;
            }
        }
    }
}

