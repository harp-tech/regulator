using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bonsai.Harp;

namespace Harp.Regulator.Services;

public static class DeviceProbe
{
    const int ProbeTimeoutMilliseconds = 500;

    public static IEnumerable<string> GetCandidatePorts()
    {
        var portNames = SerialPort.GetPortNames();
        if (OperatingSystem.IsMacOS())
        {
            return portNames
                .Where(portName => portName.Contains("cu."))
                .Where(portName => !portName.Contains("Bluetooth"));
        }

        return portNames;
    }

    public static async Task<DeviceProbeResult> ProbePortAsync(
        string portName,
        CancellationToken cancellationToken = default)
    {
        using var probeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probeTimeout.CancelAfter(TimeSpan.FromMilliseconds(ProbeTimeoutMilliseconds));

        try
        {
            using var device = new AsyncDevice(portName);
            var whoAmI = await device.ReadWhoAmIAsync(probeTimeout.Token);
            var deviceName = await device.ReadDeviceNameAsync(probeTimeout.Token);
            var hardwareVersion = await device.ReadHardwareVersionAsync(probeTimeout.Token);
            var firmwareVersion = await device.ReadFirmwareVersionAsync(probeTimeout.Token);
            return DeviceProbeResult.Responding(
                new DeviceInfo(whoAmI, deviceName, hardwareVersion, firmwareVersion));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DeviceProbeResult.Failed(
                DeviceProbeStatus.NotResponding,
                "No response. May not be a Harp device, or still starting up.");
        }
        catch (HarpException ex)
        {
            return DeviceProbeResult.Failed(DeviceProbeStatus.ProtocolError, ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return DeviceProbeResult.Failed(
                DeviceProbeStatus.PortUnavailable,
                "Port could not be opened. Another application may be using it, "
                    + "or the current user may lack permission.");
        }
        catch (IOException)
        {
            return DeviceProbeResult.Failed(
                DeviceProbeStatus.PortUnavailable,
                "Port could not be opened. It may have been disconnected. Try refreshing the list.");
        }
    }
}
