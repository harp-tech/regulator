using System.Collections.Generic;

namespace Harp.Regulator.Services;

/// <summary>
/// Represents the app registered for a device, as its distribution channels keyed by channel name.
/// Channels this launcher cannot start are included.
/// </summary>
public sealed record DeviceApp(IReadOnlyDictionary<string, string> Channels)
{
    public bool IsDeclared => Channels.Count > 0;
}
