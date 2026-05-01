using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RobotFieldUi.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {

        public MenuBarViewModel MenuBar { get; } = new();

        public StatusBarViewModel StatusBar { get; } = new();
        

    }
}
