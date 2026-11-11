using Harp.Toolkit;

namespace Harp.Regulator.ViewModels;

static class DeviceIdentityExtensions
{
    public static string Describe(this DeviceIdentity identity)
    {
        return $"{identity.DeviceName}, WhoAmI {identity.WhoAmI}, firmware {identity.FirmwareVersion}, hardware {identity.HardwareVersion}";
    }
}
