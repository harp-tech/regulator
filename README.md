# Harp Regulator

A cross-platform launcher for Harp device apps. Discover connected devices, install the matching app, and launch it.

## Overview

Harp device apps are published as `dotnet` tool packages on NuGet. `Harp.Regulator` enumerates available serial ports, identifies connected Harp devices through the `WhoAmI` register, looks up the app registered for each device, installs the matching app version on demand, and launches it.

## Installation

Download the installer for Windows, Linux, or macOS from the [Releases](https://github.com/harp-tech/regulator/releases) page and run it.

## Building from source

The repository follows the standard Harp software layout, with the app project under `src/Harp.Regulator/` and shared build infrastructure under `build/`.

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

`Harp.Regulator` is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
