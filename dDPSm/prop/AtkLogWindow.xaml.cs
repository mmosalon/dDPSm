using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace dDPSm
{
    /// <summary>
    /// AtkLogWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AtkLogWindow : Window
    {
        private DispatcherTimer updateTimer = new DispatcherTimer();

        public AtkLogWindow() => InitializeComponent();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            updateTimer.Tick += new EventHandler(Update);
            updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            updateTimer.Start();

            Update(sender, e);
        }

        private void Update_Click(object sender, RoutedEventArgs e) => Update(null, null);

        private void Update(object sender, EventArgs e)
        {
            AtkLogList.Items.Clear();
            foreach (Hit atk in MainWindow.userattacks) { AtkLogList.Items.Insert(0, atk); }
            AtkCount.Content = MainWindow.userattacks.Count;
        }

        private void Reset_Click(object sender, RoutedEventArgs e) => MainWindow.userattacks.Clear();

        private void AutoDel_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.Isautodel)
            {
                MainWindow.Isautodel = false;
                AutoDel.Content = "6";
                AutoDel.ToolTip = "AutoDelete Disabled";
            } else if (!MainWindow.Isautodel)
            {
                MainWindow.Isautodel = true;
                AutoDel.Content = "7";
                AutoDel.ToolTip = "AutoDelete Enabled";
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            updateTimer.Stop();
            AtkLogList.Items.Clear();
            SystemCommands.CloseWindow(this);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }

    }
}
