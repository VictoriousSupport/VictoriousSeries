/// <summary>
/// [Jinx's Thresh]
///     [version 6.0.0.2]
///     1. Q Logic 변경
///        - OKTW Prediction 추가
///        - 선택 그랩 구문 추가
///        - 최소사거리 구문 추가 (Default: 450)
///     2. Skillshot 기본 수치 변경
///     3. Item 삭제
///     4. Safe Lantern 기능 보강 (1500범위내 체력 20이하 자동 렌턴)
///        - 내 주변 반경 500 내에 적챔피언이 없을때만 날아감.
///     5. Safe Lantern Key 기능 보강
///        - Q1 적중이면, 1500범위내 가장 멀리 있는 녀석에게
///        - 일반 상황에서는 1500범위내 가장 체력 낮은 녀석에게
///        - W 범위내에 있는 녀석에게는 OKTW 로직 적용
/// </summary>
/// 

using System;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using System.Collections.Generic;

namespace JinxsSupport.Plugins
{
    internal class Thresh : IPlugin
    {
        
        static Orbwalking.Orbwalker Orbwalker;
        public static Menu config;
        static Spell Q, W, E, R;
        static Obj_AI_Hero catchedUnit = null;
        static int qTimer;
        static readonly Obj_AI_Hero Player = ObjectManager.Player;

        //Mana
        static int QMana { get { return 80; } }
        static int WMana { get { return 50 * W.Level; } }
        static int EMana { get { return 60 * E.Level; } }
        static int RMana { get { return R.Level > 0 ? 100 : 0; } }

        #region Load() Function
        public void Load()
        {
            if (Player.ChampionName != "Thresh")
                return;

            Entry.PrintChat("<font color=\"#FFCC66\" >Thresh</font>");
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {

            LoadSpellData();

            config = new Menu("Victorious Thresh", "Jinx's Thresh", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.GreenYellow);

            //OrbWalk
            Orbwalker = new Orbwalking.Orbwalker(config.SubMenu("Orbwalking"));

            //Target selector
            var TargetSelectorMenu = new Menu("Target Selector", "Target Selector");
            {
                TargetSelector.AddToMenu(TargetSelectorMenu);

                config.AddSubMenu(TargetSelectorMenu);
            }

            var combomenu = new Menu("Combo", "Combo");
            {
                var Qmenu = new Menu("Q", "Q");
                {
                    Qmenu.AddItem(new MenuItem("C-UseQ", "Use Q", true).SetValue(true));
                    Qmenu.AddItem(new MenuItem("C-UseQ2", "Use Auto Q2", true).SetValue(false));
                    Qmenu.AddItem(new MenuItem("minGrab", "Min Range Grab", true).SetValue(new Slider(450, 125, (int)Q.Range)));            // by Jinx
                    Qmenu.AddItem(new MenuItem("Predict", "Set Predict", true).SetValue(new StringList(new[] { "Common", "OKTW" }, 1)));
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                        Qmenu.SubMenu("Q Enable").AddItem(new MenuItem("GrabSelect" + enemy.ChampionName, enemy.ChampionName).SetValue(true));    // by Jinx

                    combomenu.AddSubMenu(Qmenu);
                }
                var Wmenu = new Menu("W", "W");
                {
                    Wmenu.AddItem(new MenuItem("C-UseHW", "Use Hooeked W", true).SetValue(false));
                    Wmenu.AddItem(new MenuItem("Use-SafeLantern", "Use SafeLantern for our team", true).SetValue(true));
                    Wmenu.AddItem(new MenuItem("C-UseSW", "Use Shield W Min 3", true).SetValue(false));

                    combomenu.AddSubMenu(Wmenu);
                }
                var Emenu = new Menu("E", "E");
                {
                    Emenu.AddItem(new MenuItem("C-UseE", "Use E", true).SetValue(true));
                    combomenu.AddSubMenu(Emenu);
                }
                var Rmenu = new Menu("R", "R");
                {
                    Rmenu.AddItem(new MenuItem("C-UseR", "Use Auto R", true).SetValue(true));
                    Rmenu.AddItem(new MenuItem("minNoEnemies", "Min No. Of Enemies R", true).SetValue(new Slider(3, 1, 5)));
                    combomenu.AddSubMenu(Rmenu);
                }
                combomenu.AddItem(new MenuItem("ComboActive", "Combo", true).SetValue(new KeyBind(32, KeyBindType.Press)));
                combomenu.AddItem(new MenuItem("FlayPush", "Flay Push Key", true).SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Press)));
                combomenu.AddItem(new MenuItem("FlayPull", "Flay Pull Key", true).SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));
                combomenu.AddItem(new MenuItem("SafeLanternKey", "Safe Lantern Key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                config.AddSubMenu(combomenu);
            }

            var harassmenu = new Menu("Harass", "Harass");
            {
                var Qmenu = new Menu("Q", "Q");
                {
                    Qmenu.AddItem(new MenuItem("H-UseQ", "Use Q", true).SetValue(false));
                    harassmenu.AddSubMenu(Qmenu);
                }
                var Emenu = new Menu("E", "E");
                {
                    Emenu.AddItem(new MenuItem("H-UseE", "Use E", true).SetValue(true));
                    harassmenu.AddSubMenu(Emenu);
                }
                harassmenu.AddItem(new MenuItem("HarassActive", "Harass", true).SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Press)));
                harassmenu.AddItem(new MenuItem("Mana", "ManaManager", true).SetValue(new Slider(30, 0, 100)));
                config.AddSubMenu(harassmenu);
            }

            var KSmenu = new Menu("KS", "KS");
            {
                KSmenu.AddItem(new MenuItem("KS-UseQ", "Use Q KS", true).SetValue(false));
                KSmenu.AddItem(new MenuItem("KS-UseE", "Use E KS", true).SetValue(true));
                KSmenu.AddItem(new MenuItem("KS-UseR", "Use R KS", true).SetValue(false));

                config.AddSubMenu(KSmenu);
            }

            var Miscmenu = new Menu("Misc", "Misc");
            {
                Miscmenu.AddItem(new MenuItem("UseEGapCloser", "Use E On Gap Closer", true).SetValue(true));
                Miscmenu.AddItem(new MenuItem("UseQInterrupt", "Use Q On Interrupt", true).SetValue(false));
                Miscmenu.AddItem(new MenuItem("UseEInterrupt", "Use E On Interrupt", true).SetValue(true));
                Miscmenu.AddItem(new MenuItem("DebugMode", "Debug Mode", true).SetValue(false));

                var EscapeMenu = new Menu("Block Enemy Escape Skills", "Block Enemy Escape Skills");
                {
                    EscapeMenu.AddItem(new MenuItem("BlockEscapeE", "Use E When Enemy have to Use Escape Skills", true).SetValue(true));
                    //EscapeMenu.AddItem(new MenuItem("BlockEscapeQ", "Use Q When Enemy have to Use Escape Skills", true).SetValue(true));
                    //EscapeMenu.AddItem(new MenuItem("BlockEscapeFlash", "Use Q When Enemy have to Use Flash", true).SetValue(true));

                    Miscmenu.AddSubMenu(EscapeMenu);
                }

                config.AddSubMenu(Miscmenu);
            }

            var Drawingmenu = new Menu("Drawings", "Drawings");
            {
                Drawingmenu.AddItem(new MenuItem("DrawTarget", "Draw Target", true).SetValue(true));
                Drawingmenu.AddItem(new MenuItem("Qcircle", "Q Range", true).SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
                Drawingmenu.AddItem(new MenuItem("Wcircle", "W Range", true).SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
                Drawingmenu.AddItem(new MenuItem("Ecircle", "E Range", true).SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
                Drawingmenu.AddItem(new MenuItem("Rcircle", "R Range", true).SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Drawingmenu.AddItem(new MenuItem("Lcircle", "L Range", true).SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));

                config.AddSubMenu(Drawingmenu);
            }
            config.AddItem(new MenuItem("PermaShow", "PermaShow", true).SetShared().SetValue(true)).ValueChanged += (s, args) => {
                if (args.GetNewValue<bool>())
                {
                    config.Item("ComboActive", true).Permashow(true, "Combo", SharpDX.Color.Aqua);
                    config.Item("HarassActive", true).Permashow(true, "Harass", SharpDX.Color.Aqua);
                    config.Item("FlayPush", true).Permashow(true, "E Push", SharpDX.Color.AntiqueWhite);
                    config.Item("FlayPull", true).Permashow(true, "E Pull", SharpDX.Color.AntiqueWhite);
                    config.Item("SafeLanternKey", true).Permashow(true, "Safe Lantern", SharpDX.Color.Aquamarine);
                }
                else
                {
                    config.Item("ComboActive", true).Permashow(false, "Combo");
                    config.Item("HarassActive", true).Permashow(false, "Harass");
                    config.Item("FlayPush", true).Permashow(false, "E Push");
                    config.Item("FlayPull", true).Permashow(false, "E Pull");
                    config.Item("SafeLanternKey", true).Permashow(false, "Safe Lantern");
                }
            };
            config.Item("ComboActive", true).Permashow(config.IsBool("PermaShow"), "Combo", SharpDX.Color.Aqua);
            config.Item("HarassActive", true).Permashow(config.IsBool("PermaShow"), "Harass", SharpDX.Color.Aqua);
            config.Item("FlayPush", true).Permashow(config.IsBool("PermaShow"), "E Push", SharpDX.Color.AntiqueWhite);
            config.Item("FlayPull", true).Permashow(config.IsBool("PermaShow"), "E Pull", SharpDX.Color.AntiqueWhite);
            config.Item("SafeLanternKey", true).Permashow(config.IsBool("PermaShow"), "Safe Lantern", SharpDX.Color.Aquamarine);

            config.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Base_OnPlayAnimation;
            EscapeBlocker.OnDetectEscape += EscapeBlocker_OnDetectEscape;

        }
        #endregion

        #region Logic Combo

        static void LoadSpellData()
        {
            Q = new Spell(SpellSlot.Q, 1075);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 450);
            R = new Spell(SpellSlot.R, 420);

            Q.SetSkillshot(0.5f, 65f, 1900f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.2f, 10, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 50, float.MaxValue, false, SkillshotType.SkillshotLine);
        }

        static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var Etarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            if (config.IsActive("ComboActive"))
            {
                if (target != null)
                {
                    if (CastQ2())
                    {
                        CastCatchedLatern();
                    }
                    if (config.IsBool("C-UseQ") && Q.IsReady())
                    {
                        CastQ(target);
                    }
                    if (config.IsBool("C-UseSW") && W.IsReady())    // Use Shield W Min 3
                    {
                        ShieldLantern();
                    }
                    KSCheck(target);
                }

                if (Etarget != null)
                {
                    if (config.IsBool("C-UseE") && E.IsReady())
                    {
                        CastE(Etarget);
                    }
                }
            }

            if (config.IsActive("FlayPush") && Etarget != null && 
                E.IsReady())
            {
                Push(Etarget);
            }

            if (config.IsActive("FlayPull") && Etarget != null &&
                E.IsReady())
            {
                Pull(Etarget);
            }

            if (config.IsActive("SafeLanternKey"))
            {
                SafeLanternKeybind();
            }

            if (config.IsBool("Use-SafeLantern"))
            {
                SafeLantern();
            }
        }

        static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var Etarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var mana = config.Item("Mana", true).GetValue<Slider>().Value;

            if (Player.ManaPercents() < mana)
                return;

            if (config.IsActive("HarassActive"))
            {
                if (config.IsBool("H-UseE") && E.IsReady() && Etarget != null)
                {
                    CastE(Etarget);
                }
                if (config.IsBool("H-UseQ") && Q.IsReady() && target != null)
                {
                    CastQ(target);
                }
            }
        }

        static void KSCheck(Obj_AI_Hero target)
        {
            if (target != null)
            {
                if (config.Item("KS-UseQ", true).GetValue<bool>())
                {
                    var myDmg = Player.GetSpellDamage(target, SpellSlot.Q);
                    if (myDmg >= target.Health)
                    {
                        CastQ(target);
                    }
                }
                if (config.Item("KS-UseE", true).GetValue<bool>())
                {
                    var myDmg = Player.GetSpellDamage(target, SpellSlot.E);
                    if (myDmg >= target.Health)
                    {
                        CastE(target);
                    }
                }
                if (config.Item("KS-UseR", true).GetValue<bool>())
                {
                    var myDmg = Player.GetSpellDamage(target, SpellSlot.R);
                    if (myDmg >= target.Health)
                    {
                        if (Player.Distance(target.Position) <= R.Range)
                        {
                            R.Cast();
                        }
                    }
                }
            }
        }

        #endregion

        #region Logic Q

        public static bool CastOKTW(Obj_AI_Hero target, OKTWPrediction.HitChance hitChance)
        {
            var spell = Q;
            var OKTWPlayer = Player;

            OKTWPrediction.SkillshotType CoreType2 = OKTWPrediction.SkillshotType.SkillshotLine;
            bool aoe2 = false;

            var predInput2 = new OKTWPrediction.PredictionInput
            {
                Aoe = aoe2,
                Collision = spell.Collision,
                Speed = spell.Speed,
                Delay = spell.Delay,
                Range = spell.Range,
                From = OKTWPlayer.ServerPosition,
                Radius = spell.Width,
                Unit = target,
                Type = CoreType2
            };
            var poutput2 = OKTWPrediction.Prediction.GetPrediction(predInput2);

            if (spell.Speed != float.MaxValue && OKTWPrediction.CollisionYasuo(OKTWPlayer.ServerPosition, poutput2.CastPosition))
                return false;

            if (poutput2.Hitchance >= hitChance)
            {
                return spell.Cast(poutput2.CastPosition);
            }
            return false;
        }

        static void CastQ(Obj_AI_Hero target)
        {
            if (!Q.IsReady() || target == null || Helper.EnemyHasShield(target) || !target.IsValidTarget())
                return;

            var Catched = IsPulling().Item1;
            var CatchedQtarget = IsPulling().Item2;

            if (!Catched && qTimer == 0)
            {

                if (!E.IsReady() || (E.IsReady() &&
                    E.Range < Player.Distance(target.Position)))
                {
                    var Mode = config.Item("Predict", true).GetValue<StringList>().SelectedIndex;
                    float minGrab = config.Item("minGrab", true).GetValue<Slider>().Value;  // by Jinx, 최소 사거리 

                    switch (Mode)
                    {
                        #region L# Predict
                        case 0:
                            {
                                var b = Q.GetPrediction(target);

                                if (b.Hitchance >= HitChance.High &&
                                    Player.Distance(target.ServerPosition) < Q.Range &&
                                    Player.Distance(target.ServerPosition) > minGrab)       // by Jinx
                                {

                                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                                    {
                                        if (enemy.Team != Player.Team && target != null
                                            && config.Item("GrabSelect" + enemy.ChampionName).GetValue<bool>() && Q.IsReady()
                                            && target.ChampionName == enemy.ChampionName && Player.Spellbook.GetSpell(SpellSlot.Q).Name == "ThreshQ")
                                        {
                                            Q.Cast(target);
                                        }
                                    }

                                        
                                }
                            }
                            break;
                        #endregion

                        #region OKTW Predict2
                        case 1:
                            {
                                if (Player.Distance(target.ServerPosition) < Q.Range &&
                                    Player.Distance(target.ServerPosition) > minGrab)       // by Jinx
                                {
                                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                                    {
                                        if (enemy.Team != Player.Team && target != null
                                            && config.Item("GrabSelect" + enemy.ChampionName).GetValue<bool>() && Q.IsReady()
                                            && target.ChampionName == enemy.ChampionName && Player.Spellbook.GetSpell(SpellSlot.Q).Name == "ThreshQ")
                                        {
                                            CastOKTW(target, OKTWPrediction.HitChance.VeryHigh);
                                        }
                                    }

                                    
                                }
                            }
                            break;
                            #endregion
                    }
                }
            }
            else if (Catched && Environment.TickCount > qTimer - 200 && CastQ2() && CatchedQtarget.Type == GameObjectType.obj_AI_Hero && config.IsBool("C-UseQ2"))
            {
                Q.Cast();
            }
        }

        static bool CastQ2()                        // Q2로 날아갈 것인지 아닌지 판단하기
        {
            var status = false;
            var Catched = IsPulling().Item1;
            var CatchedQtarget = IsPulling().Item2;

            if (Catched && CatchedQtarget != null && 
                CatchedQtarget.Type == GameObjectType.obj_AI_Hero && 
                !Turret.IsUnderEnemyTurret(CatchedQtarget))
            {
                var EnemiesCount = Helper.GetEnemiesNearTarget(CatchedQtarget).Count();
                var AlliesCount = GetAlliesNearTarget(CatchedQtarget).Item1;
                var CanKill = GetAlliesNearTarget(CatchedQtarget).Item2;

                if (CanKill)
                {
                    EnemiesCount = EnemiesCount - 1;
                }

                if (EnemiesCount == 0)
                {
                    status = true;
                }
                else if (AlliesCount >= EnemiesCount)
                {
                    status = true;
                }
                else if (E.IsReady() && Turret.IsUnderAllyTurret(CatchedQtarget))
                {
                    status = true;
                }
            }
            return status;
        }

        static Tuple<int, bool> GetAlliesNearTarget(Obj_AI_Hero target)
        {
            var Count = 0;
            var status = false;
            double dmg = 0;
            double allyDmg = 0;

            foreach (Obj_AI_Hero allyhero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly && !x.IsDead))
            {
                if (allyhero.Distance(target.Position) <= 900)
                {
                    Count += 1;

                    dmg = dmg + Player.GetAutoAttackDamage(target, true);
                    dmg = dmg + E.GetDamage(target);
                    dmg = R.IsReady() ? dmg + R.GetDamage(target) : dmg;

                    if (allyhero.ChampionName != Player.ChampionName)
                    {
                        allyDmg = allyDmg + Helper.GetAlliesComboDmg(target, allyhero);
                    }

                }
            }

            if (E.IsReady())
            {
                dmg = dmg * 2;
                allyDmg = allyDmg * 1.5;
            }

            var totalDmg = dmg + allyDmg;

            if (totalDmg > target.Health)
            {
                status = true;
            }

            return new Tuple<int, bool>(Count, status);
        }

        #endregion

        #region Logic W

        // 렌턴에 OKTW 알고리즘 적용
        public static bool CastWOKTW(Obj_AI_Hero target, OKTWPrediction.HitChance hitChance)
        {

            if (!W.IsReady())
                return false;

            var spell = W;
            var OKTWPlayer = Player;

            OKTWPrediction.SkillshotType CoreType2 = OKTWPrediction.SkillshotType.SkillshotLine;
            bool aoe2 = false;

            var predInput2 = new OKTWPrediction.PredictionInput
            {
                Aoe = aoe2,
                Collision = spell.Collision,
                Speed = spell.Speed,
                Delay = spell.Delay,
                Range = spell.Range,
                From = OKTWPlayer.ServerPosition,
                Radius = spell.Width,
                Unit = target,
                Type = CoreType2
            };
            var poutput2 = OKTWPrediction.Prediction.GetPrediction(predInput2);

            if (spell.Speed != float.MaxValue && OKTWPrediction.CollisionYasuo(OKTWPlayer.ServerPosition, poutput2.CastPosition))
                return false;

            if (poutput2.Hitchance >= hitChance)
            {
                return spell.Cast(poutput2.CastPosition);
            }
            return false;
        }

        static void CastW(Vector3 Position)
        {
            if (!W.IsReady() || Player.Distance(Position) > W.Range)
                return;

            W.Cast(Position);
        }

        // 실제로 사용되는 경우는 없음. Q1 적중후 자동으로 날아가는 구문
        static void CastCatchedLatern()
        {
            if (!W.IsReady() || !config.Item("C-UseHW", true).GetValue<bool>())
                return;

            bool Catched = IsPulling().Item1;
            Obj_AI_Hero CatchedQtarget = IsPulling().Item2;

            if (Catched && CatchedQtarget != null && CatchedQtarget.Type == GameObjectType.obj_AI_Hero)
            {
                var Wtarget = GetFurthestAlly(CatchedQtarget);  // 1500 범위 내에 나와 가장 멀리 떨어져 있는 녀석을 데려온다.
                if (Wtarget != null)
                {
                    if (Player.Distance(Wtarget.Position) <= W.Range)
                    {
                        CastW(Wtarget.Position);
                    }
                    else if (Player.Distance(Wtarget.Position) > W.Range)
                    {
                        W.Cast(Player.Position.Extend(Wtarget.Position, W.Range));      // by Jinx
                    }
                }
            }
        }

        // 가장 멀리 있는 아군을 찾는 로직 (1500 범위 내)
        static Obj_AI_Hero GetFurthestAlly(Obj_AI_Hero target)
        {
            Obj_AI_Hero Wtarget = null;
            float distance = 0;

            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly && !x.IsMe && !x.IsDead))
            {
                if (Player.Distance(hero.Position) <= 1500 &&
                    hero.Distance(target.Position) > Player.Distance(target.Position))
                {
                    var temp = Player.Distance(hero.Position);

                    if (distance == 0 && Wtarget == null)
                    {
                        Wtarget = hero;
                        distance = Player.Distance(hero.Position);
                    }
                    else if (temp > distance)
                    {
                        Wtarget = hero;
                        distance = Player.Distance(hero.Position);
                    }
                }
            }
            return Wtarget;
        }

        static void ShieldLantern()
        {
            int count = 0;
            Obj_AI_Hero target = null;
            foreach (var allyhero in ObjectManager.Get<Obj_AI_Hero>().Where
                (x => x.IsAlly &&
                    Player.Distance(x.Position) < W.Range &&
                    !x.IsDead && !x.HasBuff("Recall")))
            {
                var tmp = Utility.CountAlliesInRange(allyhero, 200);

                if (count == 0)
                {
                    count = tmp;
                }
                else if (tmp > count)
                {
                    count = tmp;
                }
            }

            if (count > 2 && target != null)
            {
                CastW(target.Position);
            }
        }

        // 자동 렌턴 발동
        static void SafeLantern()
        {
            if (!ManaManager())
                return;

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => x.IsAlly && !x.IsDead && !x.IsMe &&
                Player.Distance(x.Position) < 1500 && 
                !x.HasBuff("Recall")))
            {
                if ((hero.HpPercents() < 20) && CheckSafeZone())    // 아군 HP 20 이하고, 내 주변에 적이 없을때
                {
                    if (Player.Distance(hero.Position) <= W.Range)
                    {
                        var Pos = W.GetPrediction(hero).CastPosition;
                        CastW(Pos);
                    }
                    else
                    {
                        var Pos = Player.Position + (hero.Position - Player.Position).Normalized() * W.Range;
                        CastW(Pos);
                    }
                }
                else if ((hero.HasBuffOfType(BuffType.Suppression) ||
                    hero.HasBuffOfType(BuffType.Taunt) ||
                    hero.HasBuffOfType(BuffType.Knockup) ||
                    hero.HasBuffOfType(BuffType.Flee))) 
                {
                    if ((hero.HpPercents() < 20) && CheckSafeZone())    // 아군 HP 20 이하고, 내 주변에 적이 없을때
                    {
                        if (Player.Distance(hero.Position) <= W.Range)
                            CastW(hero.Position);
                        else
                        {
                            var Pos = Player.Position + (hero.Position - Player.Position).Normalized() * W.Range;
                            CastW(Pos);
                        }
                    }
                }
            }
        }

        static bool CheckSafeZone()             //by Jinx
        {
            // 내 주변 반경 500 안에 적군이 없으면...
            var hit = HeroManager.Enemies.Where(i => i.IsValidTarget(500)).ToList();

            if (hit.Count < 1)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        static void SafeLanternKeybind()
        {

            bool Catched = IsPulling().Item1;
            Obj_AI_Hero CatchedQtarget = IsPulling().Item2;
            Obj_AI_Hero Wtarget = null;
            float Hp = 0;

            // 만약에 Q1 적중상태에서 T키를 누르면, 1500 범위내 가장 멀리 있는 녀석한테 렌턴이 날아간다.
            if (Catched && CatchedQtarget != null && CatchedQtarget.Type == GameObjectType.obj_AI_Hero)
            {
                Wtarget = GetFurthestAlly(CatchedQtarget);  // 1500 범위 내에 나와 가장 멀리 떨어져 있는 녀석을 데려온다.
                if (Wtarget != null)
                {
                    if (Player.Distance(Wtarget.Position) <= W.Range)
                    {
                        /*
                        var Pos = W.GetPrediction(Wtarget).CastPosition;
                        CastW(Pos);
                        */
                        CastWOKTW(Wtarget, OKTWPrediction.HitChance.High);
                    }
                    else if (Player.Distance(Wtarget.Position) > W.Range)
                    {
                        var Pos = Player.Position + (Wtarget.Position - Player.Position).Normalized() * W.Range;
                        CastW(Pos);
                    }
                }
            }
            else // 만약 Q1이 맞지 않은 상태에서 T키를 누르면, 1500 범위내 가장 체력 낮은 녀석에게 준다.
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>()
                    .Where(x => x.IsAlly && !x.IsDead && !x.IsMe &&
                    Player.Distance(x.Position) < 1500 &&
                    !x.HasBuff("Recall")))
                {
                    var temp = hero.HpPercents();

                    if (hero.HasBuffOfType(BuffType.Suppression) ||
                        hero.HasBuffOfType(BuffType.Taunt) ||
                        hero.HasBuffOfType(BuffType.Knockup) ||
                        hero.HasBuffOfType(BuffType.Flee))
                    {
                        if (Player.Distance(hero.Position) <= W.Range)
                        {
                            var Pos = W.GetPrediction(hero).CastPosition;
                            CastW(Pos);
                        }

                        else
                        {
                            var Pos = Player.Position + (Wtarget.Position - Player.Position).Normalized() * W.Range;
                            CastW(Pos);
                        }
                            
                    }

                    if (Wtarget == null && Hp == 0)
                    {
                        Wtarget = hero;
                        Hp = temp;
                    }
                    else if (temp < Hp)
                    {
                        Wtarget = hero;
                        Hp = temp;
                    }
                }

                // 가장 체력 낮은 녀석한테 주는 것이 기본이나, 한놈만 있으면 그놈한테 준다.
                if (Wtarget != null)
                {
                        if (Player.Distance(Wtarget.Position) <= W.Range)
                        {
                            var Pos = W.GetPrediction(Wtarget).CastPosition;
                            CastW(Pos);
                        }
                        else
                        {
                            var Pos = Player.Position + (Wtarget.Position - Player.Position).Normalized() * W.Range;
                            CastW(Pos);
                        }
                      
                }
            }



        }

        #endregion

        #region Logic E

        static void CastE(Obj_AI_Hero target)
        {
            if (!E.IsReady() || target == null || !target.IsValidTarget())
                return;

            bool Catched = IsPulling().Item1;
            Obj_AI_Hero CatchedQtarget = IsPulling().Item2;

            if (!Catched && qTimer == 0)
            {
                if (Player.Distance(target.Position) <= E.Range)
                {
                    if (Player.HpPercents() < 40 && 
                        target.HpPercents() > 20)
                    {
                        Push(target);
                    }
                    else
                    {
                        Pull(target);
                    }
                }
            }
            else if (Catched && CatchedQtarget != null)
            {
                if (Environment.TickCount > qTimer - 200 && Player.Distance(CatchedQtarget.Position) <= E.Range)
                {
                    Pull(CatchedQtarget);
                }
            }
        }

        static void Pull(Obj_AI_Base target)
        {
            var pos = target.Position.Extend(Player.Position, Player.Distance(target.Position) + 200);
            E.Cast(pos);
        }

        static void Push(Obj_AI_Base target)
        {
            var pos = target.Position.Extend(Player.Position, Player.Distance(target.Position) - 200);
            E.Cast(pos);
        }

        #endregion

        #region Logic R

        static void AutoR()
        {
            if (!R.IsReady() && config.Item("C-UseR", true).GetValue<bool>())
                return;

            // Menu Count
            int RequireCount = config.Item("minNoEnemies", true).GetValue<Slider>().Value;

            // Enemeis count in R range
            var hit = HeroManager.Enemies.Where(i => i.IsValidTarget(R.Range)).ToList();

            if (RequireCount <= hit.Count && R.IsReady())
            {
                R.Cast();
            }
            else
            {
                // 타워밑에서 기술 사용 (임시코드! 안되면 삭제할 것)
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && Player.UnderAllyTurret())
                {
                    if (Player.Distance(t.ServerPosition) > Player.Distance(t.Position))
                        R.Cast();
                }
            }
        }

        #endregion

        #region Others

        static Tuple<bool, Obj_AI_Hero> IsPulling()
        {
            bool Catched;
            Obj_AI_Hero CatchedQtarget;

            if (catchedUnit != null)
            {
                Catched = true;
                CatchedQtarget = catchedUnit;
            }
            else
            {
                Catched = false;
                CatchedQtarget = null;
            }

            return new Tuple<bool, Obj_AI_Hero>(Catched, CatchedQtarget);
        }

        static void CheckBuff()
        {
            if (Player.IsDead)
                return;

            foreach (Obj_AI_Hero enemyhero in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && !enemy.IsDead && enemy.IsValid))
            {
                // ThreshQ를 맞은 상태의 챔프 구별
                if (enemyhero.HasBuff("ThreshQ") || enemyhero.HasBuff("threshqfakeknockup"))
                {
                    catchedUnit = enemyhero;
                    return;
                }
            }

            if (catchedUnit != null)
            {
                if (!catchedUnit.HasBuff("ThreshQ"))
                {
                    catchedUnit = null;
                }
            }
        }

        static bool ManaManager()
        {
            var status = false;
            var ReqMana = R.IsReady() ? QMana + EMana + RMana : QMana + EMana; 

            if (ReqMana < Player.Mana)
            {
                status = true;
            }
            else if (Player.MaxHealth * 0.3 > Player.Health)
            {
                status = true;
            }

            return status;
        }

        static bool Debug()
        {
            return config.IsBool("DebugMode");
        }

        public static void Debug(string s)
        {
            if (Debug())
            {
                Console.WriteLine("" + s);
            }
        }

        static void Debug(Vector3 pos)
        {
            if (!Debug())
                return;

            Drawing.OnDraw += delegate(EventArgs args)
            {
                Render.Circle.DrawCircle(pos, 150, System.Drawing.Color.Yellow);
            };
        }

        #endregion 

        #region Events

        static void Game_OnUpdate(EventArgs args)
        {
            CheckBuff();

            Combo();
            Harass();

            AutoR();
        }

        static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!config.Item("UseEInterrupt", true).GetValue<bool>())
                return;

            if (Player.Distance(sender.Position) < E.Range && sender.IsEnemy)
            {
                if (E.IsReady())
                {
                    Game.PrintChat("<font color='#3492EB'>Jinx's Thresh:</font> <font color='#FFFFFF'>EInterrupt</font>");
                    Pull(sender);
                }
            }

            if (Player.Distance(sender.ServerPosition) < Q.Range && 
                (!E.IsReady() || (E.IsReady() && E.Range < Player.Distance(sender.Position))) && 
                sender.IsEnemy && args.DangerLevel == Interrupter2.DangerLevel.High && 
                args.EndTime > Utils.TickCount + Q.Delay + (Player.Distance(sender.Position) / Q.Speed))
            {
                if (Q.IsReady())
                {
                    CastQ(sender);
                }
            }
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!config.Item("UseEGapCloser", true).GetValue<bool>())
                return;

            if (Player.Distance(gapcloser.Sender.Position) < E.Range && gapcloser.Sender.IsEnemy)
            {
                if (E.IsReady())
                {
                    Push(gapcloser.Sender);
                    //Game.PrintChat("<font color='#3492EB'>Jinx's Thresh:</font> <font color='#FFFFFF'>AntiGapClose=Type_Me</font>");
                }
            }

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly && !x.IsMe && Player.Distance(x.Position) < E.Range))
            {
                if (gapcloser.End.Distance(hero.Position) < 100 &&
                    E.IsReady())
                {
                    Push(gapcloser.Sender);
                    Game.PrintChat("<font color='#3492EB'>Jinx's Thresh:</font> <font color='#FFFFFF'>AntiGapClose=Type_Ally</font>");
                }
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            var QCircle = config.Item("Qcircle", true).GetValue<Circle>();
            var WCircle = config.Item("Wcircle", true).GetValue<Circle>();
            var ECircle = config.Item("Ecircle", true).GetValue<Circle>();
            var RCircle = config.Item("Rcircle", true).GetValue<Circle>();
            var LCircle = config.Item("Lcircle", true).GetValue<Circle>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            
            if (QCircle.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, QCircle.Color);
            }

            if (WCircle.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, WCircle.Color);
            }

            if (ECircle.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, ECircle.Color);
            }

            if (RCircle.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, RCircle.Color);
            }

            if (config.Item("DrawTarget", true).GetValue<bool>() && target != null)
            {
                Render.Circle.DrawCircle(target.Position, 150, System.Drawing.Color.Red);
            }

            if (LCircle.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, 1500f, LCircle.Color, 1);
            }

        }

        static void Obj_AI_Base_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Animation.ToLower() == "spell1_in")
                {
                    qTimer = Environment.TickCount + 1200;
                }
                else if (args.Animation.ToLower() == "spell1_out")
                {
                    qTimer = 0;
                }
                else if (args.Animation.ToLower() == "spell1_pull1")
                {
                    qTimer = Environment.TickCount + 900;
                }
                else if (args.Animation.ToLower() == "spell1_pull2")
                {
                    qTimer = Environment.TickCount + 900;
                }
                else if (qTimer > 0 && Environment.TickCount > qTimer)
                {
                    qTimer = 0;
                }
            }


            if (!(sender is Obj_AI_Hero))
                return;

            var _sender = sender as Obj_AI_Hero;
            var dis = _sender.GetBuffCount("rengartrophyicon1") > 5 ? 600 : 750;
            
            if (_sender.ChampionName == "Rengar" && args.Animation == "Spell5" &&
                Player.Distance(_sender.Position) < dis && E.IsReady())
            {
                Push(_sender);
            }
        }
        
        static void EscapeBlocker_OnDetectEscape(Obj_AI_Hero sender, GameObjectEscapeDetectorEventArgs args)
        {
            if (!sender.IsEnemy)
                return;

            #region BLockFlashEscape
            /*
            if (Menubool("BlockEscapeFlash") && sender.IsEnemy &&
                args.SpellData == "summonerflash")
            {
                if (Player.Distance(args.End) < Q.Range && Q.IsReady() &&
                    Player.Distance(args.End) > E.Range)
                {
                    Debug(args.End);
                    Debug("flash");

                    var predict = Q.GetPrediction(sender);

                    if (predict.Hitchance != HitChance.Collision)
                    {
                        Debug("EscapeFlash");
                        Q.Cast(args.End);
                    }
                }
            }
            */
            #endregion

            #region BLockSpellsEscape

            if (args.SpellData == "summonerflash")
                return;

            if (Player.Distance(args.Start) < E.Range && E.IsReady() &&
                    Player.Distance(args.End) > E.Range &&
                    config.IsBool("BlockEscapeE"))
            {
                Debug(args.End);
                Debug("EscapeE");
                Pull(sender);
            }
                /*
            else if ((!E.IsReady() || Player.Distance(args.Start) > E.Range) &&
                Player.Distance(args.End) < Q.Range && Q.IsReady() &&
                Player.Distance(args.End) > E.Range &&
                Menubool("BlockEscapeQ"))
            {
                var predict = Q.GetPrediction(sender);

                if (predict.Hitchance != HitChance.Collision)
                {
                    Debug(args.End);
                    Debug("EscapeQ");
                    Q.Cast(args.End);
                }
            }
            */
            #endregion
        }
        
        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Debug() && sender.IsEnemy && sender is Obj_AI_Hero)
            {
                var _sender = sender as Obj_AI_Hero;
                Console.WriteLine(": " + args.SData.Name + " - " + _sender.ChampionName + _sender.GetSpellSlot(args.SData.Name));
            }
        }

        #endregion
    }

    internal class GameObjectEscapeDetectorEventArgs
    {
        public Obj_AI_Hero Sender;
        public SpellSlot Slot;
        public Vector3 Start;
        public Vector3 End;
        public int StartTickCount;
        public string SpellData;
    }

    internal delegate void EscapeDetector(Obj_AI_Hero sender, GameObjectEscapeDetectorEventArgs args);

    internal class EscapeBlocker
    {
        public static event EscapeDetector OnDetectEscape;

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static List<string> EscapeSpells = new List<string>();
        private static List<GameObjectEscapeDetectorEventArgs> ActiveEscapeSpells = new List<GameObjectEscapeDetectorEventArgs>();

        static EscapeBlocker()
        {
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnUpdate += Game_OnUpdate;

            InitializeSpells();
        }

        private static void InitializeSpells()
        {
            #region Spells

            //EscapeSpells.Add("summonerflash"); // ALL Champions Flash
            EscapeSpells.Add("LucianE"); // Lucian E
            EscapeSpells.Add("LeesinW"); // Leesin W
            EscapeSpells.Add("BlindMonkQTwo"); // Leesin Q2
            EscapeSpells.Add("KhazixE"); // Khazix E khazixelong
            EscapeSpells.Add("KhazixELong"); // Khazix E Long
            EscapeSpells.Add("Pounce"); // Nidalee W
            EscapeSpells.Add("TristanaW"); // Tristana W
            EscapeSpells.Add("SejuaniArcticAssault"); // Sejuani Q
            EscapeSpells.Add("ShenShadowDash"); // Shen E
            EscapeSpells.Add("AatroxQ"); // Aatorx Q
            EscapeSpells.Add("RenektonSliceAndDice"); // Renekton E
            EscapeSpells.Add("GravesMove"); // Graves E
            EscapeSpells.Add("JarvanIVDragonStrike"); // Jarvan EQ
            EscapeSpells.Add("GragasE"); // Gragas E
            EscapeSpells.Add("GnarE"); // Gnar E
            EscapeSpells.Add("ViQ"); // Vi Q
            //EscapeSpells.Add("EzrealE"); // Ezreal E
            EscapeSpells.Add("RivenE"); // Riven E
            //EscapeSpells.Add("KatarinaE"); // Katarina E
            EscapeSpells.Add("CorkiW"); // Corki W

            EscapeSpells.Add("VayneQ"); // Vayne Q
            EscapeSpells.Add("ShacoQ"); // shaco Q
            EscapeSpells.Add("CayitlynE"); // Cayitlyn E
            EscapeSpells.Add("ZacE"); // Zac E
            EscapeSpells.Add("ShyvanaR"); // Shyvana R

            // Leblanc W
            // Ahri R 
            // Jax Q
            // Ziqs W
            // Kassadin R
            // Queen
            // Hecarim R

            #endregion
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            ActiveEscapeSpells.RemoveAll(x => Environment.TickCount > x.StartTickCount + 900);

            if (OnDetectEscape == null)
                return;

            foreach (var a in ActiveEscapeSpells)
            {
                OnDetectEscape(a.Sender, a);
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!(sender is Obj_AI_Hero))
                return;

            var _sender = sender as Obj_AI_Hero;

            if (!CheckSpells(_sender, args))
                return;

            ActiveEscapeSpells.Add(new GameObjectEscapeDetectorEventArgs
            {
                Sender = _sender,
                Slot = _sender.GetSpellSlot(args.SData.Name),
                Start = args.Start,
                End = args.End,
                StartTickCount = Environment.TickCount,
                SpellData = args.SData.Name
            });
        }

        private static bool CheckSpells(Obj_AI_Hero sender, GameObjectProcessSpellCastEventArgs args)
        {
            return EscapeSpells.Contains(args.SData.Name) || EscapeSpells.Contains(sender.ChampionName + sender.GetSpellSlot(args.SData.Name));
        }
    }

    internal static class Extensions
    {
        #region Obj_AI_Hero

        public static float HpPercents(this Obj_AI_Hero hero)
        {
            return hero.Health / hero.MaxHealth * 100;
        }

        public static float ManaPercents(this Obj_AI_Hero hero)
        {
            return hero.Mana / hero.MaxMana * 100;
        }

        #endregion

        #region Menu

        public static bool IsBool(this Menu Menu, string item)
        {
            return Menu.Item(item, true).GetValue<bool>();
        }

        public static bool IsActive(this Menu Menu, string item)
        {
            return Menu.Item(item, true).GetValue<KeyBind>().Active;
        }

        public static int GetValue(this Menu Menu, string item)
        {
            return Menu.Item(item, true).GetValue<Slider>().Value;
        }

        #endregion

    }

    internal class Helper
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public static Obj_AI_Hero GetMostAD(bool IsAllyTeam, float range)
        {
            Obj_AI_Hero MostAD = null;

            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>()
                .Where(x => (IsAllyTeam ? x.IsAlly : x.IsEnemy && x.IsValidTarget()) &&
                    !x.IsMe && !x.IsDead))
            {
                if (Player.Distance(hero.Position) < range)
                {
                    if (MostAD == null)
                    {
                        MostAD = hero;
                    }
                    else if (MostAD != null && MostAD.TotalAttackDamage < hero.TotalAttackDamage)
                    {
                        MostAD = hero;
                    }
                }
            }

            return MostAD;
        }

        public static IEnumerable<Obj_AI_Hero> GetEnemiesNearTarget(Obj_AI_Hero target)
        {
            return HeroManager.Enemies.Where(x => target.Distance(x.Position) < 1500 && !x.IsDead);
        }

        public static bool EnemyHasShield(Obj_AI_Hero target)
        {
            var status = false;

            if (target.HasBuff("blackshield"))
            {
                status = true;
            }

            if (target.HasBuff("sivire"))
            {
                status = true;
            }

            if (target.HasBuff("nocturneshroudofdarkness"))
            {
                status = true;
            }

            if (target.HasBuff("bansheesveil"))
            {
                status = true;
            }
            return status;
        }

        public static double GetAlliesComboDmg(Obj_AI_Hero target, Obj_AI_Hero ally)
        {
            var SpellSlots = new List<SpellSlot>();
            double dmg = 0;
            #region SpellSots
            SpellSlots.Add(SpellSlot.Q);
            SpellSlots.Add(SpellSlot.W);
            SpellSlots.Add(SpellSlot.E);
            SpellSlots.Add(SpellSlot.R);
            #endregion

            foreach (var slot in SpellSlots)
            {
                var spell = ally.Spellbook.GetSpell(slot);

                dmg += ally.GetSpellDamage(target, slot);
                dmg += ally.GetAutoAttackDamage(target);
            }

            return dmg;
        }

    }

    internal class Turret
    {
        public static bool IsUnderEnemyTurret(Obj_AI_Hero hero)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.Distance(hero.Position) < 950 && turret.IsEnemy);
        }

        public static bool IsUnderAllyTurret(Obj_AI_Hero hero)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.Distance(hero.Position) < 950 && turret.IsAlly);
        }
    }

}
