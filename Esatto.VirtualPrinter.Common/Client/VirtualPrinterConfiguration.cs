using Esatto.VirtualPrinter.IPC;
using Microsoft.Win32;
using System;
using System.Printing;
using System.Reflection;

namespace Esatto.VirtualPrinter
{
    public sealed class VirtualPrinterConfiguration
    {
        private readonly RegistryKey ConfigKey;
        private readonly VirtualPrinterSystemConfiguration Parent;
        internal readonly PrintQueue PrintQueue;

        public string HandlerCodebase { get; }

        public string HandlerTypeName { get; }

        public string Name => this.PrintQueue.Name;

        internal VirtualPrinterConfiguration(VirtualPrinterSystemConfiguration parent, PrintQueue printQueue, RegistryKey key)
        {
            Contract.Requires(parent != null, "parent != null");
            Contract.Requires(printQueue != null, "printQueue != null");

            this.Parent = parent;
            this.PrintQueue = printQueue;
            this.ConfigKey = key;

            this.HandlerTypeName = (string)key.GetValue("HandlerTypeName");
            this.HandlerCodebase = (string)key.GetValue("HandlerCodebase");
        }

        internal void Dispose()
        {
            this.ConfigKey.Dispose();
            this.PrintQueue.Dispose();
        }

        public IPrintTarget GetHandler()
        {
            try
            {
                return (IPrintTarget)Activator.CreateInstance(GetHandlerType(this.HandlerTypeName, this.HandlerCodebase));
            }
            catch (Exception ex)
            {
                var from = this.HandlerCodebase ?? "GAC";
                var ex2 = new TypeLoadException($"Failed to load handler '{this.HandlerTypeName}' from '{from}' for '{this.Name}'", ex);
                Log.Error($"{ex2.Message}\r\n\r\nException:\r\n{ex}", 1094);
                throw ex2;
            }
        }

        internal static Type GetHandlerType(string typeName, string codebase)
        {
            if (codebase != null)
            {
                return Assembly.LoadFrom(codebase).GetType(typeName, true);
            }
            else
            {
                return Type.GetType(typeName, true);
            }
        }

        internal static void ValidateHandlerType(string typeName, string codebase)
        {
            if (codebase != null)
            {
                Assembly.ReflectionOnlyLoadFrom(codebase).GetType(typeName, true);
            }
            else
            {
                Type.ReflectionOnlyGetType(typeName, true, false);
            }
        }
    }
}

