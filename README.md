# Harp Regulator

A cross-platform launcher for Harp device GUI applications. Discover connected devices, install the matching configuration tool, and launch it.

## Overview

Each Harp device repository publishes its GUI as a `dotnet` tool package on NuGet. `Harp.Regulator` enumerates available serial ports, identifies connected Harp devices through the `WhoAmI` register, installs the matching tool version on demand, and launches it. One launcher, every Harp device GUI.

## Installation

Download the installer for your operating system from the [Releases](https://github.com/harp-tech/regulator/releases) page and run it. Installers are provided for Windows, Linux, and macOS.

## Building from source

The repository follows the standard Harp software layout, with the application project under `src/Harp.Regulator/` and shared build infrastructure under `build/`.

```sh
dotnet restore
dotnet build --configuration Release
```

To produce a self-contained per-platform build:

```sh
dotnet publish src/Harp.Regulator --configuration Release --runtime <RID> --self-contained
```

where `<RID>` is one of `win-x64`, `linux-x64`, or `osx-x64`.

## License

`Harp.Regulator` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT).
