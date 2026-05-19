using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RobotFieldUi.Models.TreePanel;

public enum SegmentType  { Line, Arc }
public enum ArcDirection { CW, CCW }

public class PathPoint : INotifyPropertyChanged
{
    private double       _x;
    private double       _y;
    private SegmentType  _segmentOut   = SegmentType.Line;
    private double       _arcRadius    = 100.0;
    private ArcDirection _arcDirection = ArcDirection.CCW;
    private bool         _isCenter;

    public double X
    {
        get => _x;
        set { if (_x == value) return; _x = value; Notify(); Notify(nameof(DisplayName)); }
    }

    public double Y
    {
        get => _y;
        set { if (_y == value) return; _y = value; Notify(); Notify(nameof(DisplayName)); }
    }

    public SegmentType SegmentOut
    {
        get => _segmentOut;
        set { if (_segmentOut == value) return; _segmentOut = value; Notify(); }
    }

    // Dla zwykłych punktów: promień łuku.
    // Dla środka okręgu (IsCenter=true): promień całego okręgu.
    public double ArcRadius
    {
        get => _arcRadius;
        set { if (_arcRadius == value) return; _arcRadius = value; Notify(); }
    }

    public ArcDirection ArcDirection
    {
        get => _arcDirection;
        set { if (_arcDirection == value) return; _arcDirection = value; Notify(); }
    }

    public bool IsCenter
    {
        get => _isCenter;
        set { if (_isCenter == value) return; _isCenter = value; Notify(); Notify(nameof(DisplayName)); }
    }

    public string DisplayName => IsCenter
        ? $"Środek ({X:0.##}, {Y:0.##})"
        : $"Punkt ({X:0.##}, {Y:0.##})";

    public PathPoint(double x, double y) { _x = x; _y = y; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
