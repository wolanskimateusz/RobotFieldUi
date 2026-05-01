using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RobotFieldUi.Models.TreePanel;

public class Mission : INotifyPropertyChanged
{
    private string _name = "Nowa Misja";

    public string Name
    {
        get => _name;
        set { if (_name == value) return; _name = value; Notify(); }
    }

    public ObservableCollection<MissionPath> Paths { get; } = new();

    public Mission(string name) { _name = name; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
