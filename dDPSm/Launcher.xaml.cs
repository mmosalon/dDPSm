using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace dDPSm
{
    /// <summary>
    /// Launcher.xaml の相互作用ロジック
    /// </summary>
    public partial class Launcher : Window
    {
        public Launcher()
        {
            InitializeComponent();
            BinPath.Text = "pso2_bin : " + Properties.Settings.Default.Path;
            PathCheck();
        }

        private void BanCheck_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BanChecked = BanCheck.IsChecked;
            SetBin.IsEnabled = BanCheck.IsChecked;
            if (BanCheck.IsChecked == false) { Continue_Button.IsEnabled = false; }
            PathCheck();
        }

        private void PathCheck()
        {
            if (File.Exists(Properties.Settings.Default.Path + "\\pso2.exe"))
            {
                PathResult.Content = "OK"; PathResult.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                if (Properties.Settings.Default.BanChecked)
                {
                    SetBin.IsEnabled = true;
                    CopyPSO2H.IsEnabled = true;
                    Copyddraw.IsEnabled = true;
                    Copydmgdmp.IsEnabled = true;
                    Copydmgcfg.IsEnabled = true;
                    AllCopyButton.IsEnabled = true;
                    CheckDll();
                } else
                {
                    SetBin.IsEnabled = false;
                    CopyPSO2H.IsEnabled = false;
                    Copyddraw.IsEnabled = false;
                    Copydmgdmp.IsEnabled = false;
                    Copydmgcfg.IsEnabled = false;
                    AllCopyButton.IsEnabled = false;
                }
            } else
            {
                PathResult.Content = "Error"; PathResult.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                CopyPSO2H.IsEnabled = false;
                Copyddraw.IsEnabled = false;
                Copydmgdmp.IsEnabled = false;
                Copydmgcfg.IsEnabled = false;
            }
            Status.Content = StatusString();
        }

        private string StatusString()
        {
            if (!Properties.Settings.Default.BanChecked) { return "Status : Warning not checked"; }
            if (!File.Exists(Properties.Settings.Default.Path + "\\pso2.exe")) { return "Status : pso2_bin check Failed"; }
            if (!File.Exists(Properties.Settings.Default.Path + "\\pso2h.dll")) { return "Status : DLL Not Installed"; }
            if (0 < Process.GetProcessesByName("pso2").Length) { return "Status : OK  (Warning : pso2 running now...)"; }
            return "Status : OK";
        }

        private void SetBin_Click(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if ((bool)dialog.ShowDialog())
            {
                Properties.Settings.Default.Path = dialog.SelectedPath;
                BinPath.Text = "pso2_bin : " + Properties.Settings.Default.Path;
                PathCheck();
            } else
            {
                PathCheck();
            }

        }



        private void CopyPSO2H_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Copy(@"./resources/pso2h.dll", Properties.Settings.Default.Path + @"/pso2h.dll", true);
            } catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show("dDPSmディレクトリ内にResourcesが見つかりませんでした\n誤って必要なファイルを削除していませんか?\n\n" + ex.ToString());
            } catch (IOException ex)
            {
                MessageBox.Show("pso2.exeが起動している、もしくは権限が足りません\npso2.exeを終了させてから管理者権限でコピーを試して下さい\n\n" + ex.ToString());
            } catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            PathCheck();
        }

        private void Copyddraw_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Copy(@"./resources/ddraw.dll", Properties.Settings.Default.Path + @"/ddraw.dll", true);
            } catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show("dDPSmディレクトリ内にResourcesが見つかりませんでした\n誤って必要なファイルを削除していませんか?\n\n" + ex.ToString());
            } catch (IOException ex)
            {
                MessageBox.Show("pso2.exeが起動している、もしくは権限が足りません\npso2.exeを終了させてから管理者権限でコピーを試して下さい\n\n" + ex.ToString());
            } catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            PathCheck();
        }

        private void Copydmgdmp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Properties.Settings.Default.Path + @"/plugins");
                File.Copy(@"./resources/PSO2DamageDump.dll", Properties.Settings.Default.Path + @"/plugins/PSO2DamageDump.dll", true);
            } catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show("dDPSmディレクトリ内にResourcesが見つかりませんでした\n誤って必要なファイルを削除していませんか?\n\n" + ex.ToString());
            } catch (IOException ex)
            {
                MessageBox.Show("pso2.exeが起動している、もしくは権限が足りません\npso2.exeを終了させてから管理者権限でコピーを試して下さい\n\n" + ex.ToString());
            } catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            PathCheck();
        }

        private void Copydmgcfg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Properties.Settings.Default.Path + @"/plugins");
                File.Copy(@"./resources/PSO2DamageDump.cfg", Properties.Settings.Default.Path + @"/plugins/PSO2DamageDump.cfg", true);
            } catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show("dDPSmディレクトリ内にResourcesが見つかりませんでした\n誤って必要なファイルを削除していませんか?\n\n" + ex.ToString());
            } catch (IOException ex)
            {
                MessageBox.Show("pso2.exeが起動している、もしくは権限が足りません\npso2.exeを終了させてから管理者権限でコピーを試して下さい\n\n" + ex.ToString());
            } catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            PathCheck();
        }

        private void AllCopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Copy(@"./resources/pso2h.dll", Properties.Settings.Default.Path + @"/pso2h.dll", true);
                File.Copy(@"./resources/ddraw.dll", Properties.Settings.Default.Path + @"/ddraw.dll", true);
                Directory.CreateDirectory(Properties.Settings.Default.Path + @"/plugins");
                File.Copy(@"./resources/PSO2DamageDump.dll", Properties.Settings.Default.Path + @"/plugins/PSO2DamageDump.dll", true);
                File.Copy(@"./resources/PSO2DamageDump.cfg", Properties.Settings.Default.Path + @"/plugins/PSO2DamageDump.cfg", true);
            } catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show("dDPSmディレクトリ内にResourcesが見つかりませんでした\n誤って必要なファイルを削除していませんか?\n\n" + ex.ToString());
            } catch (IOException ex)
            {
                MessageBox.Show("pso2.exeが起動している、もしくは権限が足りません\npso2.exeを終了させてから管理者権限でコピーを試して下さい\n\n" + ex.ToString());
            } catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            PathCheck();
        }

        private void CheckDll()
        {
            SolidColorBrush okbrush = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            SolidColorBrush passbrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 64));
            SolidColorBrush errorbrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            string pso2h = Dllhash("pso2h"); string ddraw = Dllhash("ddraw"); string dmgdump = Dllhash("plugins\\PSO2DamageDump");
            HookHash.Content = "pso2h.dll : " + pso2h; DDrawHash.Content = "ddraw.dll : " + ddraw; DmgdmpHash.Content = "PSO2DamageDump : " + dmgdump;
            if (pso2h == "7a616bf1f82bbc366cffd813a3c41f4e9dda060319af1b169bad37bcf48f0899")
            {
                HookResult.Content = "OK (4.0.0)"; HookResult.Foreground = okbrush;
            } else if (pso2h == "601d0b4e8d477fec3c2bab061ab4555af33f7c248d5121fe9211f7103d91a27b")
            {
                HookResult.Content = "OK (3.1.7)"; HookResult.Foreground = okbrush;
            } else if (pso2h == "aaee1145b0bfc202f1a4fbf3b414d8d83a731d2d8d2544735efe6a17cb657fc0")
            {
                HookResult.Content = "ERROR (3.1.6)"; HookResult.Foreground = errorbrush;
            } else if (pso2h == "Not Found")
            {
                HookResult.Content = "Not Found"; HookResult.Foreground = errorbrush;
            } else { HookResult.Content = "Unknown"; HookResult.Foreground = passbrush; }

            if (ddraw == "f462107109553bd0fe271978e1462e288e76e12c2b3238ebc0efa66655290447")
            {
                DDrawResult.Content = "OK (4.0.0)"; DDrawResult.Foreground = okbrush;
            } else if (ddraw == "0e2700bba52170f9dc5b4c811a7d7f23e22e9b39eae30006e1947a4acf05f63d")
            {
                DDrawResult.Content = "OK (3.1.7)"; DDrawResult.Foreground = okbrush;
            } else if (ddraw == "cb0d66924d2e5f98cc5bb7c59c353b248f7460822f5d6be8c649ef9aecce48b3")
            {
                DDrawResult.Content = "ERROR (3.1.6)"; DDrawResult.Foreground = errorbrush;
            } else if (ddraw == "Not Found")
            {
                DDrawResult.Content = "Not Found"; DDrawResult.Foreground = errorbrush;
            } else { DDrawResult.Content = "Unknown"; DDrawResult.Foreground = passbrush; }

            if (dmgdump == "46d396a3f1c08c2b4b9ea39d4cb56f989fbaca875d06ca607784a77b6771905f")
            {
                DmgdmpResult.Content = "OK (4.0.0)"; DmgdmpResult.Foreground = okbrush;
            } else if (dmgdump == "33ddffb54c4267ea62cc4ecf50ea12f19be9304d196e164439cc7091b6d55a41")
            {
                DmgdmpResult.Content = "OK (3.1.7)"; DmgdmpResult.Foreground = okbrush;
            } else if (dmgdump == "82bb5d1f9e04827947dc025068b8593f0e5c8e1ec5e4624233b5bcb4e7de3f18")
            {
                DmgdmpResult.Content = "ERROR (3.1.6)"; DmgdmpResult.Foreground = errorbrush;
            } else if (dmgdump == "Not Found")
            {
                DmgdmpResult.Content = "Not Found"; DmgdmpResult.Foreground = errorbrush;
            } else { DmgdmpResult.Content = "Unknown"; DmgdmpResult.Foreground = passbrush; }

            if (File.Exists(Properties.Settings.Default.Path + "\\plugins\\PSO2DamageDump.cfg")) { DmgcfgResult.Content = "OK"; DmgcfgResult.Foreground = okbrush; } else { DmgcfgResult.Content = "Not Found"; DmgcfgResult.Foreground = errorbrush; }

            if (Properties.Settings.Default.BanChecked && IsInstalled())
            {
                Continue_Button.IsEnabled = true;
            } else
            {
                Continue_Button.IsEnabled = false;
            }
        }


        private string Dllhash(string name)
        {
            try
            {
                if (!File.Exists(Properties.Settings.Default.Path + $"\\{name}.dll")) { return "Not Found"; }
                using (var fs = new FileStream(Properties.Settings.Default.Path + $"\\{name}.dll", FileMode.Open, FileAccess.Read))
                {
                    byte[] sha256byte = SHA256.Create().ComputeHash(fs);
                    string sha256string = BitConverter.ToString(sha256byte).Replace("-", "").ToLower();
                    return sha256string;
                }
            } catch (Exception ex) { return ex.InnerException.ToString(); }
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
            string pso2h = Dllhash("pso2h");
            string dmgdump = Dllhash("plugins\\PSO2DamageDump");
            if (File.Exists(Properties.Settings.Default.Path + "\\ddraw.dll"))
            {
                string ddraw = Dllhash("ddraw");
                if (ddraw == "cb0d66924d2e5f98cc5bb7c59c353b248f7460822f5d6be8c649ef9aecce48b3") { return false; }
            }
            if (pso2h == "aaee1145b0bfc202f1a4fbf3b414d8d83a731d2d8d2544735efe6a17cb657fc0") { return false; }
            if (dmgdump == "82bb5d1f9e04827947dc025068b8593f0e5c8e1ec5e4624233b5bcb4e7de3f18") { return false; }
            return true;
        }



        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }
        private void Continue_Click(object sender, RoutedEventArgs e) => DialogResult = true;
        private void Close_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    }
}
