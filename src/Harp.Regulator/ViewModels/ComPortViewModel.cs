using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Harp.Regulator.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Harp.Regulator.ViewModels;

public class ComPortViewModel : ReactiveObject
{
    private const string DownloadingStatus = "Downloading...";

    private readonly DeviceAppLauncher appLauncher;

    public ComPortViewModel(string portName, DeviceAppLauncher launcher)
    {
        PortName = portName;
        appLauncher = launcher;

        Download = ReactiveCommand.CreateFromTask(
            DownloadAsync,
            this.WhenAnyValue(
                port => port.Info,
                port => port.IsDownloaded,
                (info, downloaded) => info is not null && !downloaded));

        Launch = ReactiveCommand.Create(
            LaunchApp,
            this.WhenAnyValue(port => port.IsDownloaded));
    }

    [Reactive] public string PortName { get; set; }

    [Reactive] public string Status { get; set; } = string.Empty;

    [Reactive] public DeviceInfo? Info { get; set; }

    [Reactive] public bool IsDownloaded { get; set; }

    [Reactive] public bool NeedsNetwork { get; set; }

    [Reactive] public string DownloadHint { get; set; } = string.Empty;

    [Reactive] public string Provenance { get; set; } = string.Empty;

    public ReactiveCommand<Unit, Unit> Download { get; }

    public ReactiveCommand<Unit, Unit> Launch { get; }

    public void RefreshAppState()
    {
        var info = Info;
        IsDownloaded = info is not null && appLauncher.IsInstalled(info);
        NeedsNetwork = info is not null && appLauncher.NeedsNetwork(info);
        DownloadHint = info is null ? string.Empty
            : IsDownloaded ? "Already downloaded."
            : NeedsNetwork ? "Will download from a configured package source."
            : "Already in the local package cache, so no download is needed.";
        Provenance = DescribeProvenance(info);
    }

    private string DescribeProvenance(DeviceInfo? info)
    {
        if (info is null)
            return string.Empty;

        var provenance = appLauncher.GetProvenance(info);
        if (provenance is null)
            return appLauncher.DescribeUnavailable(info) ?? string.Empty;

        var lines = new List<string>
        {
            $"Package: {provenance.PackageId} {provenance.Version}",
            $"Sources: {string.Join(", ", provenance.Sources)}"
        };

        if (provenance.Authors is null)
            lines.Add("Author, license and project details become available once downloaded.");
        else
        {
            lines.Add($"Authors: {provenance.Authors}");
            if (provenance.License is not null)
                lines.Add($"License: {provenance.License}");
            if (provenance.ProjectUrl is not null)
                lines.Add($"Project: {provenance.ProjectUrl}");
            if (provenance.RequiresLicenseAcceptance)
                lines.Add("This package asks that its license terms be accepted before use.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Status) ? PortName : $"{PortName}\t{Status}";
    }

    private async Task DownloadAsync()
    {
        var info = Info;
        if (info is null)
            return;

        Status = DownloadingStatus;
        var error = await appLauncher.DownloadAsync(info);
        RefreshAppState();
        Status = error ?? info.ToString();
    }

    private void LaunchApp()
    {
        var info = Info;
        if (info is null)
            return;

        var error = appLauncher.Launch(PortName, info);
        if (error is not null)
            Status = error;
    }
}
