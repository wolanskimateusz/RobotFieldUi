using Avalonia.Controls;
using Avalonia.Interactivity;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.Views.Dialogs;

public partial class CircleDialog : Window
{
    public (double Cx, double Cy, double R, int N, ArcDirection Dir)? Result { get; private set; }

    public CircleDialog() => InitializeComponent();

    private void OnInsert(object? sender, RoutedEventArgs e)
    {
        Result = (
            (double)(CxInput.Value    ?? 0),
            (double)(CyInput.Value    ?? 0),
            (double)(RadiusInput.Value ?? 100),
            (int)(NInput.Value         ?? 8),
            RbCCW.IsChecked == true ? ArcDirection.CCW : ArcDirection.CW
        );
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
