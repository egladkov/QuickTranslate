using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace QuickTranslate.Infrastructure;

public static class AppIcon
{
    private static ImageSource? _imageSource;
    private static readonly object _lock = new();

    public static ImageSource ImageSource => GetImageSource();

    public static ImageSource GetImageSource()
    {
        if (_imageSource != null) return _imageSource;

        lock (_lock)
        {
            if (_imageSource != null) return _imageSource;

            using var bitmap = CreateBitmap(64);
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            _imageSource = bitmapImage;
            return _imageSource;
        }
    }

    public static Icon CreateTrayIcon()
    {
        using var bitmap = CreateBitmap(64);
        var hIcon = bitmap.GetHicon();
        try
        {
            using var tempIcon = Icon.FromHandle(hIcon);
            return (Icon)tempIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    public static byte[] CreateIcoData(params int[] sizes)
    {
        if (sizes == null || sizes.Length == 0)
        {
            sizes = [16, 24, 32, 48, 64, 128, 256];
        }

        var pngDataList = new List<byte[]>();
        foreach (var size in sizes)
        {
            using var bmp = CreateBitmap(size);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            pngDataList.Add(ms.ToArray());
        }

        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // ICONDIR header
        writer.Write((ushort)0); // Reserved
        writer.Write((ushort)1); // Resource type: 1 = Icon
        writer.Write((ushort)sizes.Length); // Image count

        int offset = 6 + (16 * sizes.Length);

        for (int i = 0; i < sizes.Length; i++)
        {
            int size = sizes[i];
            byte[] png = pngDataList[i];

            // ICONDIRENTRY
            writer.Write((byte)(size >= 256 ? 0 : size)); // Width
            writer.Write((byte)(size >= 256 ? 0 : size)); // Height
            writer.Write((byte)0); // Color count
            writer.Write((byte)0); // Reserved
            writer.Write((ushort)1); // Color planes
            writer.Write((ushort)32); // Bits per pixel
            writer.Write((uint)png.Length); // Image size in bytes
            writer.Write((uint)offset); // Offset of image data in ICO stream

            offset += png.Length;
        }

        // Image payloads
        foreach (var png in pngDataList)
        {
            writer.Write(png);
        }

        return output.ToArray();
    }

    public static void SaveIco(string filePath, params int[] sizes)
    {
        var data = CreateIcoData(sizes);
        File.WriteAllBytes(filePath, data);
    }

    public static Bitmap CreateBitmap(int size = 64)
    {
        var bitmap = new Bitmap(size, size);
        bitmap.SetResolution(96f, 96f);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        float scale = size / 64f;
        int padding = Math.Max(1, (int)Math.Round(2 * scale));
        int rectSize = size - padding * 2;
        int radius = Math.Max(2, (int)Math.Round(16 * scale));

        // Background rounded rectangle
        using var brush = new SolidBrush(Color.FromArgb(37, 99, 235)); // Accent blue (#2563EB)
        using var path = GetRoundedRectPath(new Rectangle(padding, padding, rectSize, rectSize), radius);
        g.FillPath(brush, path);

        // Draw letter 'Q'
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", 34 * scale, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("Q", font, textBrush, new RectangleF(0, 0, size, size), sf);

        return bitmap;
    }

    private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var size = new Size(diameter, diameter);
        var arc = new Rectangle(rect.Location, size);

        // Top-left
        path.AddArc(arc, 180, 90);

        // Top-right
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Bottom-right
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Bottom-left
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
