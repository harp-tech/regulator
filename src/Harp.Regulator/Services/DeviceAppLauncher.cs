using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Harp.Regulator.Services;

public class DeviceAppLauncher
{
    readonly Dictionary<string, Process> runningApps = new(StringComparer.OrdinalIgnoreCase);
    readonly DeviceRegistry registry = new();

    const string HarpDirectoryName = ".harp";
    const string FeedVariable = "HARP_REGULATOR_FEED";
    const string AppVersionVariable = "HARP_REGULATOR_APP_VERSION";
    const string NuGetConfigFileName = "nuget.config";
    const string PublicFeedUrl = "https://api.nuget.org/v3/index.json";

    static readonly string[] SupportedChannels = ["dotnetTool"];

    const string EmptyManifest = """
        {
          "version": 1,
          "isRoot": true,
          "tools": {}
        }
        """;

    /// <summary>
    /// Gets a value indicating whether a device app started by this launcher is still running on
    /// the specified port.
    /// </summary>
    public bool IsRunning(string portName)
    {
        if (!runningApps.TryGetValue(portName, out var running))
            return false;

        if (!running.HasExited)
            return true;

        running.Dispose();
        runningApps.Remove(portName);
        return false;
    }

    /// <summary>
    /// Gets a value indicating whether the device app matching the specified device is already in
    /// a local manifest, so that it can be started without installing anything.
    /// </summary>
    public bool IsInstalled(DeviceInfo info)
    {
        var location = ResolveDotnetToolLocation(info);
        return location is not null
            && FindToolCommand(location.ManifestPath, location.PackageId) is not null;
    }

    /// <summary>
    /// Gets a value indicating whether installing the device app matching the specified device
    /// would have to download from a package source, rather than resolving from the local
    /// package cache.
    /// </summary>
    public bool NeedsNetwork(DeviceInfo info)
    {
        var location = ResolveDotnetToolLocation(info);
        if (location is null)
            return false;

        var cached = Path.Combine(
            GetGlobalPackagesFolder(),
            location.PackageId.ToLowerInvariant(),
            location.Version);
        return !Directory.Exists(cached);
    }

    /// <summary>
    /// Gets what is known about the device app matching the specified device, or
    /// <see langword="null"/> if no app is registered for it. Author details are read from the
    /// downloaded package, so they are absent until the app has been downloaded.
    /// </summary>
    public AppProvenance? GetProvenance(DeviceInfo info)
    {
        var location = ResolveDotnetToolLocation(info);
        if (location is null)
            return null;

        var sources = CollectPackageSources().Select(source => source.Value).ToArray();
        var packageDirectory = Path.Combine(
            GetGlobalPackagesFolder(),
            location.PackageId.ToLowerInvariant(),
            location.Version);
        var metadata = ReadPackageMetadata(packageDirectory);
        return new AppProvenance(
            location.PackageId,
            location.Version,
            sources,
            metadata.Authors,
            metadata.License,
            metadata.ProjectUrl,
            metadata.RequiresLicenseAcceptance);
    }

    static PackageMetadata ReadPackageMetadata(string packageDirectory)
    {
        var nuspecPath = Directory.Exists(packageDirectory)
            ? Directory.EnumerateFiles(packageDirectory, "*.nuspec").FirstOrDefault()
            : null;
        if (nuspecPath is null)
            return new PackageMetadata(null, null, null, false);

        try
        {
            var document = XDocument.Load(nuspecPath);
            XElement? Element(string name) => document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == name);

            var license = Element("license");
            var licenseText = license is null
                ? Element("licenseUrl")?.Value
                : license.Attribute("type")?.Value == "file"
                    ? $"{license.Value}, included in the package"
                    : license.Value;

            return new PackageMetadata(
                Element("authors")?.Value,
                licenseText,
                Element("projectUrl")?.Value,
                string.Equals(Element("requireLicenseAcceptance")?.Value, "true", StringComparison.OrdinalIgnoreCase));
        }
        catch (System.Xml.XmlException)
        {
            return new PackageMetadata(null, null, null, false);
        }
    }

    /// <summary>
    /// Installs the device app matching the specified device into a manifest of its own, without
    /// starting it. Returns a message describing the step that failed, or <see langword="null"/>
    /// on success.
    /// </summary>
    public async Task<string?> DownloadAsync(DeviceInfo info, CancellationToken cancellationToken = default)
    {
        var location = ResolveDotnetToolLocation(info);
        if (location is null)
            return DescribeUnavailable(info);

        if (FindToolCommand(location.ManifestPath, location.PackageId) is not null)
            return null;

        Directory.CreateDirectory(Path.GetDirectoryName(location.ManifestPath)!);
        if (!File.Exists(location.ManifestPath))
            await File.WriteAllTextAsync(location.ManifestPath, EmptyManifest, cancellationToken);

        var configPath = WriteNuGetConfig(location.ToolRoot, CollectPackageSources());
        var installError = await RunDotnetAsync(
            location.ToolRoot,
            cancellationToken,
            "tool", "install", location.PackageId,
            "--version", location.Version,
            "--local",
            "--configfile", configPath);
        if (installError is not null)
            return installError;

        return FindToolCommand(location.ManifestPath, location.PackageId) is null
            ? $"The tool manifest lists no command for {location.PackageId}."
            : null;
    }

    /// <summary>
    /// Starts the already installed device app matching the specified device, passing the port to
    /// open. Returns a message describing why the app was not started, or
    /// <see langword="null"/> if it was started or was already running on that port.
    /// </summary>
    public string? Launch(string portName, DeviceInfo info)
    {
        if (IsRunning(portName))
            return null;

        var location = ResolveDotnetToolLocation(info);
        if (location is null)
            return DescribeUnavailable(info);

        var command = FindToolCommand(location.ManifestPath, location.PackageId);
        return command is null
            ? $"{location.PackageId} has not been downloaded yet."
            : StartTool(location.ToolRoot, command, portName);
    }

    /// <summary>
    /// Describes why no app can be started for the specified device, or <see langword="null"/> if
    /// one can be.
    /// </summary>
    public string? DescribeUnavailable(DeviceInfo info)
    {
        var app = registry.FindApp(info.WhoAmI);
        if (app is null)
        {
            return registry.LoadError is not null
                ? $"The device registry at {registry.RegistryPath} could not be read. {registry.LoadError}"
                : $"No device app is registered for WhoAmI {info.WhoAmI}.";
        }

        return FindRunnableChannel(app) is not null
            ? null
            : $"An app is registered for WhoAmI {info.WhoAmI} on the "
                + $"{string.Join(" and ", app.Channels.Keys)} channel, "
                + "which this launcher cannot start.";
    }

    static string? FindRunnableChannel(DeviceApp app)
    {
        foreach (var channel in SupportedChannels)
        {
            if (app.Channels.TryGetValue(channel, out var packageId))
                return packageId;
        }

        return null;
    }

    DotnetToolLocation? ResolveDotnetToolLocation(DeviceInfo info)
    {
        var app = registry.FindApp(info.WhoAmI);
        var packageId = app is null ? null : FindRunnableChannel(app);
        if (packageId is null)
            return null;

        var version = Environment.GetEnvironmentVariable(AppVersionVariable)
            ?? info.FirmwareVersion.ToString();
        var toolRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            HarpDirectoryName,
            "tools",
            packageId,
            version);
        return new DotnetToolLocation(packageId, version, toolRoot, ResolveManifestPath(toolRoot));
    }

    static string GetGlobalPackagesFolder()
    {
        var configured = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        return !string.IsNullOrEmpty(configured)
            ? configured
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget",
                "packages");
    }

    record DotnetToolLocation(string PackageId, string Version, string ToolRoot, string ManifestPath);

    record PackageMetadata(
        string? Authors,
        string? License,
        string? ProjectUrl,
        bool RequiresLicenseAcceptance);

    static IEnumerable<KeyValuePair<string, string>> CollectPackageSources()
    {
        yield return new KeyValuePair<string, string>("nuget.org", PublicFeedUrl);

        var feed = Environment.GetEnvironmentVariable(FeedVariable);
        if (!string.IsNullOrEmpty(feed))
            yield return new KeyValuePair<string, string>("regulator-feed", feed);
    }

    static string WriteNuGetConfig(string toolRoot, IEnumerable<KeyValuePair<string, string>> sources)
    {
        var packageSources = new XElement("packageSources", new XElement("clear"));
        foreach (var source in sources)
        {
            packageSources.Add(new XElement(
                "add",
                new XAttribute("key", source.Key),
                new XAttribute("value", source.Value)));
        }

        var configPath = Path.Combine(toolRoot, NuGetConfigFileName);
        new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("configuration", packageSources))
            .Save(configPath);
        return configPath;
    }

    static string ResolveManifestPath(string toolRoot)
    {
        var configManifest = Path.Combine(toolRoot, ".config", "dotnet-tools.json");
        if (File.Exists(configManifest))
            return configManifest;

        var rootManifest = Path.Combine(toolRoot, "dotnet-tools.json");
        return File.Exists(rootManifest) ? rootManifest : configManifest;
    }

    static string? FindToolCommand(string manifestPath, string packageId)
    {
        if (!File.Exists(manifestPath))
            return null;

        using var stream = File.OpenRead(manifestPath);
        JsonDocument manifest;
        try
        {
            manifest = JsonDocument.Parse(stream);
        }
        catch (JsonException)
        {
            return null;
        }

        using var document = manifest;
        if (!document.RootElement.TryGetProperty("tools", out var tools))
            return null;

        foreach (var tool in tools.EnumerateObject())
        {
            if (!string.Equals(tool.Name, packageId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (tool.Value.TryGetProperty("commands", out var commands) &&
                commands.ValueKind == JsonValueKind.Array &&
                commands.GetArrayLength() > 0)
            {
                return commands[0].GetString();
            }
        }

        return null;
    }

    static async Task<string?> RunDotnetAsync(
        string workingDirectory,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        var startInfo = CreateDotnetStartInfo(workingDirectory, arguments);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using var process = Process.Start(startInfo);
        if (process is null)
            return "Failed to start the dotnet CLI.";

        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var error = await errorTask;
        var output = await outputTask;
        if (process.ExitCode == 0)
            return null;

        var detail = string.IsNullOrWhiteSpace(error) ? output : error;
        return $"dotnet {string.Join(' ', arguments)} failed. {detail.Trim()}";
    }

    string? StartTool(string workingDirectory, string command, string portName)
    {
        var startInfo = CreateDotnetStartInfo(
            workingDirectory,
            ["tool", "run", command, "--", "--port", portName]);
        try
        {
            var process = Process.Start(startInfo);
            if (process is null)
                return $"Failed to start {command}.";

            runningApps[portName] = process;
            return null;
        }
        catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
        {
            return ex.Message;
        }
    }

    static ProcessStartInfo CreateDotnetStartInfo(string workingDirectory, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
