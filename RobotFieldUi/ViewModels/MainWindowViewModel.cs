using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RobotFieldUi.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {

        public MenuBarViewModel MenuBar { get; } = new();
        // [ObservableProperty] generuje publiczną właściwość + INotifyPropertyChanged automatycznie.
        // Bez CommunityToolkit trzeba by pisać getter, setter i OnPropertyChanged() ręcznie.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private double _cursorX;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private double _cursorY;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private double _scale = 1.0;

        // Wyliczana właściwość – zmiana Scale/CursorX/CursorY automatycznie odświeża ten tekst.
        public string StatusText =>
            $"Skala: {Scale:0.##}:1  |  X: {CursorX:0.##}  Y: {CursorY:0.##}  |  mm";


    }
}
