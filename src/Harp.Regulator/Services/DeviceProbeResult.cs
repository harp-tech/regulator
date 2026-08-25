namespace Harp.Regulator.Services;

public sealed record DeviceProbeResult(
    DeviceProbeStatus Status,
    DeviceInfo? Info,
    string? ErrorMessage)
{
    public static DeviceProbeResult Responding(DeviceInfo info)
        => new(DeviceProbeStatus.Responding, info, ErrorMessage: null);

    public static DeviceProbeResult Failed(DeviceProbeStatus status, string errorMessage)
        => new(status, Info: null, errorMessage);
}
