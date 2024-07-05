using Microsoft.Win32;
using System;
using System.Printing;

namespace Esatto.VirtualPrinter;

public sealed class VirtualPrinterConfiguration
{
    private readonly RegistryKey ConfigKey;
    private readonly VirtualPrinterSystemConfiguration Parent;
    internal readonly PrintQueue PrintQueue;

    public string TargetExe { get; }

    public string? TargetArgs { get; }

    public string Name => this.PrintQueue.Name;

    internal VirtualPrinterConfiguration(VirtualPrinterSystemConfiguration parent, PrintQueue printQueue, RegistryKey key)
    {
        this.Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        this.PrintQueue = printQueue ?? throw new ArgumentNullException(nameof(printQueue));
        this.ConfigKey = key;

        this.TargetArgs = (string?)key.GetValue("TargetArgs");
        this.TargetExe = (string)key.GetValue("TargetExe")!;
    }

    internal void Dispose()
    {
        this.ConfigKey.Dispose();
        this.PrintQueue.Dispose();
    }
}

