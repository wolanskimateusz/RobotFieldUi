using System;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobotFieldUi.Models.TreePanel;
using RobotFieldUi.Services;

namespace RobotFieldUi.ViewModels;

public partial class PropertiesViewModel : ViewModelBase
{
    private readonly UndoRedoService _undoRedo;

    // Delegate ustawiany przez MainWindowViewModel
    public Func<PathPoint, MissionPath?>? GetParentPath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(TypeLabel), nameof(HasName), nameof(HasCoords),
        nameof(HasDiameter), nameof(InfoLabel))]
    private object? _selectedObject;

    [ObservableProperty] private string _draftName     = "";
    [ObservableProperty] private string _draftX        = "0";
    [ObservableProperty] private string _draftY        = "0";
    [ObservableProperty] private string _draftDiameter = "0";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDirty;

    private string _savedName     = "";
    private double _savedX, _savedY, _savedDiameter;

    // ── Computed properties ───────────────────────────────────────────────────

    public string TypeLabel => SelectedObject switch
    {
        PathPoint { IsCenter: true } => "Środek okręgu",
        Project                      => "Projekt",
        Mission                      => "Misja",
        MissionPath                  => "Ścieżka",
        PathPoint                    => "Punkt",
        _                            => ""
    };

    public bool HasName     => SelectedObject is Project or Mission or MissionPath;
    public bool HasCoords   => SelectedObject is PathPoint;
    public bool HasDiameter => SelectedObject is PathPoint { IsCenter: true };

    public string InfoLabel => SelectedObject switch
    {
        Project p      => $"Misji: {p.Missions.Count}",
        Mission m      => $"Ścieżek: {m.Paths.Count}",
        MissionPath mp => $"Punktów: {mp.Points.Count}",
        _              => ""
    };

    public PropertiesViewModel(UndoRedoService undoRedo) => _undoRedo = undoRedo;

    // ── Ładowanie wartości przy zmianie zaznaczenia ────────────────────────────

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
            case PathPoint { IsCenter: true } center:
                _savedX        = center.X;  _savedY = center.Y;
                _savedDiameter = center.ArcRadius * 2;
                DraftX        = Fmt(center.X);
                DraftY        = Fmt(center.Y);
                DraftDiameter = Fmt(center.ArcRadius * 2);
                break;
            case PathPoint pt:
                _savedX = pt.X; _savedY = pt.Y;
                DraftX  = Fmt(pt.X);
                DraftY  = Fmt(pt.Y);
                break;
            default:
                _savedName = ""; _savedX = 0; _savedY = 0; _savedDiameter = 0;
                DraftName = ""; DraftX = "0"; DraftY = "0"; DraftDiameter = "0";
                break;
        }
        IsDirty = false;
    }

    partial void OnDraftNameChanged(string value)     => RefreshDirty();
    partial void OnDraftXChanged(string value)        => RefreshDirty();
    partial void OnDraftYChanged(string value)        => RefreshDirty();
    partial void OnDraftDiameterChanged(string value) => RefreshDirty();

    private void RefreshDirty()
    {
        IsDirty = SelectedObject switch
        {
            PathPoint { IsCenter: true } =>
                Diff(DraftX, _savedX) || Diff(DraftY, _savedY) || Diff(DraftDiameter, _savedDiameter),
            PathPoint =>
                Diff(DraftX, _savedX) || Diff(DraftY, _savedY),
            _ =>
                DraftName.Trim() != _savedName
        };
    }

    private bool CanSave() => IsDirty;

    // ── Zapis ─────────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        switch (SelectedObject)
        {
            case Project or Mission or MissionPath:
            {
                var target  = SelectedObject;
                var oldName = _savedName;
                var newName = DraftName.Trim();
                ApplyName(target, newName);
                _undoRedo.Record(new RenameAction(ApplyName, target, oldName, newName));
                break;
            }

            case PathPoint { IsCenter: true } center:
                SaveCenter(center);
                break;

            case PathPoint pt:
            {
                var oldX = _savedX; var oldY = _savedY;
                var newX = Parse(DraftX); var newY = Parse(DraftY);
                ApplyCoords(pt, newX, newY);
                OnPropertyChanged(nameof(InfoLabel));
                _undoRedo.Record(new MovePointAction(ApplyCoords, pt, oldX, oldY, newX, newY));
                break;
            }
        }
        IsDirty = false;
    }

    private void SaveCenter(PathPoint center)
    {
        var newCx     = Parse(DraftX);
        var newCy     = Parse(DraftY);
        var newRadius = Parse(DraftDiameter) / 2.0;
        if (newRadius <= 0) newRadius = center.ArcRadius;

        var path      = GetParentPath?.Invoke(center);
        // Szukamy punktów obwodowych po ArcRadius — przy tworzeniu okręgu wszystkie
        // perimeter.ArcRadius == center.ArcRadius, co pozwala odróżnić je od innych
        // punktów ścieżki gdy okrąg jest wstawiony do istniejącej ścieżki.
        var perimeter = path?.Points
            .Where(p => !p.IsCenter && Math.Abs(p.ArcRadius - center.ArcRadius) < 1e-6)
            .ToList();

        // Snapshot przed
        var before = new System.Collections.Generic.List<PointSnapshot>();
        before.Add(new PointSnapshot(center, center.X, center.Y, center.ArcRadius, center.ArcDirection));
        if (perimeter != null)
            foreach (var p in perimeter)
                before.Add(new PointSnapshot(p, p.X, p.Y, p.ArcRadius, p.ArcDirection));

        // Zastosuj nowe centrum
        var oldCx = center.X; var oldCy = center.Y; var oldRadius = center.ArcRadius;
        center.X         = newCx;
        center.Y         = newCy;
        center.ArcRadius = newRadius;

        // Przelicz punkty obwodowe
        if (perimeter != null)
        {
            foreach (var p in perimeter)
            {
                var angle = Math.Atan2(p.Y - oldCy, p.X - oldCx);
                p.X         = Math.Round(newCx + newRadius * Math.Cos(angle), 2);
                p.Y         = Math.Round(newCy + newRadius * Math.Sin(angle), 2);
                p.ArcRadius = newRadius;
            }
        }

        // Snapshot po
        var after = new System.Collections.Generic.List<PointSnapshot>();
        after.Add(new PointSnapshot(center, center.X, center.Y, center.ArcRadius, center.ArcDirection));
        if (perimeter != null)
            foreach (var p in perimeter)
                after.Add(new PointSnapshot(p, p.X, p.Y, p.ArcRadius, p.ArcDirection));

        _undoRedo.Record(new TransformPointsAction(before, after));

        // Zaktualizuj draft (bez wywoływania RefreshDirty w pętli)
        _savedX = newCx; _savedY = newCy; _savedDiameter = newRadius * 2;
        DraftX        = Fmt(newCx);
        DraftY        = Fmt(newCy);
        DraftDiameter = Fmt(newRadius * 2);
    }

    // ── Helpery ───────────────────────────────────────────────────────────────

    private void ApplyName(object target, string name)
    {
        switch (target)
        {
            case Project p:      p.Name  = name; break;
            case Mission m:      m.Name  = name; break;
            case MissionPath mp: mp.Name = name; break;
        }
        if (ReferenceEquals(SelectedObject, target))
        {
            _savedName = name;
            DraftName  = name;
            IsDirty    = false;
        }
    }

    private void ApplyCoords(PathPoint pt, double x, double y)
    {
        pt.X = x;
        pt.Y = y;
        if (ReferenceEquals(SelectedObject, pt))
        {
            _savedX = x; _savedY = y;
            DraftX  = Fmt(x);
            DraftY  = Fmt(y);
            IsDirty = false;
        }
    }

    private static string Fmt(double v) =>
        v.ToString("0.##", CultureInfo.InvariantCulture);

    private static double Parse(string s)
    {
        var normalized = s.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private bool Diff(string draft, double saved) =>
        Math.Abs(Parse(draft) - saved) > 1e-9;
}
