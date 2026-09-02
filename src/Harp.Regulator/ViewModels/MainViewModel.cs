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

    private readonly DeviceAppLauncher appLauncher = new();

    public ObservableCollection<ComPortViewModel> Ports { get; } = new();

    [Reactive] public ComPortViewModel? SelectedPort { get; set; }

    public ReactiveCommand<Unit, Unit> RefreshPorts { get; }

    public MainViewModel()
    {
        RefreshPorts = ReactiveCommand.Create(RefreshPortList);

        this.WhenAnyValue(viewModel => viewModel.SelectedPort)
            .Select(ProbePort)
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(probed =>
            {
                probed.Port.Status = probed.Status;
                probed.Port.Info = probed.Info;
                probed.Port.RefreshAppState();
            });

        RefreshPortList();
    }

    private IObservable<(ComPortViewModel Port, string Status, DeviceInfo? Info)> ProbePort(ComPortViewModel? port)
    {
        if (port is null)
            return Observable.Empty<(ComPortViewModel, string, DeviceInfo?)>();

        port.Status = ProbingStatus;
        port.Info = null;
        return Observable
            .FromAsync(cancellationToken => DeviceProbe.ProbePortAsync(port.PortName, cancellationToken))
            .Select(result => (port, DescribeProbeResult(result), result.Info))
            .Catch((Exception ex) => ex is OperationCanceledException
                ? Observable.Empty<(ComPortViewModel, string, DeviceInfo?)>()
                : Observable.Return((port, ex.Message, (DeviceInfo?)null)));
    }

    private static string DescribeProbeResult(DeviceProbeResult result)
    {
        var info = result.Info;
        return info is null
            ? result.ErrorMessage ?? string.Empty
            : info.ToString();
    }

    private void RefreshPortList()
    {
        var selectedPortName = SelectedPort?.PortName;
        Ports.Clear();
        foreach (var portName in DeviceProbe.GetCandidatePorts())
        {
            Ports.Add(new ComPortViewModel(portName, appLauncher));
        }

        SelectedPort = Ports.FirstOrDefault(port => port.PortName == selectedPortName);
    }
}
