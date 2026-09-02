using System.Collections.Generic;

namespace Harp.Regulator.Services;

/// <summary>
/// Represents the app registered for a device, as the distribution channels declared by the
/// registry, keyed by channel name. Channels are carried even when this launcher cannot start
/// them, so that a device with an unsupported app is not reported as having none.
/// </summary>
public sealed record DeviceApp(IReadOnlyDictionary<string, string> Channels)
{
    public bool IsDeclared => Channels.Count > 0;
}
