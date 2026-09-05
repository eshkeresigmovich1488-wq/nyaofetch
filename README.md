<p align="center">
  <img src="assets/logo.svg" width="120" alt="nyaofetch logo" />
</p>

<h1 align="center">nyaofetch</h1>

<p align="center">
  <b>A neofetch clone for Windows that renders images and GIFs right in your terminal.</b><br>
  Instead of a static ASCII logo — a truecolor render of any image or animation.
</p>

<p align="center">
  <img alt="platform" src="https://img.shields.io/badge/platform-Windows-7C5CFC">
  <img alt="language" src="https://img.shields.io/badge/language-C%23%20%2F%20.NET%208-512BD4">
  <img alt="license" src="https://img.shields.io/badge/license-MIT-informational">
  <img alt="status" src="https://img.shields.io/badge/status-WIP-yellow">
</p>

---

## What is this

`nyaofetch` is a Windows clone of [neofetch](https://github.com/dylanaraps/neofetch) that, alongside
your system specs (OS, CPU, GPU, RAM, disk, uptime...), can draw **an actual image or GIF** in the
terminal instead of just a static ASCII logo.

## Features

- 🖥️ **Sysinfo** — OS and build, CPU, GPU, RAM, disk, uptime, shell, screen resolution
- 🖼️ **Image → ANSI** — any image (`png`/`jpg`/`bmp`) gets rendered as truecolor blocks
  (`▀` + fg/bg escape codes), giving near-photorealistic output in the terminal
- 🎬 **GIF animations** — an animated logo plays right in the terminal, frame by frame, in real time
- 🪟 **Big Windows logo by default** — a truecolor four-pane flag, neofetch-style
- ⚙️ **Flexible config** — easily switch between the logo, an image, or a GIF
- 🎥 **Video (planned)** — decoding video into a frame sequence (roadmap)

## Installation

### One-command install (like `apt install neofetch` on Linux)

Once this repo is pushed to GitHub, anyone can install it with a single command —
no cloning, no build flags, nothing to configure:

```powershell
irm https://raw.githubusercontent.com/neofetch-tech/nyaofetch/main/install.ps1 | iex
```

This clones the repo, publishes a self-contained `nyaofetch.exe` (no separate .NET
install needed to *run* it), and adds it to your PATH. Open a new terminal and just run:

```powershell
nyaofetch
```

...exactly like typing `neofetch` on Linux. Re-running the same command later updates
to the latest version.

### Manual install

```powershell
git clone https://github.com/neofetch-tech/nyaofetch.git
cd nyaofetch
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The binary lands in `bin\Release\net8.0-windows\win-x64\publish\nyaofetch.exe`.

## Usage

```powershell
nyaofetch.exe --image assets\me.png
nyaofetch.exe --gif assets\dance.gif --cols 40 --rows 20
nyaofetch.exe --cat
nyaofetch.exe --help
```

## Building for development

You'll need the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
git clone https://github.com/neofetch-tech/nyaofetch.git
cd nyaofetch
dotnet run
```

## Project structure

```
nyaofetch/
├── assets/            # logo and sample images
├── config/            # example config
├── src/
│   ├── Program.cs      # CLI + layout (art next to sysinfo, neofetch-style) + Windows logo art
│   ├── SysInfo.cs       # system data collection via WinAPI P/Invoke + registry
│   └── Renderer.cs      # image/gif → ANSI truecolor render (System.Drawing)
└── nyaofetch.csproj
```

## How the render works

Each terminal character encodes **two** vertical pixels at once: the top pixel becomes the
`▀` glyph's foreground color, the bottom pixel becomes its background color. This gives roughly
double the vertical resolution compared to a naive "one pixel = one character" approach — the same
trick `chafa` and `timg` use.

## Roadmap

- [ ] Video rendering (via decoding into a frame sequence)
- [ ] `--config` flag to load `nyaofetch.conf`
- [ ] Custom output themes/palettes
- [ ] Dithering for better quality at low `--cols`/`--rows`

## License

MIT
