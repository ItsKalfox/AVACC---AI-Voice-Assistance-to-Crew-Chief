using System.Windows.Controls;
using System.Windows.Input;
using AVACC.App.ViewModels;

namespace AVACC.App.Views;

public partial class VisualCMDView : UserControl
{
    public VisualCMDView()
    {
        InitializeComponent();
    }

    private void OnHoldToSpeakMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is VisualCMDViewModel vm)
        {
            vm.TriggerPttDownCommand.Execute(null);
        }
    }

    private void OnHoldToSpeakMouseUp(object sender, MouseEventArgs e)
    {
        if (DataContext is VisualCMDViewModel vm)
        {
            vm.TriggerPttUpCommand.Execute(null);
        }
    }
}
