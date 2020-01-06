using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace dDPSm
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool IsRunning, IsConnect;
        public static StreamReader logReader;
        public static string currentPlayerID = null;
        public static string currentPlayerName = null;
        public static string[] jaignoreskill, critignoreskill, playerid;
        public static Dictionary<uint, string> skillDict = new Dictionary<uint, string>();
        public static DirectoryInfo damagelogs;
        public static FileInfo damagelogcsv;
        private List<Player> workingList = new List<Player>();
        public static Session current = new Session();
        public static Session backup = new Session();
        public static List<Hit> userattacks = new List<Hit>();
        public static bool Isautodel = true;
        public DispatcherTimer damageTimer, logCheckTimer, inactiveTimer;
        private List<string> sessionLogFilenames = new List<string>();
        private IntPtr hwndcontainer;

        public MainWindow()
        {
            try { Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\dDPSm"); Directory.CreateDirectory("Logs"); Directory.CreateDirectory("Export"); } catch (Exception ex)
            {
                MessageBox.Show($"dDPSmに必要なアクセス権限がありません！\n管理者としてdDPSmを実行してみるか、システムのアクセス権を確認して下さい！\ndDPSmを別のフォルダーに移動してみるのも良いかも知れません。\n\n{ex.ToString()}");
                Application.Current.Shutdown();
            }
            Properties.Resources.Culture = CultureInfo.GetCultureInfo(Properties.Settings.Default.Language);
            InitializeComponent();
            Dispatcher.UnhandledException += ErrorToLog;
            if (!IsInstalled())
            {
                Launcher launcher = new Launcher(); launcher.ShowDialog();
                if (launcher.DialogResult != true && Application.Current != null) { Application.Current.Shutdown(); return; }
            }

            //if (Properties.Settings.Default.BouyomiStartup) { Process.Start(Properties.Settings.Default.BouyomiPath); }

            AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
            ConfigLoad();
            LoadListColumn();
        }

        private void ErrorToLog(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Directory.CreateDirectory("ErrorLogs");
                string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
                string filename = $"ErrorLogs/ErrorLogs - {datetime}.txt";
                File.WriteAllText(filename, e.Exception.ToString());
            } catch
            {
                MessageBox.Show("dDPSmはDirectory<ErrorLogs>の作成に失敗しました。" + Environment.NewLine + "dDPSm内のディレクトリにErrorLogを保存しました。");
                string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
                File.WriteAllText($"ErrorLogs - {datetime}.txt", e.Exception.ToString());
            }
        }

        private bool IsInstalled()
        {
            bool IsPso2_bin = File.Exists(Properties.Settings.Default.Path + "\\pso2.exe");
            bool IsPlugin = File.Exists(Properties.Settings.Default.Path + "\\pso2h.dll") && File.Exists(Properties.Settings.Default.Path + "\\plugins\\PSO2DamageDump.dll");
            bool IsPluginHash = IsCheckPlugin();

            if (Properties.Settings.Default.BanChecked && IsPso2_bin && IsPlugin && IsPluginHash) { return true; } else { return false; }
        }

        private bool IsCheckPlugin()
        {
            string pso2h = PluginHash("pso2h");
            string dmgdump = PluginHash("plugins\\PSO2DamageDump");
            if (File.Exists(Properties.Settings.Default.Path + "\\ddraw.dll"))
            {
                string ddraw = PluginHash("ddraw");
                if (ddraw == "cb0d66924d2e5f98cc5bb7c59c353b248f7460822f5d6be8c649ef9aecce48b3") { return false; }
            }
            if (pso2h == "aaee1145b0bfc202f1a4fbf3b414d8d83a731d2d8d2544735efe6a17cb657fc0") { return false; }
            if (dmgdump == "82bb5d1f9e04827947dc025068b8593f0e5c8e1ec5e4624233b5bcb4e7de3f18") { return false; }
            return true;
        }

        private string PluginHash(string dllname)
        {
            try
            {
                using (FileStream fs = new FileStream(Properties.Settings.Default.Path + $"\\{dllname}.dll", FileMode.Open, FileAccess.Read))
                {
                    byte[] sha256byte = SHA256.Create().ComputeHash(fs);
                    string sha256string = BitConverter.ToString(sha256byte).Replace("-", "").ToLower();
                    return sha256string;
                }
            } catch { return "Error"; }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            damagelogs = new DirectoryInfo(Properties.Settings.Default.Path + "\\damagelogs");
            if (Directory.Exists(Properties.Settings.Default.Path + "\\damagelogs") && damagelogs.GetFiles().Any())
            {
                damagelogcsv = damagelogs.GetFiles().Where(f => Regex.IsMatch(f.Name, @"\d+\.")).OrderByDescending(f => f.Name).FirstOrDefault();
                FileStream fileStream = File.Open(damagelogcsv.DirectoryName + "\\" + damagelogcsv.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(0, SeekOrigin.Begin);
                logReader = new StreamReader(fileStream);

                try
                {
                    while (currentPlayerID == null && !logReader.EndOfStream)
                    {
                        string line = logReader.ReadLine();
                        if (line == "") { continue; }
                        string[] parts = line.Split(',');
                        if (parts[0] == "0" && parts[3] == "YOU") { currentPlayerID = parts[2]; }
                    }
                } catch
                {
                    currentPlayerID = "12345678";
                }

                fileStream.Seek(0, SeekOrigin.End);
                logReader = new StreamReader(fileStream);
            }

            Task SkillLoad = SkillsLoad();
            HotKeyLoad();

            await Task.WhenAll(SkillLoad);

            damageTimer = new DispatcherTimer();
            logCheckTimer = new DispatcherTimer();
            inactiveTimer = new DispatcherTimer();
            damageTimer.Tick += new EventHandler(UpdateForm);
            damageTimer.Interval = new TimeSpan(0, 0, 0, 0, Properties.Settings.Default.Updateinv);
            logCheckTimer.Tick += new EventHandler(CheckNewCsv);
            logCheckTimer.Interval = new TimeSpan(0, 0, 20);
            inactiveTimer.Tick += new EventHandler(HideIfInactive);
            inactiveTimer.Interval = new TimeSpan(0, 0, 1);
            damageTimer.Start();
            logCheckTimer.Start();
            inactiveTimer.Start();
        }

        private void HideIfInactive(object sender, EventArgs e)
        {
            if (!Properties.Settings.Default.AutoHideWindow) { return; }
            string title = WindowsServices.GetActiveWindowTitle();
            string[] relevant = { "dDPSm", "dDPSm Setup", "dDPSm Error", "Encounter Timeout", "Phantasy Star Online 2", "Settings", "AtkLog", "Detalis", "Color", "dDPSm Install" };
            if (!relevant.Contains(title))
            {
                Opacity = 0;
            } else
            {
                TheWindow.Opacity = Properties.Settings.Default.WindowOpacity;
            }
        }

        private void CheckNewCsv(object sender, EventArgs e)
        {
            if (!Directory.Exists(Properties.Settings.Default.Path + "\\damagelogs") || !damagelogs.GetFiles().Any()) { return; }
            FileInfo curornewcsv = damagelogs.GetFiles().Where(f => Regex.IsMatch(f.Name, @"\d+\.")).OrderByDescending(f => f.Name).FirstOrDefault();
            if (damagelogcsv != null && curornewcsv.LastWriteTimeUtc < damagelogcsv.LastWriteTimeUtc) { return; }
            damagelogcsv = curornewcsv;
            FileStream fileStream = File.Open(damagelogcsv.DirectoryName + "\\" + damagelogcsv.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileStream.Seek(0, SeekOrigin.Begin);
            logReader = new StreamReader(fileStream);

            try
            {
                while (currentPlayerID == null && !logReader.EndOfStream)
                {
                    string line = logReader.ReadLine();
                    if (line == "") { continue; }
                    string[] parts = line.Split(',');
                    if (parts[0] == "0" && parts[3] == "YOU") { currentPlayerID = parts[2]; }
                }

            } catch
            {
                currentPlayerID = "12345678";
            }
            fileStream.Seek(0, SeekOrigin.End);
            logReader = new StreamReader(fileStream);
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Get this window's handle
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            hwndcontainer = hwnd;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            TheWindow.Opacity = Properties.Settings.Default.WindowOpacity;
            Window window = (Window)sender;
            window.Topmost = AlwaysOnTop.IsChecked;

            if (Properties.Settings.Default.ClickthroughEnabled)
            {
                int extendedStyle = WindowsServices.GetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE);
                WindowsServices.SetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE, extendedStyle & ~WindowsServices.WS_EX_TRANSPARENT);
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = AlwaysOnTop.IsChecked;
            if (Properties.Settings.Default.ClickthroughEnabled)
            {
                int extendedStyle = WindowsServices.GetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE);
                WindowsServices.SetWindowLong(hwndcontainer, WindowsServices.GWL_EXSTYLE, extendedStyle | WindowsServices.WS_EX_TRANSPARENT);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized) { WindowState = WindowState.Normal; }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //WIndowを移動可能にする
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }

        private void ListViewItem_MouseRightClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem data = sender as ListViewItem;
            Player data2 = (Player)data.DataContext;
            Detalis f = new Detalis(data2) { Owner = this };
            f.Show();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Closing...
            if (WindowState == WindowState.Maximized)
            {
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            } else
            {
                Properties.Settings.Default.Top = Top;
                Properties.Settings.Default.Left = Left;
                Properties.Settings.Default.Height = Height;
                Properties.Settings.Default.Width = Width;
                Properties.Settings.Default.Maximized = false;
            }

            if (IsRunning) { WriteLog(); }
            Properties.Settings.Default.Save();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        private void OpenRecentLog_Click(object sender, RoutedEventArgs e)
        {
            string filename = sessionLogFilenames[SessionLogs.Items.IndexOf((e.OriginalSource as MenuItem))];
            //attempting to open
            Process.Start(Directory.GetCurrentDirectory() + "\\" + filename);
        }

    }
}

