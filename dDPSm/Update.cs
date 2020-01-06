using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace dDPSm
{
    public partial class MainWindow : Window
    {
        public static string lastStatus = "";
        public static byte noUpdateCount = 0;
        public static byte UpdateCount = 0;
        public static int speechcount = 1;

        //Session.current
        public async Task<bool> UpdateLog(object sender, EventArgs e)
        {
            if (logReader == null) { return false; }
            string newLines = await logReader.ReadToEndAsync();
            if (newLines == "") { return false; }

            string[] dataLine = newLines.Split('\n');
            foreach (string line in dataLine)
            {
                if (line == "") { continue; }
                string[] parts = line.Split(',');
                if (parts[0] == "0" && parts[3] == "YOU") { currentPlayerID = parts[2]; continue; }
                if (!current.instances.Contains(int.Parse(parts[1]))) { current.instances.Add(int.Parse(parts[1])); }
                if (int.Parse(parts[7]) < 0) { continue; }
                if (parts[2] == "0" || uint.Parse(parts[6]) == 0) { continue; }
                if (Sepid.IgnoreAtkID.Contains(uint.Parse(parts[6]))) { continue; }
                if (Properties.Settings.Default.Onlyme && parts[2] != currentPlayerID) { continue; }

                int lineTimestamp = int.Parse(parts[0]);
                string sourceID = parts[2];
                string sourceName = parts[3];
                string targetID = parts[4];
                string targetName = parts[5];
                uint attackID = uint.Parse(parts[6]); //WriteLog()にてID<->Nameの相互変換がある為int化が無理.......なはずだった
                int hitDamage = int.Parse(parts[7]);
                byte JA = byte.Parse(parts[8]);
                byte Cri = byte.Parse(parts[9]);
                int index = -1;

                if (sourceID == currentPlayerID) { userattacks.Add(new Hit(attackID, hitDamage, JA, Cri)); }
                if (currentPlayerName == null && sourceID == currentPlayerID) { currentPlayerName = sourceName; }

                //処理スタート
                if (10000000 < int.Parse(sourceID)) //Player->Enemy
                {
                    if (!IsRunning) { current = new Session(); }
                    current.newTimestamp = lineTimestamp; //newTimestampは常に最新
                    if (current.startTimestamp == 0) //初期が0だった場合
                    {
                        current.startTimestamp = current.newTimestamp;
                        current.nowTimestamp = current.newTimestamp;
                    }

                    if (current.newTimestamp - current.nowTimestamp >= 1)
                    {
                        current.diffTime = current.diffTime + 1;
                        current.nowTimestamp = current.newTimestamp;
                    }

                    if (Properties.Settings.Default.QuestTime) { current.ActiveTime = current.diffTime; } else { current.ActiveTime = current.newTimestamp - current.startTimestamp; }

                    foreach (Player x in current.players) { if (x.ID == sourceID && x.isTemporary == "raw") { index = current.players.IndexOf(x); } }
                    if (index == -1)
                    {
                        current.players.Add(new Player(sourceID, sourceName, "raw"));
                        index = current.players.Count - 1;
                    }

                    Player source = current.players[index];
                    if (Sepid.DBAtkID.Contains(attackID)) { source.DBDamage += hitDamage; current.totalDBDamage += hitDamage; source.DBAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else if (Sepid.LswAtkID.Contains(attackID)) { source.LswDamage += hitDamage; current.totalLswDamage += hitDamage; source.LswAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else if (Sepid.PwpAtkID.Contains(attackID)) { source.PwpDamage += hitDamage; current.totalPwpDamage += hitDamage; source.PwpAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else if (Sepid.AISAtkID.Contains(attackID)) { source.AisDamage += hitDamage; current.totalAisDamage += hitDamage; source.AisAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else if (Sepid.RideAtkID.Contains(attackID)) { source.RideDamage += hitDamage; current.totalRideDamage += hitDamage; source.RideAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); } else { source.AllyDamage += hitDamage; current.totalAllyDamage += hitDamage; source.AllyAttacks.Add(new Hit(attackID, hitDamage, JA, Cri)); }
                    if (attackID == 2106601422) { source.ZvsDamage += hitDamage; current.totalZanverse += hitDamage; }
                    if (Sepid.HTFAtkID.Contains(attackID)) { source.HTFDamage += hitDamage; current.totalFinish += hitDamage; }
                    current.totalDamage += hitDamage;
                    source.Damage += hitDamage;
                    source.Attacks.Add(new Hit(attackID, hitDamage, JA, Cri));
                    IsRunning = true;
                } else
                {
                    if (!IsRunning) { continue; }
                    if (10000000 < int.Parse(targetID)) //Enemy->Player
                    {
                        foreach (Player x in current.players) { if (x.ID == targetID && x.isTemporary == "raw") { index = current.players.IndexOf(x); } }
                        if (index == -1)
                        {
                            current.players.Add(new Player(targetID, targetName, "raw"));
                            index = current.players.Count - 1;
                        }
                        Player source = current.players[index];
                        source.Damaged += hitDamage;
                    }
                }
            }

            current.players.Sort((x, y) => y.ReadDamage.CompareTo(x.ReadDamage));

            //if (current.startTimestamp != 0) { encounterData = "0:00:00 - ∞ DPS"; }
            return true;
        }

        public async void UpdateForm(object sender, EventArgs e)
        {
            if (current.players == null) { return; }
            damageTimer.Stop();
            if (Properties.Settings.Default.Clock) { Datetime.Content = DateTime.Now.ToString("HH:mm:ss.ff"); }

            bool IsLogContain = await UpdateLog(this, null);
            if (IsLogContain == false && 10 < noUpdateCount) { noUpdateCount++; StatusUpdate(); damageTimer.Start(); return; }

            noUpdateCount = 0;

            // get a copy of the right combatants
            workingList = new List<Player>(current.players);

            //Separate Part
            if (Properties.Settings.Default.SeparateDB && 0 < current.totalDBDamage) { SeparateDBMethod(); }
            if (Properties.Settings.Default.SeparateLsw && 0 < current.totalLswDamage) { SeparateLswAsync(); }
            if (Properties.Settings.Default.SeparatePwp && 0 < current.totalPwpDamage) { SeparatePwpAsync(); }
            if (Properties.Settings.Default.SeparateAIS && 0 < current.totalAisDamage) { SeparateAISAsync(); }
            if (Properties.Settings.Default.SeparateRide && 0 < current.totalRideDamage) { SeparateRideAsync(); }

            //分けたものを含めて再ソート(ザンバース,HTFを最後にする為)
            if (SeparateTab.SelectedIndex == 0) { workingList.Sort((x, y) => y.ReadDamage.CompareTo(x.ReadDamage)); }

            if (Properties.Settings.Default.SeparateFinish && 0 < current.totalFinish) { SeparateHTFAsync(); }
            if (Properties.Settings.Default.SeparateZanverse && 0 < current.totalZanverse) { SeparateZvsAsync(); }

            // get group damage totals
            Int64 totalReadDamage = workingList.Sum(x => x.ReadDamage);
            Int64 totalAllyReadDamage = workingList.Where(p => p.isTemporary == "raw").Sum(x => x.ReadDamage);

            Int64 totalAVG;
            if (workingList.Any()) { totalAVG = totalAllyReadDamage / workingList.Count(p => p.isTemporary == "raw"); } else { totalAVG = 1; }

            // dps calcs!
            /* (Combatant c in workingList) */
            Parallel.ForEach(workingList, p =>
            {
                p.PercentReadDPS = p.ReadDamage / (double)totalReadDamage * 100;
                p.AllyPct = p.AllyDamage / (double)current.totalAllyDamage * 100;
                p.DBPct = p.DBDamage / (double)current.totalDBDamage * 100;
                p.LswPct = p.LswDamage / (double)current.totalLswDamage * 100;
                p.PwpPct = p.PwpDamage / (double)current.totalPwpDamage * 100;
                p.AisPct = p.AisDamage / (double)current.totalAisDamage * 100;
                p.RidePct = p.RideDamage / (double)current.totalRideDamage * 100;
                p.TScore = Math.Pow(Math.Abs(p.ReadDamage - totalAVG), 2);
            });

            if (workingList.Where(p => p.isTemporary == "raw").Any())
            {
                current.totalSD = Math.Sqrt(workingList.Where(p => p.isTemporary == "raw").Average(x => x.TScore));
                foreach (Player c in workingList)
                {
                    double temp = Math.Abs(c.ReadDamage - totalAVG) * 10 / current.totalSD;
                    if (c.ReadDamage < totalAVG) { c.TScore = 50.0 - temp; } else if (totalAVG < c.ReadDamage) { c.TScore = 50.0 + temp; } else if (totalAVG == c.ReadDamage) { c.TScore = 50.00; } else { c.TScore = 00.00; }
                }
            }

            // status pane updates
            StatusUpdate();

            //damage graph stuff
            current.firstDamage = 0;
            // clear out the list
            CombatantData.Items.Clear();
            AllyData.Items.Clear();
            DBData.Items.Clear();
            LswData.Items.Clear();
            PwpData.Items.Clear();
            AisData.Items.Clear();
            RideData.Items.Clear();

            foreach (Player c in workingList)
            {
                if (c.IsAlly && current.firstDamage < c.ReadDamage) { current.firstDamage = c.ReadDamage; }

                bool filtered = true;
                if (Properties.Settings.Default.SeparateDB || Properties.Settings.Default.SeparateLsw || Properties.Settings.Default.SeparatePwp || Properties.Settings.Default.SeparateAIS || Properties.Settings.Default.SeparateRide)
                {
                    if (c.IsAlly && c.isTemporary == "raw" && !HidePlayers.IsChecked) { filtered = false; }
                    if (c.IsAlly && c.isTemporary == "AIS" && !HideAIS.IsChecked) { filtered = false; }
                    if (c.IsAlly && c.isTemporary == "DB" && !HideDB.IsChecked) { filtered = false; }
                    if (c.IsAlly && c.isTemporary == "Ride" && !HideRide.IsChecked) { filtered = false; }
                    if (c.IsAlly && c.isTemporary == "Pwp" && !HidePwp.IsChecked) { filtered = false; }
                    if (c.IsAlly && c.isTemporary == "Lsw" && !HideLsw.IsChecked) { filtered = false; }
                    if (c.IsZanverse) { filtered = false; }
                    if (c.IsFinish) { filtered = false; }
                } else
                {
                    if (c.IsAlly || c.IsZanverse || c.IsFinish) { filtered = false; }
                }

                if (!filtered && (0 < c.ReadDamage) && (SeparateTab.SelectedIndex == 0)) { CombatantData.Items.Add(c); }
                if ((0 < c.AllyDamage) && (SeparateTab.SelectedIndex == 1)) { workingList.Sort((x, y) => y.AllyDamage.CompareTo(x.AllyDamage)); AllyData.Items.Add(c); }
                if ((0 < c.DBDamage) && (SeparateTab.SelectedIndex == 2)) { workingList.Sort((x, y) => y.DBDamage.CompareTo(x.DBDamage)); DBData.Items.Add(c); }
                if ((0 < c.LswDamage) && (SeparateTab.SelectedIndex == 3)) { workingList.Sort((x, y) => y.LswDamage.CompareTo(x.LswDamage)); LswData.Items.Add(c); }
                if ((0 < c.PwpDamage) && (SeparateTab.SelectedIndex == 4)) { workingList.Sort((x, y) => y.PwpDamage.CompareTo(x.PwpDamage)); PwpData.Items.Add(c); }
                if ((0 < c.AisDamage) && (SeparateTab.SelectedIndex == 5)) { workingList.Sort((x, y) => y.AisDamage.CompareTo(x.AisDamage)); AisData.Items.Add(c); }
                if ((0 < c.RideDamage) && (SeparateTab.SelectedIndex == 6)) { workingList.Sort((x, y) => y.RideDamage.CompareTo(x.RideDamage)); RideData.Items.Add(c); }
            }

            //TTS
            if (Properties.Settings.Default.Bouyomi && IsConnect && IsRunning)
            {
                Player me = workingList.Where(p => p.ID == currentPlayerID && p.isTemporary == "raw").FirstOrDefault();
                int nexttime = speechcount * Properties.Settings.Default.BouyomiSpan;
                if (current.ActiveTime != 0 && me != null && nexttime < current.ActiveTime)
                {
                    speechcount = current.ActiveTime / Properties.Settings.Default.BouyomiSpan + 1;
                    string text = (nexttime / 60).ToString() + "分";
                    if (nexttime % 60 != 0) { text += (nexttime % 60).ToString() + "秒"; }
                    text += "経過　";
                    if (!Properties.Settings.Default.BouyomiFormat)
                    {
                        text += me.DPS.ToString("N0") + "DPS　";
                        text += (workingList.IndexOf(me) + 1).ToString() + "位";
                    } else if (Properties.Settings.Default.BouyomiFormat)
                    {
                        text += me.DPS.ToString("N0") + "でぃーぴーえす　";
                        text += (workingList.IndexOf(me) + 1).ToString() + "い";
                    }
                    ReadUp(text);
                }

            }

            if (150 < userattacks.Count && Isautodel) { userattacks.RemoveRange(49, 100); }


            // autoend
            if (Properties.Settings.Default.AutoEndEncounters && IsRunning)
            {
                int unixTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if ((unixTimestamp - current.newTimestamp) >= Properties.Settings.Default.EncounterTimeout) { EndEncounter_Click(null, null); }
            }
            damageTimer.Start();
        }

        private void StatusUpdate()
        {
            if (!Directory.Exists(Properties.Settings.Default.Path + "\\damagelogs") || !damagelogs.GetFiles().Any())
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 128, 128));
                EncounterStatus.Content = "Directory No Logs : (Re)Start PSO2, Attack Enemy  or  Failed dll plugin Install";
            } else if (!IsRunning)
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 64, 192, 64));
                EncounterStatus.Content = $"Waiting - {lastStatus}";
                if (lastStatus == "") { EncounterStatus.Content = "Waiting... - " + damagelogcsv.Name; }
            } else if (IsRunning)
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 192, 255));
                TimeSpan timespan = TimeSpan.FromSeconds(current.ActiveTime);
                string timer = timespan.ToString(@"h\:mm\:ss");
                EncounterStatus.Content = $"{timer}";

                double totalDPS = current.totalDamage / (double)current.ActiveTime;
                if (totalDPS > 0) { EncounterStatus.Content += $" - Total : {current.totalDamage.ToString("N0")}" + $" - {totalDPS.ToString("N0")} DPS"; }
                if (!Properties.Settings.Default.SeparateZanverse) { EncounterStatus.Content += $" - Zanverse : {current.totalZanverse.ToString("N0")}"; }
                lastStatus = EncounterStatus.Content.ToString();
            }
        }

        #region Separate
        private void SeparateDBMethod()
        {
            List<Player> addDBPlayer = new List<Player>();
            foreach (Player p in workingList)
            {
                if (p.isTemporary != "raw") { continue; }
                if (0 < p.DBDamage)
                {
                    Player separate = new Player(p.ID, "DB|" + p.Name, "DB");
                    List<Hit> targetAttacks = p.Attacks.Where(a => Sepid.DBAtkID.Contains(a.ID)).ToList();
                    p.Attacks = p.Attacks.Except(targetAttacks).ToList();
                    separate.Damage = p.DBDamage;
                    separate.Damaged = p.Damaged;
                    separate.Attacks = p.DBAttacks;
                    addDBPlayer.Add(separate);
                }
            }
            workingList.AddRange(addDBPlayer);
        }

        private void SeparateLswAsync()
        {
            List<Player> addLswPlayer = new List<Player>();
            foreach (Player p in workingList)
            {
                if (p.isTemporary != "raw") { continue; }
                if (0 < p.LswDamage)
                {
                    Player separate = new Player(p.ID, "Lsw|" + p.Name, "Lsw");
                    List<Hit> targetAttacks = p.Attacks.Where(a => Sepid.LswAtkID.Contains(a.ID)).ToList();
                    p.Attacks = p.Attacks.Except(targetAttacks).ToList();
                    separate.Damage = p.LswDamage;
                    separate.Damaged = p.Damaged;
                    separate.Attacks = p.LswAttacks;
                    addLswPlayer.Add(separate);
                }
            }
            workingList.AddRange(addLswPlayer);
        }

        private void SeparatePwpAsync()
        {
            List<Player> addPwpPlayer = new List<Player>();
            foreach (Player p in workingList)
            {
                if (p.isTemporary != "raw") { continue; }
                if (0 < p.PwpDamage)
                {
                    Player separate = new Player(p.ID, "Pwp|" + p.Name, "Pwp");
                    List<Hit> targetAttacks = p.Attacks.Where(a => Sepid.PwpAtkID.Contains(a.ID)).ToList();
                    p.Attacks = p.Attacks.Except(targetAttacks).ToList();
                    separate.Damage = p.PwpDamage;
                    separate.Damaged = p.Damaged;
                    separate.Attacks = p.PwpAttacks;
                    addPwpPlayer.Add(separate);
                }
            }
            workingList.AddRange(addPwpPlayer);
        }

        private void SeparateAISAsync()
        {
            List<Player> addAISPlayer = new List<Player>();
            foreach (Player p in workingList)
            {
                if (p.isTemporary != "raw") { continue; }
                if (0 < p.AisDamage)
                {
                    Player separate = new Player(p.ID, "AIS|" + p.Name, "AIS");
                    List<Hit> targetAttacks = p.Attacks.Where(a => Sepid.AISAtkID.Contains(a.ID)).ToList();
                    p.Attacks = p.Attacks.Except(targetAttacks).ToList();
                    separate.Damage = p.AisDamage;
                    separate.Damaged = p.Damaged;
                    separate.Attacks = p.AisAttacks;
                    addAISPlayer.Add(separate);
                }
            }
            workingList.AddRange(addAISPlayer);
        }

        private void SeparateRideAsync()
        {
            List<Player> addRidePlayer = new List<Player>();
            foreach (Player p in workingList)
            {
                if (p.isTemporary != "raw") { continue; }
                if (0 < p.RideDamage)
                {
                    Player separate = new Player(p.ID, "Ride|" + p.Name, "Ride");
                    List<Hit> targetAttacks = p.Attacks.Where(a => Sepid.RideAtkID.Contains(a.ID)).ToList();
                    p.Attacks = p.Attacks.Except(targetAttacks).ToList();
                    separate.Damage = p.RideDamage;
                    separate.Damaged = p.Damaged;
                    separate.Attacks = p.RideAttacks;
                    addRidePlayer.Add(separate);
                }
            }
            workingList.AddRange(addRidePlayer);
        }

        private void SeparateHTFAsync()
        {
            Player addHTFPlayer = new Player("99999998", "HTF Attacks", "HTF");
            foreach (Player p in workingList)
            {
                if (p.isTemporary != "raw") { continue; }
                if (0 < current.totalFinish)
                {
                    List<Hit> targetAttacks = p.Attacks.Where(a => Sepid.HTFAtkID.Contains(a.ID)).ToList();
                    addHTFPlayer.Damage += p.HTFDamage;
                    addHTFPlayer.Attacks.AddRange(targetAttacks);
                }
            }
            workingList.Add(addHTFPlayer);
        }

        private void SeparateZvsAsync()
        {
            Player addZvsPlayer = new Player("99999999", "Zanverse", "Zvs");
            foreach (Player p in workingList)
            {
                if (p.isTemporary != "raw") { continue; }
                if (0 < current.totalZanverse)
                {
                    List<Hit> targetAttacks = p.Attacks.Where(a => a.ID == 2106601422).ToList();
                    addZvsPlayer.Damage += p.ZvsDamage;
                    addZvsPlayer.Attacks.AddRange(targetAttacks);
                }
            }
            workingList.Add(addZvsPlayer);
        }
        #endregion

        public string WriteLog()
        {
            if (current.players.Count == 0) { return null; }
            if (current.ActiveTime == 0) { current.ActiveTime = 1; }
            string timer = TimeSpan.FromSeconds(current.ActiveTime).ToString(@"mm\:ss");
            string log = DateTime.Now.ToString("F") + " | " + timer + " | TotalDamage : " + current.totalDamage.ToString("N0") + " | TotalDPS : " + current.totalDPS.ToString("N0") + " | 標準偏差 : " + current.totalSD.ToString("00.00") + Environment.NewLine + Environment.NewLine;

            foreach (Player c in workingList)
            {
                try
                {
                    if (Properties.Settings.Default.IsWriteTS)
                    {
                        log += $"{c.Name} | {c.RatioPercent}% | 偏差値:{c.ReadTScore} | {c.ReadDamage.ToString("N0")} dmg | {c.Writedmgd} dmgd | {c.DPS} DPS | JA: {c.WJAPercent}% | Critical: {c.WCRIPercent}% | Max: {c.WriteMaxdmg} ({c.MaxHit})" + Environment.NewLine;
                    }

                    if (!Properties.Settings.Default.IsWriteTS)
                    {
                        log += $"{c.Name} | {c.RatioPercent}% | {c.ReadDamage.ToString("N0")} dmg | {c.Writedmgd} dmgd | {c.DPS} DPS | JA: {c.WJAPercent}% | Critical: {c.WCRIPercent}% | Max: {c.WriteMaxdmg} ({c.MaxHit})" + Environment.NewLine;
                    }
                } catch {/* 今の所何もしないっぽい */}
            }

            log += Environment.NewLine + Environment.NewLine;

            foreach (Player c in workingList)
            {
                List<string> attackNames = new List<string>();
                List<string> finishNames = new List<string>();
                List<Tuple<string, List<int>, List<byte>, List<byte>>> attackData = new List<Tuple<string, List<int>, List<byte>, List<byte>>>();

                log += $"[ {c.Name} - {c.RatioPercent}% - {c.ReadDamage.ToString("N0")} dmg ]" + Environment.NewLine + Environment.NewLine;

                if (Properties.Settings.Default.SeparateZanverse && c.IsZanverse)
                {
                    foreach (Player zvs in current.players) { if (0 < zvs.ZvsDamage) { attackNames.Add(zvs.ID); } }
                    foreach (string s in attackNames)
                    {
                        Player zvsPlayer = current.players.First(x => x.ID == s);
                        List<int> matchingAttacks = zvsPlayer.Attacks.Where(atk => atk.ID == 2106601422).Select(a => a.Damage).ToList();
                        List<byte> jaPercents = new List<byte> { 0 }; List<byte> criPercents = c.Attacks.Where(a => a.ID == 2106601422).Select(a => a.Cri).ToList();
                        attackData.Add(new Tuple<string, List<int>, List<byte>, List<byte>>(zvsPlayer.Name, matchingAttacks, jaPercents, criPercents));
                    }
                } else if (Properties.Settings.Default.SeparateFinish && c.IsFinish)
                {
                    foreach (Player htf in current.players) { if (0 < htf.HTFDamage) { finishNames.Add(htf.ID); } }
                    foreach (string htf in finishNames)
                    {
                        Player htfPlayer = current.players.First(x => x.ID == htf);
                        List<int> fmatchingAttacks = htfPlayer.Attacks.Where(a => Sepid.HTFAtkID.Contains(a.ID)).Select(a => a.Damage).ToList();
                        List<byte> jaPercents = c.Attacks.Where(a => Sepid.HTFAtkID.Contains(a.ID)).Select(a => a.JA).ToList();
                        List<byte> criPercents = c.Attacks.Where(a => Sepid.HTFAtkID.Contains(a.ID)).Select(a => a.Cri).ToList();
                        attackData.Add(new Tuple<string, List<int>, List<byte>, List<byte>>(htfPlayer.Name, fmatchingAttacks, jaPercents, criPercents));
                    }
                } else
                {
                    List<PAHit> temphits = new List<PAHit>();
                    foreach (Hit atk in c.Attacks)
                    {
                        //PAID -> PAName
                        string temp = atk.ID.ToString();
                        if ((Properties.Settings.Default.SeparateZanverse && atk.ID == 2106601422) || (Properties.Settings.Default.SeparateFinish && Sepid.HTFAtkID.Contains(atk.ID))) { continue; } //ザンバースの場合に何もしない
                        if (skillDict.ContainsKey(atk.ID)) { temp = skillDict[atk.ID]; } // these are getting disposed anyway, no 1 cur
                        if (!attackNames.Contains(temp)) { attackNames.Add(temp); }
                        temphits.Add(new PAHit(temp, atk.Damage, atk.JA, atk.Cri));
                    }

                    foreach (string paname in attackNames)
                    {
                        //マッチングアタックからダメージを選択するだけ
                        List<int> matchingAttacks = temphits.Where(a => a.Name == paname).Select(a => a.Damage).ToList();
                        List<byte> jaPercents = temphits.Where(a => a.Name == paname).Select(a => a.JA).ToList();
                        List<byte> criPercents = temphits.Where(a => a.Name == paname).Select(a => a.Cri).ToList();
                        attackData.Add(new Tuple<string, List<int>, List<byte>, List<byte>>(paname, matchingAttacks, jaPercents, criPercents));
                    }
                }

                attackData = attackData.OrderByDescending(x => x.Item2.Sum()).ToList();

                foreach (var i in attackData)
                {
                    List<int> exja = i.Item3.ConvertAll(x => (int)x);
                    List<int> excri = i.Item4.ConvertAll(x => (int)x);

                    string min, max, avg, ja, cri;
                    double percent = i.Item2.Sum() * 100d / c.Damage;
                    string spacer = (percent >= 9) ? "" : " ";

                    string paddedPercent = percent.ToString("00.00").Substring(0, 5);
                    string hits = i.Item2.Count().ToString("N0");
                    string sum = i.Item2.Sum().ToString("N0");
                    if (i.Item2.Any())
                    {
                        min = i.Item2.Min().ToString("N0");
                        max = i.Item2.Max().ToString("N0");
                        avg = i.Item2.Average().ToString("N0");
                        ja = (exja.Average() * 100).ToString("N2");
                        cri = (excri.Average() * 100).ToString("N2");
                    } else
                    {
                        min = "0";
                        max = "0";
                        avg = "0";
                        ja = "0.00";
                        cri = "0.00";
                    }

                    log += $"{paddedPercent}%	| {i.Item1} - {sum} dmg";
                    log += $" - JA : {ja}% - Critical : {cri}%";
                    log += Environment.NewLine;
                    log += $"	|   {hits} hits - {min} min, {avg} avg, {max} max" + Environment.NewLine;
                }

                log += Environment.NewLine;
            }


            log += "Instance IDs: " + String.Join(", ", current.instances.ToArray());

            DateTime thisDate = DateTime.Now;
            string directory = string.Format("{0:yyyy-MM-dd}", DateTime.Now);
            Directory.CreateDirectory($"Logs/{directory}");
            string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
            string filename = $"Logs/{directory}/dDPSm - {datetime}.txt";
            File.WriteAllText(filename, log);


            return filename;
        }

        public string ExportWriteLog()
        {
            return "";
            /*
            if (current.players.Count == 0) { return null; }
            if (current.ActiveTime == 0) { current.ActiveTime = 1; }
            string timer = TimeSpan.FromSeconds(current.ActiveTime).ToString(@"mm\:ss");
            string log = timer + " | TotalDamage : " + current.totalDamage.ToString("N0") + " | TotalDPS : " + current.totalDPS.ToString("N0") + " | 標準偏差 : " + current.totalSD.ToString("00.00") + Environment.NewLine + Environment.NewLine;
            log += "Num/Ratio/Dmg/Dmgd/DPS/JA/Cri/Max" + Environment.NewLine;
            byte rankcount = 0;

            foreach (Player p in workingList)
            {
                rankcount++;
                log += $"#{rankcount.ToString("X")} {p.RatioPercent}% {p.ReadTScore} {p.ReadDamage.ToString("N0")} {p.BindDamaged} {p.DPS} {p.WJAPercent} {p.WCRIPercent} {p.MaxHitdmg} ({p.MaxHit})" + Environment.NewLine;
            }

            log += Environment.NewLine + Environment.NewLine;

            log += "Instance IDs: " + string.Join(", ", current.instances.ToArray());

            DateTime thisDate = DateTime.Now;
            string directory = string.Format("{0:yyyy-MM-dd}", DateTime.Now);
            Directory.CreateDirectory($"Export/{directory}");
            string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
            string filename = $"Export/{directory}/Export - {datetime}.txt";
            File.WriteAllText(filename, log);

            return filename;
            */
        }

        private void ReadUp(string text)
        {
            byte bCode = 0;
            short iVoice = 1;
            short iVolume = -1;
            short iSpeed = -1;
            short iTone = -1;
            short iCommand = 0x0001;

            byte[] bMessage = Encoding.UTF8.GetBytes(text);
            Int32 iLength = bMessage.Length;

            //棒読みちゃんのTCPサーバへ接続
            string sHost = "127.0.0.1"; //棒読みちゃんが動いているホスト
            int iPort = 50001;       //棒読みちゃんのTCPサーバのポート番号(デフォルト値)
            TcpClient tc = null;
            try
            {
                tc = new TcpClient(sHost, iPort);
            } catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            try
            {
                if (tc != null)
                {
                    //メッセージ送信
                    using (NetworkStream ns = tc.GetStream())
                    {
                        using (BinaryWriter bw = new BinaryWriter(ns))
                        {
                            bw.Write(iCommand); //コマンド（ 0:メッセージ読み上げ）
                            bw.Write(iSpeed);   //速度    （-1:棒読みちゃん画面上の設定）
                            bw.Write(iTone);    //音程    （-1:棒読みちゃん画面上の設定）
                            bw.Write(iVolume);  //音量    （-1:棒読みちゃん画面上の設定）
                            bw.Write(iVoice);   //声質    （ 0:棒読みちゃん画面上の設定、1:女性1、2:女性2、3:男性1、4:男性2、5:中性、6:ロボット、7:機械1、8:機械2、10001～:SAPI5）
                            bw.Write(bCode);    //文字列のbyte配列の文字コード(0:UTF-8, 1:Unicode, 2:Shift-JIS)
                            bw.Write(iLength);  //文字列のbyte配列の長さ
                            bw.Write(bMessage); //文字列のbyte配列
                        }
                    }

                    //切断
                    tc.Close();
                }
            } catch
            {

            }

        }

        public struct PAHit //Use WriteLog Only
        {
            public string Name;
            public int Damage;
            public byte JA, Cri;
            public PAHit(string paname, int dmg, byte ja, byte cri) { Name = paname; Damage = dmg; JA = ja; Cri = cri; }
        }


    }
}
