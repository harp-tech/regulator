using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Harp.Regulator.Services;

/// <summary>
/// Resolves the app registered for a device identity. Entries are read from a local file
/// in the WhoAmI registry format, so that a device under development can be declared before it is
/// registered.
/// </summary>
public class DeviceRegistry
{
    const string RegistryFileVariable = "HARP_REGULATOR_REGISTRY";
    const string RegistryFileName = "whoami.yml";
    const string HarpDirectoryName = ".harp";

    static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public string? LoadError { get; private set; }

    public string RegistryPath =>
        Environment.GetEnvironmentVariable(RegistryFileVariable)
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                HarpDirectoryName,
                RegistryFileName);

    public DeviceApp? FindApp(int whoAmI)
    {
        LoadError = null;

        var path = RegistryPath;
        if (!File.Exists(path))
            return null;

        RegistryDocument? document;
        try
        {
            using var reader = new StreamReader(path);
            document = Deserializer.Deserialize<RegistryDocument>(reader);
        }
        catch (YamlException ex)
        {
            LoadError = ex.Message;
            return null;
        }

        if (document?.Devices is null || !document.Devices.TryGetValue(whoAmI, out var device))
            return null;

        var channels = device.App;
        return channels is null || channels.Count == 0
            ? null
            : new DeviceApp(channels);
    }

    class RegistryDocument
    {
        public Dictionary<int, RegistryDevice>? Devices { get; set; }
    }

    class RegistryDevice
    {
        public string Name { get; set; } = string.Empty;

        public Dictionary<string, string>? App { get; set; }
    }
}
