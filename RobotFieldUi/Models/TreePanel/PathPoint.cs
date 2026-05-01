using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RobotFieldUi.Models.TreePanel;

public class PathPoint : INotifyPropertyChanged
{
    private double _x;
    private double _y;

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

    public string DisplayName => $"Punkt ({X:0.##}, {Y:0.##})";

    public PathPoint(double x, double y) { _x = x; _y = y; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
