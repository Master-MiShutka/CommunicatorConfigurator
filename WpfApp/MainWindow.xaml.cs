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
                string? tag = (tabControl.SelectedItem as TabItem)?.Tag?.ToString();

                if (string.IsNullOrEmpty(tag))
                {
                    if (this.WindowState == WindowState.Maximized)
                    {
                        this.WindowState = WindowState.Normal;
                    }

                    if (this.oldWidth != 0 && this.oldHeight != 0)
                    {
                        this.Width = this.oldWidth;
                        this.Height = this.oldHeight;
                    }
                }
                else
                {
                    // selected CheckContent tab
                    if (this.WindowState == WindowState.Maximized)
                    {
                        this.oldWidth = this.MinWidth;
                        this.oldHeight = this.MinHeight;
                    }
                    else
                    {
                        if (tag == "CheckContent")
                        {
                            this.oldWidth = this.Width == 0 ? this.ActualWidth : this.Width;
                            this.oldHeight = this.Height == 0 ? this.ActualHeight : this.Height;

                            this.Width += 610d;
                            this.Height += 200d;

                            if (this.Top + this.Height > System.Windows.SystemParameters.VirtualScreenHeight)
                            {
                                this.Top -= System.Windows.SystemParameters.VirtualScreenHeight - this.Height;
                            }

                            if (this.Left + this.Width > System.Windows.SystemParameters.VirtualScreenWidth)
                            {
                                this.Left -= System.Windows.SystemParameters.VirtualScreenWidth - this.Width;
                            }

                            if (this.Left < 0)
                            {
                                this.Left = 0;
                            }

                            if (this.Top < 0)
                            {
                                this.Top = 0;
                            }
                        }
                    }
                }
            }
        }
    }
}
