using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RobotFieldUi.Views.Dialogs;

public partial class MirrorDialog : Window
{
    // Kąt osi lustra w stopniach (0=poziomo, 90=pionowo)
    public double? Result { get; private set; }

    public MirrorDialog()
    {
        InitializeComponent();
        RbC.IsCheckedChanged += (_, _) => AngleInput.IsEnabled = RbC.IsChecked == true;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Result = RbH.IsChecked == true ? 0.0
               : RbV.IsChecked == true ? 90.0
               : (double)(AngleInput.Value ?? 0);
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
