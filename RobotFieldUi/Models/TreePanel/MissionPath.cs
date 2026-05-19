using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RobotFieldUi.Models.TreePanel;

public class MissionPath : INotifyPropertyChanged
{
    private string _name = "Nowa Ścieżka";
    private bool   _isActive;

    public string Name
    {
        get => _name;
        set { if (_name == value) return; _name = value; Notify(); }
    }

    // Podświetlenie aktywnej ścieżki w drzewku
    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive == value) return; _isActive = value; Notify(); }
    }

    public ObservableCollection<PathPoint> Points { get; } = new();

    public MissionPath(string name) { _name = name; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
