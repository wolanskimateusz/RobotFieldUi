using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using RobotFieldUi.ViewModels;
using RobotFieldUi.Views.Dialogs;

namespace RobotFieldUi.Views.Controls;

public partial class TreePanelControl : UserControl
{
    public TreePanelControl()
    {
        InitializeComponent();
    }

    private TreePanelViewModel? Vm => DataContext as TreePanelViewModel;

    // ── Synchronizacja wielokrotnej selekcji z ViewModel ────────────────────

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Vm is not { } vm) return;

        foreach (var item in e.RemovedItems.OfType<object>())
            vm.SelectedTreeItems.Remove(item);
        foreach (var item in e.AddedItems.OfType<object>())
            if (!vm.SelectedTreeItems.Contains(item))
                vm.SelectedTreeItems.Add(item);
    }

    // ── Pobieranie elementu drzewa z klikniętego MenuItem ───────────────────

    private static object? GetClickedItem(object? sender)
    {
        if (sender is not MenuItem mi) return null;
        var cm = mi.FindLogicalAncestorOfType<ContextMenu>();
        if (cm?.PlacementTarget?.DataContext is { } fromTarget)
            return fromTarget;
        return mi.DataContext;
    }

    // ── Akcje pojedynczego elementu ─────────────────────────────────────────

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        var item = GetClickedItem(sender);
        if (item != null) Vm?.DeleteItemCommand.Execute(item);
    }

    private void OnAddMissionClick(object? sender, RoutedEventArgs e)
    {
        var item = GetClickedItem(sender);
        if (item != null) Vm?.AddMissionCommand.Execute(item);
    }

    private void OnAddPathClick(object? sender, RoutedEventArgs e)
    {
        var item = GetClickedItem(sender);
        if (item != null) Vm?.AddPathCommand.Execute(item);
    }

    // ── Usuń zaznaczone (z potwierdzeniem) ──────────────────────────────────

    private async void OnDeleteSelectedClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not { } vm) return;

        var clicked = GetClickedItem(sender);

        // Filtruj zaznaczone do tego samego typu co kliknięty element —
        // zapobiega przypadkowemu usunięciu ścieżki/misji zaznaczonych wcześniej
        List<object> items;
        if (clicked != null)
        {
            var clickedType = clicked.GetType();
            items = vm.SelectedTreeItems.Where(i => i.GetType() == clickedType).ToList();
        }
        else
        {
            items = vm.SelectedTreeItems.ToList();
        }

        // Fallback gdy nic pasującego nie zaznaczone — użyj klikniętego elementu
        if (items.Count == 0 && clicked != null)
            items.Add(clicked);

        if (items.Count == 0) return;

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        var msg = $"Czy chcesz usunąć {FormatCount(items.Count)}?";
        var confirmed = await ConfirmDeleteDialog.ShowAsync(parentWindow, msg);

        if (confirmed)
        {
            vm.SelectedTreeItems.Clear();
            if (items.Count == 1)
                vm.DeleteItemCommand.Execute(items[0]);
            else
                vm.DeleteItemsBulk(items);
        }
    }

    private static string FormatCount(int n) => n switch
    {
        1           => "1 element",
        2 or 3 or 4 => $"{n} elementy",
        _           => $"{n} elementów"
    };
}
