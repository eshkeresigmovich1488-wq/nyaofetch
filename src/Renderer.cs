using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Nyaofetch;

// One rendered frame: terminal lines already containing ANSI truecolor
// escape codes, ready to be printed as-is.
public sealed class AnsiFrame : List<string>
{
    public AnsiFrame() : base() { }
    public AnsiFrame(IEnumerable<string> lines) : base(lines) { }
}

public static class Renderer
{
    /// <summary>
    /// Loads an image (png/jpg/bmp/...) and converts it into block-character
    /// ANSI art sized to fit targetCols x targetRows character cells.
    ///
    /// Each character encodes two vertical pixels: the top pixel becomes the
    /// glyph's foreground color, the bottom pixel its background color - the
    /// same trick chafa/timg use to double vertical resolution.
    /// </summary>
    public static AnsiFrame RenderImageToAnsi(string path, int targetCols, int targetRows)
    {
        try
        {
            using var original = new Bitmap(path);
            int dstH = targetRows * 2;
            using var resized = ResizeBitmap(original, targetCols, dstH);
            return BitmapToAnsi(resized, targetCols, dstH);
        }
        catch (Exception ex)
        {
            return new AnsiFrame { $"\x1b[31m[nyaofetch] failed to load image: {path} ({ex.Message})\x1b[0m" };
        }
    }

    /// <summary>
    /// Loads an animated GIF and returns one AnsiFrame per frame plus the
    /// per-frame delay in milliseconds.
    /// </summary>
    public static bool RenderGifToAnsiFrames(string path, int targetCols, int targetRows,
        out List<AnsiFrame> frames, out List<int> delaysMs)
    {
        frames = new List<AnsiFrame>();
        delaysMs = new List<int>();

        try
        {
            using var img = Image.FromFile(path);
            var dim = new FrameDimension(img.FrameDimensionsList[0]);
            int frameCount = img.GetFrameCount(dim);

            // Frame delay times live in a property item, one 4-byte little-endian
            // int per frame, in units of 1/100 second.
            byte[]? delayBytes = null;
            try { delayBytes = img.GetPropertyItem(0x5100)?.Value; } catch { /* no delay info */ }

            int dstH = targetRows * 2;

            for (int i = 0; i < frameCount; i++)
            {
                img.SelectActiveFrame(dim, i);
                using var frameCopy = new Bitmap(img);
                using var resized = ResizeBitmap(frameCopy, targetCols, dstH);
                frames.Add(BitmapToAnsi(resized, targetCols, dstH));

                int delayMs = 100; // sensible default
                if (delayBytes != null && delayBytes.Length >= (i + 1) * 4)
                    delayMs = Math.Max(BitConverter.ToInt32(delayBytes, i * 4) * 10, 20);
                delaysMs.Add(delayMs);
            }
            return frameCount > 0;
        }
        catch
        {
            return false;
        }
    }

    private static Bitmap ResizeBitmap(Bitmap source, int width, int height)
    {
        var resized = new Bitmap(width, height);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(source, 0, 0, width, height);
        return resized;
    }

    private static AnsiFrame BitmapToAnsi(Bitmap bmp, int width, int height)
    {
        var frame = new AnsiFrame();
        for (int y = 0; y < height; y += 2)
        {
            var line = new StringBuilder(width * 20);
            for (int x = 0; x < width; x++)
            {
                Color top = bmp.GetPixel(x, y);
                Color bot = y + 1 < height ? bmp.GetPixel(x, y + 1) : top;

                line.Append($"\x1b[38;2;{top.R};{top.G};{top.B}m\x1b[48;2;{bot.R};{bot.G};{bot.B}m\u2580");
            }
            line.Append("\x1b[0m");
            frame.Add(line.ToString());
        }
        return frame;
    }

    // ---- terminal I/O helpers ---------------------------------------------------

    private static bool _vtEnabled;

    public static void PrintFrame(AnsiFrame frame)
    {
        EnableVirtualTerminal();
        var sb = new StringBuilder();
        foreach (var line in frame) sb.Append(line).Append('\n');
        Console.Out.Write(sb.ToString());
        Console.Out.Flush();
    }

    /// <summary>Moves the cursor back up so the next frame overwrites this one in place.</summary>
    public static void CursorHome(int frameHeightLines) => Console.Write($"\x1b[{frameHeightLines}A");

    public static void EnableVirtualTerminal()
    {
        if (_vtEnabled) return;
        _vtEnabled = true;

        // Required on cmd.exe / older conhost so ANSI escape codes actually render.
        // We open CONOUT$ directly (rather than trusting STD_OUTPUT_HANDLE) because
        // that's the handle guaranteed to be the real console, and per MS docs
        // ENABLE_VIRTUAL_TERMINAL_PROCESSING only takes effect together with
        // ENABLE_PROCESSED_OUTPUT.
        var handle = NativeConsole.CreateFileW(
            "CONOUT$",
            NativeConsole.GENERIC_READ | NativeConsole.GENERIC_WRITE,
            NativeConsole.FILE_SHARE_WRITE,
            IntPtr.Zero,
            NativeConsole.OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;

        if (NativeConsole.GetConsoleMode(handle, out uint mode))
        {
            const uint ENABLE_PROCESSED_OUTPUT = 0x0001;
            const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
            NativeConsole.SetConsoleMode(handle, mode | ENABLE_PROCESSED_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
    }
}

internal static class NativeConsole
{
    internal const uint GENERIC_READ = 0x80000000;
    internal const uint GENERIC_WRITE = 0x40000000;
    internal const uint FILE_SHARE_WRITE = 0x2;
    internal const uint OPEN_EXISTING = 3;

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    internal static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    internal static extern IntPtr GetStdHandle(int nStdHandle);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
