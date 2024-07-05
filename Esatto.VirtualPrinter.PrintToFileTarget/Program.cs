using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using System.Windows;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Markup;
using System.IO.Packaging;

namespace Esatto.VirtualPrinter.PrintToFileTarget;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Console.ReadLine();
        if (args.Length != 1)
        {
            PrintUsage();
            return;
        }

        using var spoolFile = SpoolFile.OpenAsync(args[0]).GetAwaiter().GetResult();
        HandleJob(spoolFile);
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: PrintToFileTarget.exe [print queue name] [targetfile.xps]");
        Environment.Exit(1);
    }

    private static void HandleJob(SpoolFile spool)
    {
        try
        {
            var dialog = new SaveFileDialog()
            {
                Title = $"Save print job for {spool.PrinterName}",
                FileName = spool.DocumentName,
                Filter = "Tiff File (*.tiff)|*.tiff|XPS File (*.xps)|*.xps"
            };
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                if (dialog.FileName.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertFile(spool, dialog.FileName);
                }
                else
                {
                    using var outFile = File.OpenWrite(dialog.FileName);
                    spool.CopyTo(outFile);
                }
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show("Failed to convert job\r\n" + exception.ToString());
        }
    }

    private static void ConvertFile(Stream xps, string tiffPath)
    {
        using var package = Package.Open(xps);
        var uri = new Uri($"pack://{Guid.NewGuid():n}.xps");
        using var packageStoreHandle = new PackageStoreHandle(package, uri);
        using var document = new XpsDocument(package);
        document.Uri = uri;

        var documentPaginator = document.GetFixedDocumentSequence().DocumentPaginator;
        if (!documentPaginator.IsPageCountValid)
        {
            documentPaginator.ComputePageCount();
        }

        var encoder = new TiffBitmapEncoder();
        encoder.Compression = TiffCompressOption.Zip;

        // render each page to the tiff
        for (int pageIndex = 0; pageIndex < documentPaginator.PageCount; pageIndex++)
        {
            var page = documentPaginator.GetPage(pageIndex);

            // scale to destination DPI
            const double dpi = 300;
            const double scaleFactor = dpi / 96d;
            var source = new RenderTargetBitmap(
                (int)(page.Size.Width * scaleFactor), (int)(page.Size.Height * scaleFactor),
                dpi, dpi, PixelFormats.Pbgra32);

            // render
            source.Render(page.Visual);
            encoder.Frames.Add(BitmapFrame.Create(source));
        }

        // save to file
        using var stream = File.OpenWrite(tiffPath);
        encoder.Save(stream);
    }

    private class PackageStoreHandle : IDisposable
    {
        private readonly Uri Uri;

        public PackageStoreHandle(Package package, Uri uri)
        {
            this.Uri = uri;
            PackageStore.AddPackage(uri, package);
        }

        public void Dispose() => PackageStore.RemovePackage(Uri);
    }
}
