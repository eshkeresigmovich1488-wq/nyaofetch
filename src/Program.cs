using System.Text;
using System.Threading;

namespace Nyaofetch;

public static class Program
{
    // Small cat ASCII art, kept behind --cat for old times' sake.
    private static readonly string[] CatAscii =
    {
        "   /\\_/\\  ",
        "  ( o.o ) ",
        "   > ^ <  ",
        " nyaofetch",
    };

    public static int Main(string[] args)
    {
        string? imagePath = null;
        string? gifPath = null;
        int cols = 32, rows = 16;
        bool useCat = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--image" when i + 1 < args.Length: imagePath = args[++i]; break;
                case "--gif" when i + 1 < args.Length: gifPath = args[++i]; break;
                case "--cols" when i + 1 < args.Length: int.TryParse(args[++i], out cols); break;
                case "--rows" when i + 1 < args.Length: int.TryParse(args[++i], out rows); break;
                case "--cat": useCat = true; break;
                case "--help":
                case "-h":
                    PrintUsage();
                    return 0;
            }
        }

        var info = SysInfo.Collect();

        if (!string.IsNullOrEmpty(gifPath))
        {
            if (!Renderer.RenderGifToAnsiFrames(gifPath, cols, rows, out var frames, out var delays))
            {
                Console.Error.WriteLine($"Failed to load gif: {gifPath}");
                return 1;
            }

            Console.WriteLine($"{info.Username}@{info.Hostname}\n-------------------");
            bool first = true;
            while (true)
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    if (!first) Renderer.CursorHome(frames[i].Count);
                    first = false;
                    Renderer.PrintFrame(frames[i]);
                    Thread.Sleep(delays[i]);
                }
            }
        }

        AnsiFrame art = !string.IsNullOrEmpty(imagePath)
            ? Renderer.RenderImageToAnsi(imagePath, cols, rows)
            : useCat
                ? new AnsiFrame(CatAscii)
                : BuildWindowsLogo();

        PrintSysInfoBesideArt(art, info);
        return 0;
    }

    // Big blue four-pane Windows flag in truecolor block characters - the
    // default logo now, neofetch-style. Windows accent blue (0,120,215).
    private static AnsiFrame BuildWindowsLogo()
    {
        const string block = "\u2588"; // U+2588 FULL BLOCK
        string pane = string.Concat(Enumerable.Repeat(block, 18));
        string gap = new string(' ', 3);
        string paneRow = pane + gap + pane;
        string blankRow = new string(' ', 39);

        const string blue = "\x1b[1;38;2;0;120;215m";
        const string reset = "\x1b[0m";

        var logo = new AnsiFrame();
        for (int i = 0; i < 8; i++) logo.Add(blue + paneRow + reset);
        for (int i = 0; i < 2; i++) logo.Add(blankRow);
        for (int i = 0; i < 8; i++) logo.Add(blue + paneRow + reset);
        return logo;
    }

    private static void PrintSysInfoBesideArt(AnsiFrame art, SystemInfo info)
    {
        var lines = new List<string>
        {
            $"{info.Username}@{info.Hostname}",
            "-------------------",
            $"OS:       {info.OsName} ({info.OsBuild})",
            $"CPU:      {info.CpuName}",
            $"GPU:      {info.GpuName}",
            $"RAM:      {info.UsedRamMb} / {info.TotalRamMb} MB",
            $"Disk (C): {info.DiskFreeGb}GB free / {info.DiskTotalGb}GB",
            $"Uptime:   {info.Uptime}",
            $"Shell:    {info.Shell}",
            $"Res:      {info.Resolution}",
        };

        int rowsTotal = Math.Max(art.Count, lines.Count);
        var sb = new StringBuilder();
        for (int i = 0; i < rowsTotal; i++)
        {
            string artLine = i < art.Count ? art[i] : "";
            string infoLine = i < lines.Count ? lines[i] : "";
            sb.Append(artLine).Append("  ").Append(infoLine).Append('\n');
        }
        Console.Out.Write(sb.ToString());
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "nyaofetch - a neofetch clone for Windows with image/gif rendering\n\n" +
            "Usage:\n" +
            "  nyaofetch                    show the big Windows logo + sysinfo\n" +
            "  nyaofetch --cat              show the small cat ASCII art instead\n" +
            "  nyaofetch --image <path>     render an image (png/jpg/bmp) as the logo\n" +
            "  nyaofetch --gif <path>       play an animated GIF in place of the logo\n" +
            "  nyaofetch --cols N --rows N  set render size (default 32x16)\n" +
            "  nyaofetch --help             show this message\n");
    }
}
