using Esatto.Win32.Printing;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Printing;
using System.Printing.IndexedProperties;
using System.Security.Cryptography.X509Certificates;

namespace Esatto.VirtualPrinter.PortInstaller;

using static DriverConstants;

internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length < 1) args = ["install"];

            string command = args[0].ToLower();
            if (command == "install")
            {
                RunInstallCert();
                RunInstallPort(["installport"]);
                RunInstallDriver(["installdriver"]);
            }
            else if (command == "installport")
            {
                RunInstallPort(args);
            }
            else if (command == "installdriver")
            {
                RunInstallDriver(args);
            }
            else if (command == "uninstallport")
            {
                RunUninstall(args);
            }
            else if (command == "addprinter")
            {
                RunAddPrinter(args);
            }
            else if (command == "removeprinter")
            {
                RunRemovePrinter(args);
            }
            else throw new UnknownArgumentException();

            return 0;
        }
        catch (UnknownArgumentException)
        {
            WriteUsage();
            return -1;
        }
    }

    private static void RunInstallCert()
    {
        var cert = new X509Certificate2(GetResourceAsByteArary("signer.cer"));
        using var store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);
        store.Add(cert);
    }

    private static byte[] GetResourceAsByteArary(string resName)
    {
        var asm = typeof(Program).Assembly;
        using var resource = asm.GetManifestResourceStream($"{asm.GetName().Name}.{resName}")
                        ?? throw new FileNotFoundException($"Could not find resource {resName}");
        var result = new byte[resource.Length];
        if (resource.Read(result, 0, result.Length) != result.Length) throw new EndOfStreamException();
        return result;
    }

    private static void RunAddPrinter(string[] args)
    {
        var printerName = args[1];
        var regBase = "HKEY_LOCAL_MACHINE\\SOFTWARE\\In Touch Technologies\\Esatto\\Virtual Printer\\Printers\\" + printerName;
        if (args.Length > 2)
        {
            Registry.SetValue(regBase, "TargetExe", args[2]);
        }
        if (args.Length > 3)
        {
            Registry.SetValue(regBase, "TargetArgs", args[3]);
        }

        var printServer = new LocalPrintServer();
        var initialParameters = new PrintPropertyDictionary();
        var printQueue = printServer.InstallPrintQueue(printerName,
            DriverModelName, [PortName], "WinPrint", initialParameters);
    }

    private static void RunRemovePrinter(string[] args)
    {
        var printerName = args[1];
        var printServer = new LocalPrintServer();
        var printQueue = printServer.GetPrintQueue(printerName)
            ?? throw new FileNotFoundException("Queue not found");
        if (!PrintServer.DeletePrintQueue(printQueue))
        {
            throw new InvalidOperationException("Failed to delete queue");
        }
    }

    private static void RunInstallPort(string[] args)
    {
        string dllName, displayName;
        if (args.Length == 1)
        {
            dllName = PortDllName;
            displayName = PortDisplayName;
        }
        else if (args.Length == 3)
        {
            dllName = args[1];
            displayName = args[2];
        }
        else throw new UnknownArgumentException();

        string path = Path.Combine(Environment.SystemDirectory, dllName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Port dll does not exist at '{path}', exiting.", path);
        }

        try
        {
            PortMonitors.AddPortMonitor(displayName, dllName, null, null);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 3006 /* ERROR_PRINT_MONITOR_ALREADY_INSTALLED */)
        {
            Console.WriteLine("Port already installed, skipping.");
        }
    }

    private static void RunInstallDriver(string[] args)
    {
        string infName, driverName;
        if (args.Length == 1)
        {
            infName = DriverInfName;
            driverName = DriverModelName;
        }
        else if (args.Length == 2)
        {
            infName = args[1];
            driverName = DriverModelName;
        }
        else if (args.Length == 3)
        {
            infName = args[1];
            driverName = args[2];
        }
        else throw new UnknownArgumentException();

        string path = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), infName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Driver inf does not exist at '{path}', exiting.", path);
        }

        PrinterDriver.InstallInf(path, driverName);
    }

    private static void RunUninstall(string[] args)
    {
        string displayName;
        if (args.Length == 1)
        {
            displayName = PortDisplayName;
        }
        else if (args.Length == 2)
        {
            displayName = args[1];
        }
        else throw new UnknownArgumentException();

        PortMonitors.RemovePortMonitor(displayName, null, null);
    }

    private static void WriteUsage()
    {
        Console.Error.WriteLine(@"Usage: Esatto.VirtualPrinter.PortInstaller.exe command [args]

    install         Install the port and driver
    installport     Install the port
    uninstallport   Uninstall the port
    installdriver   Install the driver
    addprinter      Add a virtual printer queue
    removeprinter   Remove a virtual printer queue

Example invocations:
    install
    installport
    installport foo.dll ""display name""
    uninstallport
    uninstallport ""display name""
    installdriver
    installdriver foo.inf ""driver name""
    addprinter ""Printer Name""
    addprinter ""Printer Name"" ""c:\path\to\target.exe""
    removeprinter ""Printer Name""
");
        Environment.Exit(-1);
    }
}

#if NETFRAMEWORK
[Serializable]
#endif
public class UnknownArgumentException : ArgumentException
{
    public UnknownArgumentException() { }
    public UnknownArgumentException(string message) : base(message) { }
    public UnknownArgumentException(string message, Exception inner) : base(message, inner) { }
#if NETFRAMEWORK
    protected UnknownArgumentException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
}