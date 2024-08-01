# Esatto Virtual Printer

This project is a virtual printer driver that can be used to generate XPS from 
any application that can print.  XPS can then be used, printed, converted, or 
have text extracted.

## Use

### Installation

1. Install manually or via your installer bootstrapper `Esatto.VirtualPrinter.Setup.msi` (from the release page)
1. Use `VirtualPrinterSystemConfiguration.Printers.Add(name, exePath)` or `Esatto.VirtualPrinter.PortInstaller.exe addprinter name exepath` to add a virtual printer to the system

See [`Esatto.VirtualPrinter.PrintToFileTarget.Setup`](Esatto.VirtualPrinter.PrintToFileTarget.Setup) for an example
of how to register a printer using an MSI.

### Printing

1. Print to the virtual printer from any application
1. The print driver (`esattovp3.inf`) will convert the print job to XPS
1. The spooler port (`EsPortMon4.dll`) will receive the job, save it to a file, and invoke your exe

### Job File

When your exe is invoked, it will be passed the path to the job file as the first argument. The job
file is a valid XPS document.  It does have some special data at the end that can be ignored, or
parsed out by using `Esatto.VirtualPrinter.SpoolFile`.

See [`Esatto.VirtualPrinter.PrintToFileTarget`](Esatto.VirtualPrinter.PrintToFileTarget) for an 
example app which handles a print job.