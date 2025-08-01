namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Controls;

using System.Windows.Controls;
using System.Windows.Input;

/// <summary>
/// Interaction logic for MainContent.xaml
/// </summary>
public partial class MainContent : UserControl
{
    public MainContent() => this.InitializeComponent();

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Keyboard.Focus(this.txtIpAddres);
    }
}
