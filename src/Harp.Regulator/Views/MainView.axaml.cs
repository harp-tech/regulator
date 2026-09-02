using Avalonia.Controls;
using Avalonia.Interactivity;
using Harp.Regulator.ViewModels;

namespace Harp.Regulator.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void OnCopyPortClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ComPortViewModel port })
            return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
            return;

        await clipboard.SetTextAsync(port.ToString());
    }
}
