using Esatto.VirtualPrinter.IPC;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;

namespace Esatto.VirtualPrinter.PrintToFileTarget
{
    // This is called directly (by using InstallTool to register the path to 
    // this assembly and the fully qualified typename).
    public sealed class PrintToFileTarget : IPrintTarget
    {
        public void HandleJob(PrintJob job)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog()
                {
                    Title = $"Save print job for {job.PrinterName}",
                    FileName = job.DocumentName,
                    Filter = "Tiff File (*.tiff)|*.tiff|XPS File (*.xps)|*.xps"
                };
                if (dialog.ShowDialog().Value)
                {
                    if (dialog.FilterIndex == 1)
                    {
                        ConvertFile(job.SpoolFilePath, dialog.FileName);
                    }
                    else
                    {
                        File.Copy(job.SpoolFilePath, dialog.FileName);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("Failed to convert job\r\n" + exception.ToString());
            }
        }

        private static void ConvertFile(string xpsPath, string tiffPath)
        {
            XpsDocument document = new XpsDocument(xpsPath, FileAccess.Read);

            DocumentPaginator documentPaginator = document.GetFixedDocumentSequence().DocumentPaginator;
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
                RenderTargetBitmap source = new RenderTargetBitmap(
                    (int)(page.Size.Width * scaleFactor), (int)(page.Size.Height * scaleFactor), 
                    dpi, dpi, PixelFormats.Pbgra32);

                // render
                source.Render(page.Visual);
                encoder.Frames.Add(BitmapFrame.Create(source));
            }

            // save to file
            using (FileStream stream = File.OpenWrite(tiffPath))
            {
                encoder.Save(stream);
            }
        }

        public void Ping()
        {
            // no-op
        }
    }
}
