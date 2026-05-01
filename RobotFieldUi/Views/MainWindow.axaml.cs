using System;
using Avalonia.Controls;
using RobotFieldUi.ViewModels;
using RobotFieldUi.Views.Dialogs;

namespace RobotFieldUi.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is not MainWindowViewModel vm) return;

            vm.ShowAddPathDialog = async (missions, defaultName) =>
            {
                var dialog = new AddPathDialog(missions, defaultName);
                await dialog.ShowDialog(this);
                return dialog.Result;
            };
        }
    }
}
