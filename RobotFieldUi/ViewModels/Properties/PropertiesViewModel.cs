using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.ViewModels;

public partial class PropertiesViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeLabel), nameof(HasName), nameof(HasCoords), nameof(InfoLabel))]
    private object? _selectedObject;

    [ObservableProperty] private string _draftName = "";
    [ObservableProperty] private string _draftX    = "0";
    [ObservableProperty] private string _draftY    = "0";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDirty;

    // Saved baseline — porównujemy z nim żeby wyliczyć IsDirty
    private string _savedName = "";
    private double _savedX, _savedY;

    // Computed properties z widoku
    public string TypeLabel => SelectedObject switch
    {
        Project      => "Projekt",
        Mission      => "Misja",
        MissionPath  => "Ścieżka",
        PathPoint    => "Punkt",
        _            => ""
    };

    public bool HasName   => SelectedObject is Project or Mission or MissionPath;
    public bool HasCoords => SelectedObject is PathPoint;

    public string InfoLabel => SelectedObject switch
    {
        Project p      => $"Misji: {p.Missions.Count}",
        Mission m      => $"Ścieżek: {m.Paths.Count}",
        MissionPath mp => $"Punktów: {mp.Points.Count}",
        _              => ""
    };

    // Ładuje wartości modelu do drafts gdy zmienia się zaznaczenie
    partial void OnSelectedObjectChanged(object? value)
    {
        switch (value)
        {
            case Project p:
                _savedName = p.Name;
                DraftName  = p.Name;
                break;
            case Mission m:
                _savedName = m.Name;
                DraftName  = m.Name;
                break;
            case MissionPath mp:
                _savedName = mp.Name;
                DraftName  = mp.Name;
                break;
            case PathPoint pt:
                _savedX = pt.X; _savedY = pt.Y;
                DraftX  = pt.X.ToString("0.##", CultureInfo.InvariantCulture);
                DraftY  = pt.Y.ToString("0.##", CultureInfo.InvariantCulture);
                break;
            default:
                _savedName = ""; _savedX = 0; _savedY = 0;
                DraftName = ""; DraftX = "0"; DraftY = "0";
                break;
        }
        IsDirty = false;
    }

    partial void OnDraftNameChanged(string _) => RefreshDirty();
    partial void OnDraftXChanged(string _)    => RefreshDirty();
    partial void OnDraftYChanged(string _)    => RefreshDirty();

    private void RefreshDirty()
    {
        IsDirty = SelectedObject is PathPoint
            ? Math.Abs(ParseCoord(DraftX) - _savedX) > 1e-9 ||
              Math.Abs(ParseCoord(DraftY) - _savedY) > 1e-9
            : DraftName.Trim() != _savedName;
    }

    private bool CanSave() => IsDirty;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        switch (SelectedObject)
        {
            case Project p:
                p.Name     = DraftName.Trim();
                _savedName = p.Name;
                break;
            case Mission m:
                m.Name     = DraftName.Trim();
                _savedName = m.Name;
                break;
            case MissionPath mp:
                mp.Name    = DraftName.Trim();
                _savedName = mp.Name;
                break;
            case PathPoint pt:
                pt.X = _savedX = ParseCoord(DraftX);
                pt.Y = _savedY = ParseCoord(DraftY);
                // Odśwież InfoLabel ścieżki rodzica jeśli panel nadal ją pokazuje
                OnPropertyChanged(nameof(InfoLabel));
                break;
        }
        IsDirty = false;
    }

    private static double ParseCoord(string s)
    {
        var normalized = s.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
