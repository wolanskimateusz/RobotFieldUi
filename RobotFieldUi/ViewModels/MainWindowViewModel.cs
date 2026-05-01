using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RobotFieldUi.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {

        public MenuBarViewModel MenuBar { get; } = new();
        public RibbonViewModel Ribbon { get; } = new();
        public StatusBarViewModel StatusBar { get; } = new();
        public TreePanelViewModel TreePanel { get; } = new();
        public CanvasViewModel Canvas { get; } = new();
        public PropertiesViewModel Properties { get; } = new();

        public MainWindowViewModel()
        {
            Canvas.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CanvasViewModel.CursorX))
                    StatusBar.CursorX = Canvas.CursorX;
                if (e.PropertyName == nameof(CanvasViewModel.CursorY))
                    StatusBar.CursorY = Canvas.CursorY;
            };
            
            Ribbon.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(RibbonViewModel.ActiveTool))
                    Canvas.ActiveTool = Ribbon.ActiveTool;
            };
            
            TreePanel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TreePanelViewModel.SelectedItem))
                    Properties.SelectedObject = TreePanel.SelectedItem;
            };
        }
    }
}
