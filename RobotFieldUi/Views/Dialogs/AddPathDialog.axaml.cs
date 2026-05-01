using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RobotFieldUi.Models.TreePanel;
using RobotFieldUi.ViewModels;

namespace RobotFieldUi.Views.Dialogs;

public partial class AddPathDialog : Window
{
    public (string Name, Mission Mission)? Result { get; private set; }

    public AddPathDialog(IEnumerable<Mission> missions, string defaultName)
    {
        DataContext = new AddPathDialogViewModel(missions, defaultName);
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        this.FindControl<TextBox>("PathNameBox")?.SelectAll();
        this.FindControl<TextBox>("PathNameBox")?.Focus();
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        var vm = (AddPathDialogViewModel)DataContext!;
        if (!vm.IsValid) return;
        Result = (vm.PathName.Trim(), vm.SelectedMission!);
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
