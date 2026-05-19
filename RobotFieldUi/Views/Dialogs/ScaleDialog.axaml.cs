using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RobotFieldUi.Views.Dialogs;

public partial class ScaleDialog : Window
{
    public double? Result { get; private set; }

    public ScaleDialog() => InitializeComponent();

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Result = (double)(FactorInput.Value ?? 1);
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
