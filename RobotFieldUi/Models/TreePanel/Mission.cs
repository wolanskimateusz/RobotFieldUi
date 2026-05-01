using System.Collections.ObjectModel;

namespace RobotFieldUi.Models.TreePanel;

public class Mission
{
    public string Name { get; set; } = "Nowa Misja";
    public ObservableCollection<MissionPath> Paths { get; } = new();

    public Mission(string name)
    {
        Name = name;    
    }
}