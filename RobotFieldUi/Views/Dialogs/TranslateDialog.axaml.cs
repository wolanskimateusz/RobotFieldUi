using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RobotFieldUi.Views.Dialogs;

public partial class TranslateDialog : Window
{
    public (double Dx, double Dy)? Result { get; private set; }

    public TranslateDialog() => InitializeComponent();

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Result = ((double)(DxInput.Value ?? 0), (double)(DyInput.Value ?? 0));
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
