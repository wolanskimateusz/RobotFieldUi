using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RobotFieldUi.Views.Dialogs;

public partial class RotateDialog : Window
{
    public double? Result { get; private set; }

    public RotateDialog() => InitializeComponent();

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Result = (double)(AngleInput.Value ?? 0);
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
