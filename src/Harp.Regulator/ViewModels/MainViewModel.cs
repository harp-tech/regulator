using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Harp.Regulator.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<ComPortViewModel> Ports { get; } = new();

    [Reactive] public ComPortViewModel? SelectedPort { get; set; }

    public ReactiveCommand<Unit, Unit> RefreshPorts { get; }
    public ReactiveCommand<ComPortViewModel, Unit> LaunchSelected { get; }

    public MainViewModel()
    {
        RefreshPorts = ReactiveCommand.Create(() =>
        {
            // TODO: enumerate available serial ports.
        });

        LaunchSelected = ReactiveCommand.Create<ComPortViewModel>(port =>
        {
            // TODO: probe WhoAmI on the selected port, install matching tool, launch.
        });
    }
}
