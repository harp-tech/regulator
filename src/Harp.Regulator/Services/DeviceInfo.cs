using Bonsai.Harp;

namespace Harp.Regulator.Services;

/// <summary>
/// Represents the core register values read from a Harp device.
/// </summary>
public sealed record DeviceInfo(
    int WhoAmI,
    string DeviceName,
    HarpVersion HardwareVersion,
    HarpVersion FirmwareVersion)
{
    public override string ToString()
    {
        return $"{DeviceName}, WhoAmI {WhoAmI}, firmware {FirmwareVersion}, hardware {HardwareVersion}";
    }
}
