using System;
using System.Windows;
using System.Windows.Input;

namespace WindowsCommandCenter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        MonitorsController monitorsController = new MonitorsController();

        public MainWindow()
        {
            InitializeComponent();

            brightnessLabel.Content = monitorsController.Get().ToString() + "%";
            brightnessSlider.Value = monitorsController.Get();
        }

        private void brightnessSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            UInt32 newVal = Convert.ToUInt32(brightnessSlider.Value);
            monitorsController.Set(newVal);
            brightnessLabel.Content = newVal.ToString() + "%";
        }
    }
}
