using System;
using CommunityToolkit.Mvvm.Input;

namespace RobotFieldUi.ViewModels;

public partial class MenuBarViewModel : ViewModelBase
{
    // ── Plik ──────────────────────────────────────────────────────────────────
    public event EventHandler? NewFileRequested;
    public event EventHandler? OpenFileRequested;
    public event EventHandler? SaveFileRequested;
    public event EventHandler? SaveFileAsRequested;
    public event EventHandler? CloseRequested;

    [RelayCommand] private void NewFile()    => NewFileRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void OpenFile()   => OpenFileRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void SaveFile()   => SaveFileRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void SaveFileAs() => SaveFileAsRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Close()      => CloseRequested?.Invoke(this, EventArgs.Empty);

    // ── Edycja ────────────────────────────────────────────────────────────────
    public event EventHandler? SelectAllRequested;
    public event EventHandler? UndoRequested;
    public event EventHandler? RedoRequested;

    [RelayCommand] private void SelectAll() => SelectAllRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Undo()      => UndoRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Redo()      => RedoRequested?.Invoke(this, EventArgs.Empty);

    // ── Rysowanie ─────────────────────────────────────────────────────────────
    public event EventHandler? LineToolRequested;
    public event EventHandler? ArcToolRequested;
    public event EventHandler? CircleRequested;
    public event EventHandler? TranslateRequested;
    public event EventHandler? RotateRequested;
    public event EventHandler? ScaleRequested;
    public event EventHandler? MirrorRequested;

    [RelayCommand] private void LineTool()  => LineToolRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void ArcTool()   => ArcToolRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Circle()    => CircleRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Translate() => TranslateRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Rotate()    => RotateRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Scale()     => ScaleRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Mirror()    => MirrorRequested?.Invoke(this, EventArgs.Empty);
}
