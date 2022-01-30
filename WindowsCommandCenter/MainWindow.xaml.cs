using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Hotkeys;

namespace WindowsCommandCenter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        MonitorsController monitorsController = new MonitorsController();

        GlobalHotkey ghk;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ghk = new GlobalHotkey(this);
            ghk.Initialize(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            ghk.destroyHooks();
            base.OnClosed(e);
        }

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
