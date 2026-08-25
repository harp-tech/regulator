using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Harp.Regulator.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Harp.Regulator.ViewModels;

public class MainViewModel : ViewModelBase
{
    private const string ProbingStatus = "Probing...";

    public ObservableCollection<ComPortViewModel> Ports { get; } = new();

    [Reactive] public ComPortViewModel? SelectedPort { get; set; }

    public ReactiveCommand<Unit, Unit> RefreshPorts { get; }
    public ReactiveCommand<ComPortViewModel, Unit> LaunchSelected { get; }

    public MainViewModel()
    {
        RefreshPorts = ReactiveCommand.Create(RefreshPortList);

        LaunchSelected = ReactiveCommand.Create<ComPortViewModel>(port =>
        {
            // TODO: install the matching device tool and launch it.
        });

        this.WhenAnyValue(viewModel => viewModel.SelectedPort)
            .Select(ProbePort)
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(probed => probed.Port.Status = probed.Status);

        RefreshPortList();
    }

    private IObservable<(ComPortViewModel Port, string Status)> ProbePort(ComPortViewModel? port)
    {
        if (port is null)
            return Observable.Empty<(ComPortViewModel, string)>();

        port.Status = ProbingStatus;
        return Observable
            .FromAsync(cancellationToken => DeviceProbe.ProbePortAsync(port.PortName, cancellationToken))
            .Select(result => (port, DescribeProbeResult(result)))
            .Catch((Exception ex) => ex is OperationCanceledException
                ? Observable.Empty<(ComPortViewModel, string)>()
                : Observable.Return((port, ex.Message)));
    }

    private static string DescribeProbeResult(DeviceProbeResult result)
    {
        var info = result.Info;
        if (info is null)
            return result.ErrorMessage ?? string.Empty;

        return $"{info.DeviceName}, WhoAmI {info.WhoAmI}, "
            + $"firmware {info.FirmwareVersion}, hardware {info.HardwareVersion}";
    }

    private void RefreshPortList()
    {
        var selectedPortName = SelectedPort?.PortName;
        Ports.Clear();
        foreach (var portName in DeviceProbe.GetCandidatePorts())
        {
            Ports.Add(new ComPortViewModel { PortName = portName });
        }

        SelectedPort = Ports.FirstOrDefault(port => port.PortName == selectedPortName);
    }
}
