using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace dDPSm
{
    public static class Sepid
    {
        public static uint[] DBAtkID, LswAtkID, PwpAtkID, HTFAtkID, AISAtkID, RideAtkID, IgnoreAtkID;
    }

    public struct Hit //10byte
    {
        public uint ID;
        public int Damage;
        public byte JA, Cri;
        public Hit(uint paid, int dmg, byte ja, byte cri) { ID = paid; Damage = dmg; JA = ja; Cri = cri; }

        public string ReadID => ID.ToString();
        public string IDName
        {
            get
            {
                string paname = "Unknown";
                if (MainWindow.skillDict.ContainsKey(ID)) { paname = MainWindow.skillDict[ID]; }
                return paname;
            }
        }

        public string ReadDamage => Damage.ToString("N0");
        public string IsJA { get { if (JA == 1) { return "True"; } else { return "False"; } } }
        public string IsCri { get { if (Cri == 1) { return "True"; } else { return "False"; } } }
    }

    public class Player //Welcome to Property Hell
    {
        private const float maxBGopacity = 0.6f;
        public string ID, Name, isTemporary;
        public double PercentDPS, PercentReadDPS, AllyPct, DBPct, LswPct, PwpPct, AisPct, RidePct, TScore;
        public List<Hit> Attacks, AllyAttacks, DBAttacks, LswAttacks, PwpAttacks, AisAttacks, RideAttacks;
        public Int64 Damage, Damaged, ZvsDamage, HTFDamage, AllyDamage, DBDamage, LswDamage, PwpDamage, AisDamage, RideDamage;

        public bool IsYou => ID == MainWindow.currentPlayerID;
        public string DisplayName
        {
            get
            {
                return Name;
            }
        }

        public string RatioPercent => $"{PercentReadDPS:00.00}";
        public string ReadTScore
        {
            get
            {
                if (isTemporary == "raw") { return $"{TScore:00.00}"; } else { return "--"; }
            }
        }

        //public Int64 Damage => Attacks.Sum(x => x.Damage);
        //public Int64 ZvsDamage => Attacks.Where(a => a.ID == "2106601422").Sum(x => x.Damage);
        //public Int64 HTFDamage => Attacks.Where(a => Sepid.HTFAtkID.Contains(a.ID)).Sum(x => x.Damage);
        public Int64 ReadDamage
        {
            get
            {
                if (IsZanverse || IsFinish || IsDB || IsLsw || IsPwp || IsAIS || IsRide) { return Damage; }

                Int64 temp = Damage;
                if (Properties.Settings.Default.SeparateFinish) { temp -= HTFDamage; }
                if (Properties.Settings.Default.SeparateZanverse) { temp -= ZvsDamage; }
                if (Properties.Settings.Default.SeparateDB) { temp -= DBDamage; }
                if (Properties.Settings.Default.SeparateLsw) { temp -= LswDamage; }
                if (Properties.Settings.Default.SeparatePwp) { temp -= PwpDamage; }
                if (Properties.Settings.Default.SeparateAIS) { temp -= AisDamage; }
                if (Properties.Settings.Default.SeparateRide) { temp -= RideDamage; }
                return temp;
            }
        }

        public string BindDamage
        {
            get
            {
                if (Properties.Settings.Default.DamageSI)
                {
                    return FormatDmg(ReadDamage);
                } else { return ReadDamage.ToString("N0"); }
            }
        }

        public string BindDamaged
        {
            get
            {
                if (Properties.Settings.Default.DamagedSI)
                {
                    return FormatDmg(Damaged);
                } else { return Damaged.ToString("N0"); }
            }
        }

        public double DPS => Damage / MainWindow.current.ActiveTime;
        public double ReadDPS => Math.Round(ReadDamage / (double)MainWindow.current.ActiveTime);
        public string StringDPS => ReadDPS.ToString("N0");
        public string FDPSReadout
        {
            get
            {
                if (Properties.Settings.Default.DPSSI)
                {
                    return FormatNumber(ReadDPS);
                } else
                {
                    return StringDPS;
                }
            }
        }

        public string JAPercent
        {
            get
            {
                try
                {
                    if (Properties.Settings.Default.Nodecimal) { return ((Attacks.Where(a => !MainWindow.jaignoreskill.Contains(a.ID.ToString())).Average(x => x.JA)) * 100).ToString("N0"); } else { return ((Attacks.Where(a => !MainWindow.jaignoreskill.Contains(a.ID.ToString())).Average(x => x.JA)) * 100).ToString("N2"); }
                } catch { return "Error"; }
            }
        }

        public string WJAPercent
        {
            get
            {
                try { return ((Attacks.Where(a => !MainWindow.jaignoreskill.Contains(a.ID.ToString())).Average(x => x.JA)) * 100).ToString("N2"); } catch { return "Error"; }
            }
        }

        public string CRIPercent
        {
            get
            {
                try
                {
                    if (Properties.Settings.Default.Nodecimal) { return ((Attacks.Where(a => !MainWindow.critignoreskill.Contains(a.ID.ToString())).Average(x => x.Cri)) * 100).ToString("N0"); } else { return ((Attacks.Where(a => !MainWindow.critignoreskill.Contains(a.ID.ToString())).Average(x => x.Cri)) * 100).ToString("N2"); }
                } catch { return "Error"; }
            }
        }

        public string WCRIPercent
        {
            get
            {
                try { return ((Attacks.Average(a => a.Cri)) * 100).ToString("00.00"); } catch { return "Error"; }
            }
        }

        public bool IsAlly => (int.Parse(ID) >= 10000000) && !IsZanverse && !IsFinish;
        public bool IsZanverse => (isTemporary == "Zvs");
        public bool IsFinish => (isTemporary == "HTF");
        public bool IsDB => (isTemporary == "DB");
        public bool IsLsw => (isTemporary == "Lsw");
        public bool IsPwp => (isTemporary == "Pwp");
        public bool IsAIS => (isTemporary == "AIS");
        public bool IsRide => (isTemporary == "Ride");

        public Hit MaxHitAttack
        {
            get
            {
                Attacks.Sort((x, y) => y.Damage.CompareTo(x.Damage));
                return Attacks.FirstOrDefault();
            }
        }

        public string MaxHitdmg
        {
            get
            {
                try
                {
                    if (Properties.Settings.Default.MaxSI)
                    {
                        return FormatDmg(MaxHitAttack.Damage);
                    } else { return MaxHitAttack.Damage.ToString("N0"); }
                } catch { return "Error"; }
            }
        }

        public string MaxHit
        {
            get
            {
                //if (MaxHitAttack == null) { return "--"; }
                string attack = "Unknown";
                if (MainWindow.skillDict.ContainsKey(MaxHitAttack.ID))
                {
                    attack = MainWindow.skillDict[MaxHitAttack.ID];
                }
                return MaxHitAttack.Damage.ToString(attack);
            }
        }

        #region WriteProperties
        public string Writedmgd => Damaged.ToString("N0");
        public string WriteMaxdmg
        {
            get
            {
                try { return MaxHitAttack.Damage.ToString("N0"); } catch { return "Error"; }
            }
        }


        #endregion

        //Separate
        #region 

        //Ally Data
        public string AllyReadPct => AllyPct.ToString("N2");
        public string AllyReadDamage => AllyDamage.ToString("N0");
        public string AllyDPS => Math.Round(AllyDamage / (double)MainWindow.current.ActiveTime).ToString("N0");
        public string AllyJAPct => (AllyAttacks.Where(a => !MainWindow.jaignoreskill.Contains(a.ID.ToString())).Average(x => x.JA) * 100).ToString("N2");
        public string AllyCriPct => (AllyAttacks.Where(a => !MainWindow.critignoreskill.Contains(a.ID.ToString())).Average(x => x.Cri) * 100).ToString("N2");
        public string AllyMaxHitdmg => AllyMaxHit.Damage.ToString("N0");
        public string AllyAtkName
        {
            get
            {
                //if (AllyMaxHit == null) { return "--"; }
                string attack = "Unknown";
                if (MainWindow.skillDict.ContainsKey(AllyMaxHit.ID)) { attack = MainWindow.skillDict[AllyMaxHit.ID]; }
                return AllyMaxHit.Damage.ToString(attack);
            }
        }
        public Hit AllyMaxHit
        {
            get
            {
                AllyAttacks.Sort((x, y) => y.Damage.CompareTo(x.Damage));
                if (AllyAttacks != null)
                {
                    return AllyAttacks.FirstOrDefault();
                } else
                {
                    return new Hit();
                }
            }
        }

        //DarkBlast Data
        public string DBReadPct => DBPct.ToString("N2");
        public string DBReadDamage => DBDamage.ToString("N0");
        public string DBDPS => Math.Round(DBDamage / (double)MainWindow.current.ActiveTime).ToString("N0");
        public string DBJAPct => (DBAttacks.Average(x => x.JA) * 100).ToString("N2");
        public string DBCriPct => (DBAttacks.Average(x => x.Cri) * 100).ToString("N2");
        public string DBMaxHitdmg => DBMaxHit.Damage.ToString("N0");
        public string DBAtkName
        {
            get
            {
                //if (DBMaxHit == null) { return "--"; }
                string attack = "Unknown";
                if (MainWindow.skillDict.ContainsKey(DBMaxHit.ID)) { attack = MainWindow.skillDict[DBMaxHit.ID]; }
                return DBMaxHit.Damage.ToString(attack);
            }
        }
        public Hit DBMaxHit
        {
            get
            {
                DBAttacks.Sort((x, y) => y.Damage.CompareTo(x.Damage));
                if (DBAttacks != null)
                {
                    return DBAttacks.FirstOrDefault();
                } else
                {
                    return new Hit();
                    //return null;
                }
            }
        }

        //Laconium sword Data
        public string LswReadPct => LswPct.ToString("N2");
        public string LswReadDamage => LswDamage.ToString("N0");
        public string LswDPS => Math.Round(LswDamage / (double)MainWindow.current.ActiveTime).ToString("N0");
        public string LswJAPct => (LswAttacks.Average(x => x.JA) * 100).ToString("N2");
        public string LswCriPct => (LswAttacks.Average(x => x.Cri) * 100).ToString("N2");
        public string LswMaxHitdmg => LswMaxHit.Damage.ToString("N0");
        public string LswAtkName
        {
            get
            {
                //if (LswMaxHit == null) { return "--"; }
                string attack = "Unknown";
                if (MainWindow.skillDict.ContainsKey(LswMaxHit.ID)) { attack = MainWindow.skillDict[LswMaxHit.ID]; }
                return LswMaxHit.Damage.ToString(attack);
            }
        }
        public Hit LswMaxHit
        {
            get
            {
                LswAttacks.Sort((x, y) => y.Damage.CompareTo(x.Damage));
                if (Attacks != null)
                {
                    return LswAttacks.FirstOrDefault();
                } else
                {
                    return new Hit();
                    //return null;
                }
            }
        }

        //PhotonWeapon Data
        public string PwpReadPct => PwpPct.ToString("N2");
        public string PwpReadDamage => PwpDamage.ToString("N0");
        public string PwpDPS => Math.Round(PwpDamage / (double)MainWindow.current.ActiveTime).ToString("N0");
        public string PwpJAPct => (PwpAttacks.Average(x => x.JA) * 100).ToString("N2");
        public string PwpCriPct => (PwpAttacks.Average(x => x.Cri) * 100).ToString("N2");
        public string PwpMaxHitdmg => PwpMaxHit.Damage.ToString("N0");
        public string PwpAtkName
        {
            get
            {
                //if (PwpMaxHit == null) { return "--"; }
                string attack = "Unknown";
                if (MainWindow.skillDict.ContainsKey(PwpMaxHit.ID)) { attack = MainWindow.skillDict[PwpMaxHit.ID]; }
                return PwpMaxHit.Damage.ToString(attack);
            }
        }
        public Hit PwpMaxHit
        {
            get
            {
                PwpAttacks.Sort((x, y) => y.Damage.CompareTo(x.Damage));
                if (Attacks != null)
                {
                    return PwpAttacks.FirstOrDefault();
                } else
                {
                    return new Hit();
                    //return null;
                }
            }
        }

        //AIS Data
        public string AisReadPct => AisPct.ToString("N2");
        public string AisReadDamage => AisDamage.ToString("N0");
        public string AisDPS => Math.Round(AisDamage / (double)MainWindow.current.ActiveTime).ToString("N0");
        public string AisJAPct => (AisAttacks.Average(x => x.JA) * 100).ToString("N2");
        public string AisCriPct => (AisAttacks.Average(x => x.Cri) * 100).ToString("N2");
        public string AisMaxHitdmg => AisMaxHit.Damage.ToString("N0");
        public string AisAtkName
        {
            get
            {
                //if (AisMaxHit == null) { return "--"; }
                string attack = "Unknown";
                if (MainWindow.skillDict.ContainsKey(AisMaxHit.ID)) { attack = MainWindow.skillDict[AisMaxHit.ID]; }
                return AisMaxHit.Damage.ToString(attack);
            }
        }
        public Hit AisMaxHit
        {
            get
            {
                AisAttacks.Sort((x, y) => y.Damage.CompareTo(x.Damage));
                if (Attacks != null)
                {
                    return AisAttacks.FirstOrDefault();
                } else
                {
                    return new Hit();
                    //return null;
                }
            }
        }

        //Ridroid Data
        public string RideReadPct => RidePct.ToString("N2");
        public string RideReadDamage => RideDamage.ToString("N0");
        public string RideDPS => Math.Round(RideDamage / (double)MainWindow.current.ActiveTime).ToString("N0");
        public string RideJAPct => (RideAttacks.Average(x => x.JA) * 100).ToString("N2");
        public string RideCriPct => (RideAttacks.Average(x => x.Cri) * 100).ToString("N2");
        public string RideMaxHitdmg => RideMaxHit.Damage.ToString("N0");
        public string RideAtkName
        {
            get
            {
                //if (RideMaxHit == null) { return "--"; }
                string attack = "Unknown";
                if (MainWindow.skillDict.ContainsKey(RideMaxHit.ID)) { attack = MainWindow.skillDict[RideMaxHit.ID]; }
                return RideMaxHit.Damage.ToString(attack);
            }
        }
        public Hit RideMaxHit
        {
            get
            {
                RideAttacks.Sort((x, y) => y.Damage.CompareTo(x.Damage));
                if (Attacks != null)
                {
                    return RideAttacks.FirstOrDefault();
                } else
                {
                    return new Hit();
                    //return null;
                }
            }
        }
        #endregion

        private String FormatDmg(Int64 value)
        {
            if (value >= 1000000000) { return (value / 1000000000D).ToString("0.00") + "G"; } else if (value >= 100000000) { return (value / 1000000D).ToString("0.0") + "M"; } else if (value >= 1000000) { return (value / 1000000D).ToString("0.00") + "M"; } else if (value >= 100000) { return (value / 1000).ToString("#,0") + "K"; } else if (value >= 10000) { return (value / 1000D).ToString("0.0") + "K"; }
            return value.ToString("#,0");
        }

        private String FormatNumber(double value)
        {
            if (value >= 100000000) { return (value / 1000000).ToString("#,0") + "M"; }
            if (value >= 1000000) { return (value / 1000000D).ToString("0.0") + "M"; }
            if (value >= 100000) { return (value / 1000).ToString("#,0") + "K"; }
            if (value >= 1000) { return (value / 1000D).ToString("0.0") + "K"; }
            return value.ToString("#,0");
        }

        public Brush Brush
        {
            get
            {
                if (Properties.Settings.Default.ShowDamageGraph && IsAlly)
                {
                    //
                    return GenerateBarBrush(Color.FromArgb(128, 0, 128, 128), Color.FromArgb(0, 30, 30, 30));
                } else
                {
                    if (IsYou && Properties.Settings.Default.HighlightYourDamage) { return new SolidColorBrush(Color.FromArgb(128, 0, 255, 255)); }

                    return new SolidColorBrush(Color.FromArgb(0, 30, 30, 30));
                }
            }
        }

        public Brush Brush2
        {
            get
            {
                if (Properties.Settings.Default.ShowDamageGraph && IsAlly && !IsZanverse)
                {
                    return GenerateBarBrush(Color.FromArgb(128, 0, 64, 64), Color.FromArgb(0, 0, 0, 0));
                } else
                {
                    if (IsYou && Properties.Settings.Default.HighlightYourDamage) { return new SolidColorBrush(Color.FromArgb(0, 0, 64, 64)); }
                    return new SolidColorBrush(new Color());
                }
            }
        }

        LinearGradientBrush GenerateBarBrush(Color c, Color c2)
        {
            if (!Properties.Settings.Default.ShowDamageGraph) { c = new Color(); } //グラフを表示しない場合に無効化
            if (IsYou && Properties.Settings.Default.HighlightYourDamage) { c = Color.FromArgb(128, 0, 255, 255); } //前面自分
            if (ID == "13470610") { c = Color.FromArgb(128, 255, 128, 0); }

            LinearGradientBrush lgb = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 0) };
            lgb.GradientStops.Add(new GradientStop(c, 0));
            lgb.GradientStops.Add(new GradientStop(c, ReadDamage / MainWindow.current.firstDamage));
            lgb.GradientStops.Add(new GradientStop(c2, ReadDamage / MainWindow.current.firstDamage));
            lgb.GradientStops.Add(new GradientStop(c2, 1));
            lgb.SpreadMethod = GradientSpreadMethod.Repeat;
            return lgb;
        }

        public Player(string id, string name, string temp)
        {
            ID = id;
            Name = name;
            isTemporary = temp;
            PercentDPS = -1;
            Attacks = new List<Hit>();
            PercentReadDPS = 0;
            Damaged = 0;

            if (temp == "raw")
            {
                AllyAttacks = new List<Hit>();
                DBAttacks = new List<Hit>();
                LswAttacks = new List<Hit>();
                PwpAttacks = new List<Hit>();
                AisAttacks = new List<Hit>();
                RideAttacks = new List<Hit>();
                AllyDamage = 0;
                DBDamage = 0;
                LswDamage = 0;
                PwpDamage = 0;
                AisDamage = 0;
                RideDamage = 0;
                ZvsDamage = 0;
            }
        }
    }

    public class Session
    {
        public int startTimestamp, newTimestamp, nowTimestamp, diffTime, ActiveTime;
        public List<int> instances = new List<int>();
        public Int64 totalDamage, totalAllyDamage, totalDBDamage, totalLswDamage, totalPwpDamage, totalAisDamage, totalRideDamage, totalZanverse, totalFinish;
        public double totalSD, firstDamage, totalDPS;
        public List<Player> players = new List<Player>();

        public Session()
        {
        }
    }


}
