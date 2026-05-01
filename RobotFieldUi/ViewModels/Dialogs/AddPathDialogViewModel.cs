using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.ViewModels;

public partial class AddPathDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _pathName = "";
    [ObservableProperty] private Mission? _selectedMission;

    public ObservableCollection<Mission> Missions { get; }

    public AddPathDialogViewModel(IEnumerable<Mission> missions, string defaultName)
    {
        PathName = defaultName;
        Missions = new ObservableCollection<Mission>(missions);
        SelectedMission = Missions.FirstOrDefault();
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(PathName) && SelectedMission != null;
}
