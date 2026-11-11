# Harp Regulator

A cross-platform launcher for Harp device apps. It identifies the Harp device on a serial port, installs the app registered for that device, and launches it.

## Installation

Download the installer for Windows, Linux, or macOS from the [Releases](https://github.com/harp-tech/regulator/releases) page and run it. Installing device apps also requires the [.NET SDK](https://dotnet.microsoft.com/download), version 8 or later.

## Documentation

For usage and further details, refer to the [Harp Regulator documentation](https://harp-tech.org/regulator).

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
