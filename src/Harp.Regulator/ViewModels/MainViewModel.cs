using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Harp.Regulator.Services;
using Harp.Toolkit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Harp.Regulator.ViewModels;

public class MainViewModel : ViewModelBase
{
    private const string ProbingStatus = "Probing...";

    private const int ProbeTimeoutMilliseconds = 500;

    private readonly DeviceAppLauncher appLauncher = new();

    private readonly Subject<ComPortViewModel?> probeRequests = new();

    private ProbingPort? probingPort;

    public ObservableCollection<ComPortViewModel> Ports { get; } = new();

    [Reactive] public ComPortViewModel? SelectedPort { get; set; }

    public ReactiveCommand<Unit, Unit> RefreshPorts { get; }

    public MainViewModel()
    {
        RefreshPorts = ReactiveCommand.Create(RefreshPortList);

        this.WhenAnyValue(viewModel => viewModel.SelectedPort)
            .Merge(probeRequests)
            .Select(ProbePort)
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(probed =>
            {
                probed.Port.Status = probed.Status;
                probed.Port.Info = probed.Info;
                probed.Port.RefreshAppState();
            });

        Observable.FromEvent<string>(
                handler => appLauncher.AppExited += handler,
                handler => appLauncher.AppExited -= handler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(portName =>
            {
                var port = Ports.FirstOrDefault(port => port.PortName == portName);
                if (port is null)
                    return;

                if (port == SelectedPort)
                    probeRequests.OnNext(port);
                else
                    port.RefreshAppState();
            });

        RefreshPortList();
    }

    private IObservable<(ComPortViewModel Port, string Status, DeviceIdentity? Info)> ProbePort(ComPortViewModel? port)
    {
        RestoreAbandonedProbe();
        if (port is null || appLauncher.IsRunning(port.PortName))
            return Observable.Empty<(ComPortViewModel, string, DeviceIdentity?)>();

        probingPort = new ProbingPort(port, port.Status, port.Info);
        port.Status = ProbingStatus;
        port.Info = null;
        port.RefreshAppState();
        return Observable
            .FromAsync(cancellationToken => DeviceProbe.ProbeAsync(port.PortName, ProbeTimeoutMilliseconds, cancellationToken))
            .Select(result => (port, DescribeProbeResult(result), result.Identity))
            .Catch((Exception ex) => ex is OperationCanceledException
                ? Observable.Empty<(ComPortViewModel, string, DeviceIdentity?)>()
                : Observable.Return((port, ex.Message, (DeviceIdentity?)null)));
    }

    private void RestoreAbandonedProbe()
    {
        var abandoned = probingPort;
        probingPort = null;
        if (abandoned is null || abandoned.Port.Status != ProbingStatus)
            return;

        abandoned.Port.Status = abandoned.PreviousStatus;
        abandoned.Port.Info = abandoned.PreviousInfo;
        abandoned.Port.RefreshAppState();
    }

    private static string DescribeProbeResult(DeviceProbeResult result)
    {
        return result switch
        {
            { Identity: { } identity } => identity.Describe(),
            { Status: DeviceProbeStatus.NotResponding } =>
                "No response. May not be a Harp device, or still starting up.",
            { Status: DeviceProbeStatus.PortUnavailable, Exception: UnauthorizedAccessException } =>
                "Port could not be opened. Another application may be using it, "
                    + "or the current user may lack permission.",
            { Status: DeviceProbeStatus.PortUnavailable } =>
                "Port could not be opened. It may have been disconnected. Try refreshing the list.",
            _ => result.Exception?.Message ?? string.Empty
        };
    }

    private void RefreshPortList()
    {
        var selectedPortName = SelectedPort?.PortName;
        var runningPorts = Ports
            .Where(port => appLauncher.IsRunning(port.PortName))
            .ToDictionary(port => port.PortName);
        Ports.Clear();
        foreach (var portName in PortDiscovery.GetCandidatePortNames())
        {
            Ports.Add(runningPorts.TryGetValue(portName, out var port)
                ? port
                : new ComPortViewModel(portName, appLauncher));
        }

        SelectedPort = Ports.FirstOrDefault(port => port.PortName == selectedPortName);
    }

    record ProbingPort(ComPortViewModel Port, string PreviousStatus, DeviceIdentity? PreviousInfo);
}
