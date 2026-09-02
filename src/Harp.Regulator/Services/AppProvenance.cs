using System.Collections.Generic;

namespace Harp.Regulator.Services;

/// <summary>
/// Represents what is known about the device app matching a device. The author details are read
/// from the downloaded package, so they are absent before the app is downloaded, and absent
/// altogether when the channel does not carry them.
/// </summary>
public sealed record AppProvenance(
    string PackageId,
    string Version,
    IReadOnlyList<string> Sources,
    string? Authors,
    string? License,
    string? ProjectUrl,
    bool RequiresLicenseAcceptance);
