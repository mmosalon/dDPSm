#if DEBUG
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace dDPSm
{
    /// <summary>
    /// DebugWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class DebugWindow : Window
    {
        public DebugWindow() => InitializeComponent();

        private void AddAttack_Click(object sender, RoutedEventArgs e) => AddAttack(1999999999, "ally", 1000000000);

        private void AddDBAttack_Click(object sender, RoutedEventArgs e) => AddAttack(267911699, "db", 1024);

        private void AddLswAttack_Click(object sender, RoutedEventArgs e) => AddAttack(2235773608, "lsw", 1024);

        private void AddAttack512_Click(object sender, RoutedEventArgs e) => AddAttack(1999999999, "ally", 512);

        private void AddAttack(uint atkid, string type, int dmg)
        {
            MainWindow m = (MainWindow)Application.Current.MainWindow;
            int index = -1;
            MainWindow.current.ActiveTime++;
            foreach (Player temp in MainWindow.current.players) { if (temp.ID == "10612489" && temp.isTemporary == "raw") { index = MainWindow.current.players.IndexOf(temp); } } //index処理
            if (index == -1)
            {
                MainWindow.current.players.Add(new Player("10612489", "Developer", "raw"));
                index = MainWindow.current.players.Count - 1;
            }
            Player tempattack = MainWindow.current.players[index];
            MainWindow.current.totalDamage += dmg;
            if (type == "ally") { tempattack.AllyDamage += dmg; tempattack.AllyAttacks.Add(new Hit(atkid, dmg, 1, 1)); } else if (type == "Zvs") { tempattack.ZvsDamage += dmg; MainWindow.current.totalZanverse += dmg; tempattack.Attacks.Add(new Hit(atkid, dmg, 1, 1)); } else if (type == "db") { tempattack.DBDamage += dmg; MainWindow.current.totalDBDamage += dmg; tempattack.DBAttacks.Add(new Hit(atkid, dmg, 1, 1)); } else if (type == "lsw") { tempattack.LswDamage += dmg; MainWindow.current.totalLswDamage += dmg; tempattack.LswAttacks.Add(new Hit(atkid, dmg, 1, 1)); }
            tempattack.Damage += dmg;
            tempattack.Attacks.Add(new Hit(atkid, dmg, 1, 1));
            MainWindow.IsRunning = true;
        }

        private void ShowColorBox(object sender, RoutedEventArgs e)
        {
            SelectColor selectwindow = new SelectColor(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            selectwindow.Show();
        }

        private void LoadCsvFile(object sender, RoutedEventArgs e)
        {
            /*
            MainWindow mainwindow = (MainWindow)Application.Current.MainWindow;
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "combat.csv(*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == false) { return; }

            string[] result = File.ReadAllLines(dialog.FileName);
            foreach (string str in result)
            {
                if (str == "") { continue; }
                if (str == "timestamp, instanceID, sourceID, sourceName, targetID, targetName, attackID, damage, IsJA, IsCrit, IsMultiHit, IsMisc, IsMisc2") { continue; }
                string[] parts = str.Split(',');
                int lineTimestamp = int.Parse(parts[0]);
                int instanceID = int.Parse(parts[1]);
                string sourceID = parts[2];
                string sourceName = parts[3];
                string targetID = parts[4];
                string targetName = parts[5];
                string attackID = parts[6]; //WriteLog()にてID<->Nameの相互変換がある為int化が無理
                System.Int64 hitDamage = System.Int64.Parse(parts[7]);
                byte JA = byte.Parse(parts[8]);
                byte Cri = byte.Parse(parts[9]);
                //string isMultiHit = parts[10];
                //string isMisc = parts[11];
                //string isMisc2 = parts[12];
                int index = -1;

                if (parts[0] == "0" && parts[3] == "YOU") { MainWindow.currentPlayerID = parts[2]; return; }
                if (hitDamage < 0) { return; }
                if (sourceID == "0" || attackID == "0") { return; }

                //処理スタート
                if (10000000 < int.Parse(sourceID)) //Player->Enemy
                {
                    MainWindow.current.newTimestamp = lineTimestamp; //newTimestampは常に最新
                    if (MainWindow.current.startTimestamp == 0) //初期が0だった場合
                    {
                        MainWindow.current.startTimestamp = MainWindow.current.newTimestamp;
                        MainWindow.current.nowTimestamp = MainWindow.current.newTimestamp;
                    }

                    if (MainWindow.current.newTimestamp - MainWindow.current.nowTimestamp >= 1)
                    {
                        MainWindow.current.diffTime = MainWindow.current.diffTime + 1;
                        MainWindow.current.nowTimestamp = MainWindow.current.newTimestamp;
                    }

                    if (Properties.Settings.Default.QuestTime) { MainWindow.current.ActiveTime = MainWindow.current.diffTime; } else { MainWindow.current.ActiveTime = MainWindow.current.newTimestamp - MainWindow.current.startTimestamp; }

                    lock (MainWindow.current.players)
                    {
                        foreach (Player x in MainWindow.current.players) { if (x.ID == sourceID && x.isTemporary == "raw") { index = MainWindow.current.players.IndexOf(x); } }
                    }
                    if (index == -1)
                    {
                        MainWindow.current.players.Add(new Player(sourceID, sourceName, "raw"));
                        index = MainWindow.current.players.Count - 1;
                    }

                    Player source = MainWindow.current.players[index];
                    if (Sepid.DBAtkID.Contains(attackID)) { source.DBDamage += hitDamage; MainWindow.current.totalDBDamage += hitDamage; source.DBAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else if (Sepid.LswAtkID.Contains(attackID)) { source.LswDamage += hitDamage; MainWindow.current.totalLswDamage += hitDamage; source.LswAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else if (Sepid.PwpAtkID.Contains(attackID)) { source.PwpDamage += hitDamage; MainWindow.current.totalPwpDamage += hitDamage; source.PwpAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else if (Sepid.AISAtkID.Contains(attackID)) { source.AisDamage += hitDamage; MainWindow.current.totalAisDamage += hitDamage; source.AisAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else if (Sepid.RideAtkID.Contains(attackID)) { source.RideDamage += hitDamage; MainWindow.current.totalRideDamage += hitDamage; source.RideAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else { source.AllyDamage += hitDamage; MainWindow.current.totalAllyDamage += hitDamage; source.AllyAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); }
                    if (attackID == "2106601422") { source.ZvsDamage += hitDamage; MainWindow.current.totalZanverse += hitDamage; }
                    if (Sepid.HTFAtkID.Contains(attackID)) { source.HTFDamage += hitDamage; MainWindow.current.totalFinish += hitDamage; }
                    MainWindow.current.totalDamage += hitDamage;
                    source.Damage += hitDamage;
                    source.Attacks.Add(new Hit(attackID, hitDamage, JA, Cri));
                    MainWindow.IsRunning = true;
                } else
                {
                    if (10000000 < int.Parse(targetID)) //Enemy->Player
                    {
                        if (!MainWindow.IsRunning) { return; }
                        if (10000000 < int.Parse(targetID)) //Enemy->Player
                        {
                            lock (MainWindow.current.players)
                            {
                                foreach (Player x in MainWindow.current.players) { if (x.ID == targetID && x.isTemporary == "raw") { index = MainWindow.current.players.IndexOf(x); } }
                            }
                            if (index == -1)
                            {
                                MainWindow.current.players.Add(new Player(targetID, targetName, "raw"));
                                index = MainWindow.current.players.Count - 1;
                            }
                            Player source = MainWindow.current.players[index];
                            source.Damaged += hitDamage;
                            MainWindow.IsRunning = true;
                        }
                    }
                }
            }
            MainWindow.current.players.Sort((x, y) => y.ReadDamage.CompareTo(x.ReadDamage));
                    */
        }

    }
}
#endif
