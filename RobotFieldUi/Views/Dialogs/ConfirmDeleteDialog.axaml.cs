using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RobotFieldUi.Views.Dialogs;

public partial class ConfirmDeleteDialog : Window
{
    public static async Task<bool> ShowAsync(Window? parent, string message)
    {
        if (parent == null) return false;
        var dialog = new ConfirmDeleteDialog(message);
        return await dialog.ShowDialog<bool>(parent);
    }

    public ConfirmDeleteDialog(string message)
    {
        InitializeComponent();
        this.FindControl<TextBlock>("MessageText")!.Text = message;
        this.FindControl<Button>("YesButton")?.Focus();
    }

    private void OnYes(object? sender, RoutedEventArgs e) => Close(true);
    private void OnNo(object? sender, RoutedEventArgs e)  => Close(false);
}
