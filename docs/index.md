# Harp Regulator

A cross-platform launcher for Harp device apps. Discover connected devices, install the matching app, and launch it.

## Getting started

Download the installer for Windows, Linux, or macOS from the [Releases](https://github.com/harp-tech/regulator/releases) page and run it.

Once installed, launch `Harp.Regulator` from the Start menu, the Applications folder, or a terminal.

## How it works

Harp device apps are published as `dotnet` tool packages on NuGet. Regulator enumerates available serial ports, identifies connected Harp devices through the `WhoAmI` register, looks up the app registered for each device, installs the matching app version on demand, and launches it.

## License

`Harp.Regulator` is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
