using Bonsai.Harp;

namespace Harp.Regulator.Services;

/// <summary>
/// Represents core register values read from a Harp device when probing a serial port.
/// </summary>
public sealed record DeviceInfo(
    int WhoAmI,
    string DeviceName,
    HarpVersion HardwareVersion,
    HarpVersion FirmwareVersion);
