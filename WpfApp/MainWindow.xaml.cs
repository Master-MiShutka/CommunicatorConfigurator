using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace TMP.Work.CommunicatorPSDTU.UI.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double oldWidth, oldHeight;
        public MainWindow() => this.InitializeComponent();

        private void OnNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch
            {
                ;
            }
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is TabControl tabControl && tabControl.Name == "MainTabs" && e.OriginalSource is TabControl)
            {
                if (tabControl.SelectedIndex != 1)
                {
                    if (this.WindowState == WindowState.Maximized)
                    {
                        this.WindowState = WindowState.Normal;
                    }

                    this.Width = this.oldWidth;
                    this.Height = this.oldHeight;
                }
                if (tabControl.SelectedIndex == 1)
                {
                    if (this.WindowState == WindowState.Maximized)
                    {
                        this.oldWidth = this.MinWidth;
                        this.oldHeight = this.MinHeight;
                    }
                    else
                    {
                        this.oldWidth = this.Width == 0 ? this.ActualWidth : this.Width;
                        this.oldHeight = this.Height == 0 ? this.ActualHeight : this.Height;

                        this.Width += 600d;
                        this.Height += 200d;
                    }
                }
            }
        }
    }
}
