using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Printing.IndexedProperties;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Esatto.VirtualPrinter
{
    using static DriverConstants;

    public sealed class VirtualPrinterConfigurationCollection : ObservableCollection<VirtualPrinterConfiguration>
    {
        internal bool IsInitialized;
        private readonly VirtualPrinterSystemConfiguration Parent;

        internal VirtualPrinterConfigurationCollection(VirtualPrinterSystemConfiguration parent)
        {
            Contract.Requires(parent != null, "parent != null");

            this.Parent = parent;
        }

        public VirtualPrinterConfiguration Add(string queueName, string defaultHandlerTypeName, string handlerCodebase = null, string location = null, string comment = null)
        {
            Contract.Requires(!string.IsNullOrEmpty(queueName), "!string.IsNullOrEmpty(queueName)");
            Contract.Requires(!string.IsNullOrEmpty(defaultHandlerTypeName), "!string.IsNullOrEmpty(defaultHandlerTypeName)");
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
                DriverModelName, new string[] { PortName }, "WinPrint", initialParameters);

            // Add to config registry
            var configKey = this.Parent.PrintersKey.CreateSubKey(printQueue.Name, true);
            configKey.SetValue("HandlerTypeName", defaultHandlerTypeName);
            if (!string.IsNullOrWhiteSpace(handlerCodebase))
            {
                configKey.SetValue("HandlerCodebase", handlerCodebase);
            }
            else
            {
                configKey.DeleteValue("HandlerCodebase", false);
            }

            var newConfig = new VirtualPrinterConfiguration(this.Parent, printQueue, configKey);
            base.InsertItem(0, newConfig);
            return newConfig;
        }

        private void AssertWrite()
        {
            if (this.Parent.AccessLevel != VirtualPrinterConfigurationAccessLevel.ReadWrite)
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
            Contract.Assert(item != null, "item != null");

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

