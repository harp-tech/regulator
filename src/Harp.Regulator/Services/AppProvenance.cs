using System.Collections.Generic;

namespace Harp.Regulator.Services;

/// <summary>
/// Represents what is known about the device app that matches a device. The authors, license and
/// project page are available only once the app is downloaded.
/// </summary>
public sealed record AppProvenance(
    string PackageId,
    string Version,
    IReadOnlyList<string> Sources,
    string? Authors,
    string? License,
    string? ProjectUrl,
    bool RequiresLicenseAcceptance);
