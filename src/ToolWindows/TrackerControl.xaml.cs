using System;
using System.Windows.Controls;
using DhCodetaskExtension.ViewModels;

namespace DhCodetaskExtension.ToolWindows
{
    public partial class TrackerControl : UserControl
    {
        private readonly TrackerViewModel _vm;

        public TrackerControl(TrackerViewModel vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            InitializeComponent();
            DataContext = _vm;
        }

        private void BtnHistory_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _vm.OpenHistoryAction?.Invoke();
        }

        private void BtnSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _vm.OpenSettingsAction?.Invoke();
        }
    }
}
