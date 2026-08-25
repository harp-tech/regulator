using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Harp.Regulator.ViewModels;

public class ComPortViewModel : ReactiveObject
{
    [Reactive] public string PortName { get; set; } = string.Empty;
    [Reactive] public string Status { get; set; } = string.Empty;
}
