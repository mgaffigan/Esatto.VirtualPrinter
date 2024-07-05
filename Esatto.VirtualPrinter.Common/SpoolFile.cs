using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Esatto.VirtualPrinter;

public class SpoolFile : Stream
{
    private readonly Stream s;

    public string DocumentName { get; }
    public string PrinterName { get; }
    public string DataType { get; }

    private SpoolFile(string xml, Stream s, int length)
    {
        this.s = s;
        this.Length = length;
        var xd = XDocument.Parse(xml);
        this.DocumentName = xd.Root!.Attribute("DocumentName")!.Value;
        this.PrinterName = xd.Root.Attribute("PrinterName")!.Value;
        this.DataType = xd.Root.Attribute("DataType")!.Value;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        s.Dispose();
    }

    public static async Task<SpoolFile> OpenAsync(string path)
    {
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        try
        {
            return await OpenAsync(fs).ConfigureAwait(false);
        }
        catch
        {
            fs.Dispose();
            throw;
        }
    }

    public static async Task<SpoolFile> OpenAsync(Stream stream)
    {
        stream.Position = stream.Length - 4;
        var buffer = new byte[4];
        if (await stream.ReadAsync(buffer, 0, 4).ConfigureAwait(false) != 4)
        {
            throw new InvalidDataException("Failed to read spool file header");
        }
        var xmlLen = BitConverter.ToInt32(buffer, 0);
        if (xmlLen > stream.Length - 5)
        {
            throw new InvalidDataException("Invalid spool file header");
        }

        var xmlBuffer = new byte[xmlLen];
        var spoolLength = checked((int)(stream.Length - 4 - xmlLen));
        stream.Position = spoolLength;
        if (await stream.ReadAsync(xmlBuffer, 0, xmlLen).ConfigureAwait(false) != xmlLen)
        {
            throw new InvalidDataException("Failed to read spool file header");
        }
        var xml = Encoding.Unicode.GetString(xmlBuffer);
        stream.Position = 0;

        return new SpoolFile(xml, stream, spoolLength);
    }

    #region Stream implementation

    public override bool CanRead => s.CanRead;

    public override bool CanSeek => s.CanSeek;

    public override bool CanWrite => false;

    public override long Length { get; }

    public override long Position
    {
        get => s.Position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush() => s.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Position + count > Length)
        {
            count = (int)(Length - Position);
        }
        if (count <= 0)
        {
            return 0;
        }

        return s.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.End)
        {
            offset = Length - offset;
            origin = SeekOrigin.Begin;
        }
        else if (origin == SeekOrigin.Current)
        {
            offset += Position;
            origin = SeekOrigin.Begin;
        }

        if (origin != SeekOrigin.Begin) throw new NotSupportedException();
        if (offset < 0 || offset > Length) throw new ArgumentOutOfRangeException(nameof(offset));

        return s.Seek(offset, origin);
    }

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    #endregion
}
