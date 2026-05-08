# Harp Regulator

A cross-platform launcher for Harp device GUI applications. Discover connected devices, install the matching configuration tool, and launch it.

## Getting started

Download the installer for your operating system from the [Releases](https://github.com/harp-tech/regulator/releases) page and run it. Installers are provided for Windows, Linux, and macOS.

After install, launch `Harp.Regulator` from the start menu, applications folder, or shell.

## How it works

Each Harp device repository publishes its GUI as a `dotnet` tool package on NuGet. Regulator enumerates available serial ports, identifies connected Harp devices through the `WhoAmI` register, installs the matching tool version on demand, and launches it.

## License

`Harp.Regulator` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT).
