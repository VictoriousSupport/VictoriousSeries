using System;
using System.Collections.Generic;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;

namespace JinxsSupport.Plugins
{

    internal class Katarina : IPlugin
    {
        #region Static Fields

        public static Vector2 JumpPos;

        private static readonly bool castWardAgain = true;

        private static readonly Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                                       {
                                                                           { Spells.Q, new Spell(SpellSlot.Q, 675) },
                                                                           { Spells.W, new Spell(SpellSlot.W, 375) },
                                                                           { Spells.E, new Spell(SpellSlot.E, 700) },
                                                                           { Spells.R, new Spell(SpellSlot.R, 550) }
                                                                       };

        private static SpellSlot Ignite;

        //private static bool isChanneling;

        private static long lastECast;

        private static int lastPlaced;

        private static Vector3 lastWardPos;

        private static Orbwalking.Orbwalker Orbwalker;

        private static bool reCheckWard = true;

        private static float rStart;

        private static float wcasttime;

        #endregion

        #region Enums

        public enum Spells
        {
            Q,
            W,
            E,
            R
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the menu.
        /// </summary>
        /// <value>
        ///     The menu.
        /// </value>
        private Menu Menu { get; set; }

        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        private Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        #endregion

        #region Public Methods and Operators

        public void CreateMenu()
        {
            
            Menu = new Menu("Vicroious Katarina", "ElKatarina", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.GreenYellow);
            {
                var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
                Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
                Menu.AddSubMenu(orbwalkerMenu);

                var targetSelector = new Menu("Target Selector", "TargetSelector");
                TargetSelector.AddToMenu(targetSelector);
                Menu.AddSubMenu(targetSelector);

                var comboMenu = new Menu("Combo", "Combo");
                {
                    comboMenu.AddItem(new MenuItem("ElEasy.Katarina.Combo.Q", "Use Q").SetValue(true));
                    comboMenu.AddItem(new MenuItem("ElEasy.Katarina.Combo.W", "Use W").SetValue(true));
                    comboMenu.AddItem(new MenuItem("ElEasy.Katarina.Combo.E", "Use E").SetValue(false));

                    comboMenu.SubMenu("E").AddItem(new MenuItem("ElEasy.Katarina.E.Legit", "Legit E").SetValue(false));
                    comboMenu.SubMenu("E")
                        .AddItem(new MenuItem("ElEasy.Katarina.E.Delay", "E Delay").SetValue(new Slider(1000, 0, 2000)));

                    comboMenu.SubMenu("R").AddItem(new MenuItem("ElEasy.Katarina.Combo.R", "Use R").SetValue(true));
                    comboMenu.SubMenu("R")
                        .AddItem(
                            new MenuItem("ElEasy.Katarina.Combo.Sort", "R:").SetValue(
                                new StringList(new[] { "Normal", "Smart" },1)));
                    comboMenu.SubMenu("R")
                        .AddItem(new MenuItem("ElEasy.Katarina.Combo.R.Force", "Force R").SetValue(false));
                    comboMenu.SubMenu("R")
                        .AddItem(
                            new MenuItem("ElEasy.Katarina.Combo.R.Force.Count", "Force R when in range:").SetValue(new Slider(3, 0, 5)));
                    comboMenu.AddItem(new MenuItem("ElEasy.Katarina.Combo.Ignite", "Use Ignite").SetValue(false));
                }

                Menu.AddSubMenu(comboMenu);

                var harassMenu = new Menu("Harass", "Harass");
                {
                    harassMenu.AddItem(new MenuItem("ElEasy.Katarina.Harass.Q", "Use Q").SetValue(true));
                    harassMenu.AddItem(new MenuItem("ElEasy.Katarina.Harass.W", "Use W").SetValue(true));
                    harassMenu.AddItem(new MenuItem("ElEasy.Katarina.Harass.E", "Use E").SetValue(true));

                    harassMenu.SubMenu("Harass")
                        .SubMenu("AutoHarass settings")
                        .AddItem(
                            new MenuItem("ElEasy.Katarina.AutoHarass.Activated", "Auto harass", true).SetValue(
                                new KeyBind("L".ToCharArray()[0], KeyBindType.Toggle, true)));
                    harassMenu.SubMenu("Harass")
                        .SubMenu("AutoHarass settings")
                        .AddItem(new MenuItem("ElEasy.Katarina.AutoHarass.Q", "Use Q").SetValue(true));
                    harassMenu.SubMenu("Harass")
                        .SubMenu("AutoHarass settings")
                        .AddItem(new MenuItem("ElEasy.Katarina.AutoHarass.W", "Use W").SetValue(true));

                    harassMenu.SubMenu("Harass")
                        .AddItem(
                            new MenuItem("ElEasy.Katarina.Harass.Mode", "Harass mode:").SetValue(
                                new StringList(new[] { "Q", "Q - W", "Q - E - W" },1)));
                }

                Menu.AddSubMenu(harassMenu);

                var clearMenu = new Menu("Clear", "Clear");
                {
                    clearMenu.SubMenu("Lasthit")
                        .AddItem(new MenuItem("ElEasy.Katarina.Lasthit.Q", "Use Q").SetValue(true));
                    clearMenu.SubMenu("Lasthit")
                        .AddItem(new MenuItem("ElEasy.Katarina.Lasthit.W", "Use W").SetValue(true));
                    clearMenu.SubMenu("Lasthit")
                        .AddItem(new MenuItem("ElEasy.Katarina.Lasthit.E", "Use E").SetValue(false));
                    clearMenu.SubMenu("Laneclear")
                        .AddItem(new MenuItem("ElEasy.Katarina.LaneClear.Q", "Use Q").SetValue(false));
                    clearMenu.SubMenu("Laneclear")
                        .AddItem(new MenuItem("ElEasy.Katarina.LaneClear.W", "Use W").SetValue(false));
                    clearMenu.SubMenu("Laneclear")
                        .AddItem(new MenuItem("ElEasy.Katarina.LaneClear.E", "Use E").SetValue(false));
                    clearMenu.SubMenu("Jungleclear")
                        .AddItem(new MenuItem("ElEasy.Katarina.JungleClear.Q", "Use Q").SetValue(true));
                    clearMenu.SubMenu("Jungleclear")
                        .AddItem(new MenuItem("ElEasy.Katarina.JungleClear.W", "Use W").SetValue(true));
                    clearMenu.SubMenu("Jungleclear")
                        .AddItem(new MenuItem("ElEasy.Katarina.JungleClear.E", "Use E").SetValue(false));
                }

                Menu.AddSubMenu(clearMenu);

                var wardjumpMenu = new Menu("Wardjump", "Wardjump");
                {
                    wardjumpMenu.AddItem(
                        new MenuItem("ElEasy.Katarina.Wardjump", "Wardjump key").SetValue(
                            new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

                    wardjumpMenu.AddItem(new MenuItem("ElEasy.Wardjump.Mouse", "Move to mouse").SetValue(true));
                    wardjumpMenu.AddItem(new MenuItem("ElEasy.Wardjump.Minions", "Jump to minions").SetValue(true));
                    wardjumpMenu.AddItem(new MenuItem("ElEasy.Wardjump.Champions", "Jump to champions").SetValue(true));
                }

                Menu.AddSubMenu(wardjumpMenu);

                var killstealMenu = new Menu("Killsteal", "Killsteal");
                {
                    killstealMenu.AddItem(new MenuItem("ElEasy.Katarina.Killsteal", "Killsteal").SetValue(true));
                    killstealMenu.AddItem(
                        new MenuItem("ElEasy.Katarina.Killsteal.R", "Killsteal with R").SetValue(true));
                }

                Menu.AddSubMenu(killstealMenu);

                var miscellaneousMenu = new Menu("Miscellaneous", "Miscellaneous");
                {
                    miscellaneousMenu.AddItem(new MenuItem("ElEasy.Katarina.Draw.AA", "Draw AA", true).SetValue(true));
                    miscellaneousMenu.AddItem(new MenuItem("ElEasy.Katarina.Draw.Q", "Draw Q", true).SetValue(false));
                    miscellaneousMenu.AddItem(new MenuItem("ElEasy.Katarina.Draw.W", "Draw W", true).SetValue(false));
                    miscellaneousMenu.AddItem(new MenuItem("ElEasy.Katarina.Draw.E", "Draw E", true).SetValue(true));
                    miscellaneousMenu.AddItem(new MenuItem("ElEasy.Katarina.Draw.R", "Draw R", true).SetValue(false));

                    var dmgAfterE = new MenuItem("ElEasy.Katarina.DrawComboDamage", "Draw combo damage").SetValue(true);
                    var drawFill =
                        new MenuItem("ElEasy.Katarina.DrawColour", "Fill colour", true).SetValue(
                            new Circle(true, Color.FromArgb(204, 204, 0, 0)));
                    miscellaneousMenu.AddItem(drawFill);
                    miscellaneousMenu.AddItem(dmgAfterE);

                    DrawDamage.DamageToUnit = GetComboDamage;
                    DrawDamage.Enabled = dmgAfterE.IsActive();
                    DrawDamage.Fill = drawFill.GetValue<Circle>().Active;
                    DrawDamage.FillColor = drawFill.GetValue<Circle>().Color;

                    dmgAfterE.ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs eventArgs)
                            {
                                DrawDamage.Enabled = eventArgs.GetNewValue<bool>();
                            };

                    drawFill.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
                        {
                            DrawDamage.Fill = eventArgs.GetNewValue<Circle>().Active;
                            DrawDamage.FillColor = eventArgs.GetNewValue<Circle>().Color;
                        };
                }

                Menu.AddSubMenu(miscellaneousMenu);
            }
            Menu.AddToMainMenu();

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnIssueOrder += Obj_AI_Hero_OnIssueOrder;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalking.BeforeAttack += BeforeAttack;
        }

        public void Load()
        {
            try
            {
                Ignite = Player.GetSpellSlot("summonerdot");
                spells[Spells.R].SetCharged("KatarinaR", "KatarinaR", 550, 550, 1.0f);
                Entry.PrintChat("<font color=\"#FFCCFF\" >Katarina</font>");

//                isChanneling = false;
                lastPlaced = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region Methods

        private static void CastEWard(Obj_AI_Base obj)
        {
            if (500 >= Environment.TickCount - wcasttime)
            {
                return;
            }

            spells[Spells.E].CastOnUnit(obj);
            wcasttime = Environment.TickCount;
        }

        private static bool KatarinaQ(Obj_AI_Base target)
        {
            return target.Buffs.Any(x => x.Name.Contains("katarinaqmark"));
        }

        private void CastE(Obj_AI_Base unit)
        {
            var playLegit = Menu.Item("ElEasy.Katarina.E.Legit").IsActive();
            var legitCastDelay = Menu.Item("ElEasy.Katarina.E.Delay").GetValue<Slider>().Value;

            if (playLegit)
            {
                if (Environment.TickCount > lastECast + legitCastDelay)
                {
                    spells[Spells.E].CastOnUnit(unit);
                    lastECast = Environment.TickCount;
                }
            }
            else
            {
                spells[Spells.E].CastOnUnit(unit);
                lastECast = Environment.TickCount;
            }
        }

        private InventorySlot FindBestWardItem()
        {
            var slot = Items.GetWardSlot();
            if (slot == default(InventorySlot))
            {
                return null;
            }

            var sdi = GetItemSpell(slot);

            if (sdi != default(SpellDataInst) && sdi.State == SpellState.Ready)
            {
                return slot;
            }
            return slot;
        }

        private void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!spells[Spells.E].IsReady() || !(sender is Obj_AI_Minion) || Environment.TickCount >= lastPlaced + 300)
            {
                return;
            }

            if (Environment.TickCount >= lastPlaced + 300)
            {
                return;
            }
            var ward = (Obj_AI_Minion)sender;

            if (ward.Name.ToLower().Contains("ward") && ward.Distance(lastWardPos) < 500)
            {
                spells[Spells.E].Cast(ward);
            }
        }

        private double QMarkDamage(Obj_AI_Base target)
        {
            return target.HasBuff("katarinaqmark") ? Player.GetSpellDamage(target, SpellSlot.Q, 1) : 0;
        }

        private float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (spells[Spells.Q].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            }

            damage += QMarkDamage(enemy);

            if (spells[Spells.W].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            }

            if (spells[Spells.E].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);
            }

            if (spells[Spells.R].IsReady())
            {
                damage += Player.GetSpellDamage(enemy, SpellSlot.R) * 8;
            }

            return (float)damage;
        }

        private SpellDataInst GetItemSpell(InventorySlot invSlot)
        {
            return Player.Spellbook.Spells.FirstOrDefault(spell => (int)spell.Slot == invSlot.Slot + 4);
        }

        private bool HasRBuff()
        {
            return Player.HasBuff("KatarinaR") || Player.IsChannelingImportantSpell()
                   || Player.HasBuff("katarinarsound");
        }

        private float IgniteDamage(Obj_AI_Hero target)
        {
            if (Ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        private void KillSteal()
        {
            foreach (
                var hero in
                    HeroManager.Enemies
                        .Where(hero => hero.IsValidTarget(spells[Spells.E].Range) && !hero.IsInvulnerable))
            {
                var qdmg = spells[Spells.Q].GetDamage(hero);
                var wdmg = spells[Spells.W].GetDamage(hero);
                var edmg = spells[Spells.E].GetDamage(hero);
                var markDmg = Player.CalcDamage(
                    hero,
                    Damage.DamageType.Magical,
                    Player.FlatMagicDamageMod * 0.15 + Player.Level * 15);
                float ignitedmg;

                if (Ignite != SpellSlot.Unknown)
                {
                    ignitedmg = (float)Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
                }
                else
                {
                    ignitedmg = 0f;
                }

                if (hero.HasBuff("katarinaqmark") && hero.Health - wdmg - markDmg < 0 && spells[Spells.W].IsReady()
                    && hero.IsValidTarget(spells[Spells.W].Range))
                {
                    spells[Spells.W].Cast();
                }

                if (hero.Health - ignitedmg < 0 && Ignite.IsReady() && hero.IsValidTarget(600))
                {
                    Player.Spellbook.CastSpell(Ignite, hero);
                }

                if (hero.Health - edmg < 0 && spells[Spells.E].IsReady() && hero.IsValidTarget(spells[Spells.E].Range))
                {
                    spells[Spells.E].Cast(hero);
                }

                if (hero.Health - qdmg < 0 && spells[Spells.Q].IsReady() && spells[Spells.Q].IsInRange(hero))
                {
                    spells[Spells.Q].Cast(hero);
                }

                if (hero.Health - edmg - wdmg < 0 && spells[Spells.E].IsReady() && spells[Spells.W].IsReady()
                    && hero.IsValidTarget(spells[Spells.E].Range))
                {
                    CastE(hero);
                    if (spells[Spells.W].IsInRange(hero))
                    {
                        spells[Spells.W].Cast();
                    }
                }

                if (hero.Health - edmg - qdmg < 0 && spells[Spells.E].IsReady() && spells[Spells.Q].IsReady()
                    && hero.IsValidTarget(spells[Spells.E].Range))
                {
                    CastE(hero);
                    spells[Spells.Q].Cast(hero);
                }

                if (hero.Health - edmg - wdmg - qdmg < 0 && spells[Spells.E].IsReady() && spells[Spells.Q].IsReady()
                    && spells[Spells.W].IsReady() && hero.IsValidTarget(spells[Spells.E].Range))
                {
                    CastE(hero);
                    spells[Spells.Q].Cast(hero);
                    if (hero.IsValidTarget(spells[Spells.W].Range))
                    {
                        spells[Spells.W].Cast();
                    }
                }

                if (hero.Health - edmg - wdmg - qdmg - markDmg < 0 && spells[Spells.E].IsReady()
                    && spells[Spells.Q].IsReady() && spells[Spells.W].IsReady()
                    && hero.IsValidTarget(spells[Spells.E].Range))
                {
                    CastE(hero);
                    spells[Spells.Q].Cast(hero);
                    if (hero.IsValidTarget(spells[Spells.W].Range))
                    {
                        spells[Spells.W].Cast();
                    }
                }

                if (hero.Health - edmg - wdmg - qdmg - ignitedmg < 0 && spells[Spells.E].IsReady()
                    && spells[Spells.Q].IsReady() && spells[Spells.W].IsReady() && Ignite.IsReady()
                    && hero.IsValidTarget(spells[Spells.E].Range))
                {
                    CastE(hero);
                    spells[Spells.Q].Cast(hero);
                    if (hero.IsValidTarget(spells[Spells.W].Range))
                    {
                        spells[Spells.W].Cast();
                        Player.Spellbook.CastSpell(Ignite, hero);
                    }
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || args.SData.Name != "KatarinaR" || !Player.HasBuff("katarinarsound"))
            {
                return;
            }

            //isChanneling = true;
            Orbwalker.SetMovement(false);
            Orbwalker.SetAttack(false);
            //Utility.DelayAction.Add(1, () => isChanneling = false);
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe)
            {
                args.Process = !Player.HasBuff("KatarinaR");
            }
        }

        private void Obj_AI_Hero_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender.IsMe && Utils.GameTimeTickCount < rStart  && args.Order == GameObjectOrder.MoveTo)
            {
                args.Process = false;
            }
        }

        private void OnAutoHarass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValidTarget() || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            var useQ = Menu.Item("ElEasy.Katarina.AutoHarass.Q").IsActive();
            var useW = Menu.Item("ElEasy.Katarina.AutoHarass.W").IsActive();

            if (useQ && spells[Spells.Q].IsReady() && target.IsValidTarget())
            {
                spells[Spells.Q].Cast(target);
            }

            if (useW && spells[Spells.W].IsReady() && target.IsValidTarget(spells[Spells.W].Range))
            {
                spells[Spells.W].Cast();
            }
        }

        private void OnCombo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            //UseItems(target);

            var useQ = Menu.Item("ElEasy.Katarina.Combo.Q").IsActive();
            var useW = Menu.Item("ElEasy.Katarina.Combo.W").IsActive();
            var useE = Menu.Item("ElEasy.Katarina.Combo.E").IsActive();
            var useR = Menu.Item("ElEasy.Katarina.Combo.R").IsActive();
            var useI = Menu.Item("ElEasy.Katarina.Combo.Ignite").IsActive();
            var rSort = Menu.Item("ElEasy.Katarina.Combo.Sort").GetValue<StringList>();
            var forceR = Menu.Item("ElEasy.Katarina.Combo.R.Force").IsActive();
            var forceRCount = Menu.Item("ElEasy.Katarina.Combo.R.Force.Count").GetValue<Slider>().Value;

            if (useQ && spells[Spells.Q].IsReady() && target.IsValidTarget(spells[Spells.Q].Range))
            {
                spells[Spells.Q].Cast(target);
            }

            if (useE && spells[Spells.E].IsReady() && target.IsValidTarget(spells[Spells.E].Range))
            {
                CastE(target);
            }

            if (useW && spells[Spells.W].IsReady() && target.IsValidTarget(spells[Spells.W].Range))
            {
                spells[Spells.W].Cast();
            }

            if (useR && spells[Spells.R].IsReady())
            {
                switch (rSort.SelectedIndex)
                {
                    case 0:
                        if (Player.CountEnemiesInRange(spells[Spells.R].Range) > 0 && spells[Spells.R].IsReady())
                        {
                            spells[Spells.R].Cast();
                            Orbwalker.SetMovement(false);
                            Orbwalker.SetAttack(false);
                            rStart = Utils.GameTimeTickCount + 400;
                        }
                        break;

                    case 1:
                        if (!spells[Spells.E].IsReady() || forceR && Player.CountEnemiesInRange(spells[Spells.R].Range) <= forceRCount)
                        {
                            spells[Spells.R].Cast();
                            Orbwalker.SetMovement(false);
                            Orbwalker.SetAttack(false);
                            rStart = Utils.GameTimeTickCount + 400;
                        }
                        break;
                }
            }

            if (target.IsValidTarget(600) && IgniteDamage(target) >= target.Health && useI)
            {
                Player.Spellbook.CastSpell(Ignite, target);
            }
        }

        private void OnDraw(EventArgs args)
        {
            var drawQ = Menu.Item("ElEasy.Katarina.Draw.Q", true).GetValue<bool>();
            var drawW = Menu.Item("ElEasy.Katarina.Draw.W", true).GetValue<bool>();
            var drawE = Menu.Item("ElEasy.Katarina.Draw.E", true).GetValue<bool>();
            var drawR = Menu.Item("ElEasy.Katarina.Draw.R", true).GetValue<bool>();


            if (Menu.Item("ElEasy.Katarina.Draw.AA", true).GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, Player.AttackRange + Player.BoundingRadius, System.Drawing.Color.DarkGray, 1);


            if (drawQ)
            {
                if (spells[Spells.Q].Level > 0)
                {
                    Render.Circle.DrawCircle(Player.Position, spells[Spells.Q].Range, Color.DeepPink);
                }
            }

            if (drawW)
            {
                if (spells[Spells.W].Level > 0)
                {
                    Render.Circle.DrawCircle(Player.Position, spells[Spells.W].Range, Color.DeepSkyBlue);
                }
            }

            if (drawE)
            {
                    Render.Circle.DrawCircle(Player.Position, spells[Spells.E].Range, Color.White,1);
            }

            if (drawR)
            {
                if (spells[Spells.R].Level > 0)
                {
                    Render.Circle.DrawCircle(Player.Position, spells[Spells.R].Range, Color.White);
                }
            }
        }

        private void OnHarass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            var useQ = Menu.Item("ElEasy.Katarina.Harass.Q").IsActive();
            var useW = Menu.Item("ElEasy.Katarina.Harass.W").IsActive();
            var useE = Menu.Item("ElEasy.Katarina.Harass.E").IsActive();
            var hMode = Menu.Item("ElEasy.Katarina.Harass.Mode").GetValue<StringList>().SelectedIndex;

            switch (hMode)
            {
                case 0:
                    if (useQ && spells[Spells.Q].IsReady())
                    {
                        spells[Spells.Q].CastOnUnit(target);
                    }
                    break;

                case 1:
                    if (useQ && useW)
                    {
                        if (spells[Spells.Q].IsReady() && target.IsValidTarget(spells[Spells.Q].Range))
                        {
                            spells[Spells.Q].Cast(target);
                        }

                        if (target.IsValidTarget(spells[Spells.W].Range) && spells[Spells.W].IsReady())
                        {
                            spells[Spells.W].Cast();
                        }
                    }
                    break;

                case 2:
                    if (useQ && useW && useE)
                    {
                        if (spells[Spells.Q].IsReady() && target.IsValidTarget(spells[Spells.Q].Range))
                        {
                            spells[Spells.Q].Cast(target);
                        }

                        if (spells[Spells.E].IsReady() && target.IsValidTarget(spells[Spells.E].Range))
                        {
                            CastE(target);
                        }

                        if (spells[Spells.W].IsReady() && target.IsValidTarget(spells[Spells.W].Range))
                        {
                            spells[Spells.W].Cast();
                        }
                    }
                    break;
            }
        }

        private void OnJungleclear()
        {
            var useQ = Menu.Item("ElEasy.Katarina.JungleClear.Q").IsActive();
            var useW = Menu.Item("ElEasy.Katarina.JungleClear.W").IsActive();
            var useE = Menu.Item("ElEasy.Katarina.JungleClear.E").IsActive();

            var minions =
                MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    spells[Spells.E].Range,
                    MinionTypes.All,
                    MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (minions == null)
            {
                return;
            }

            if (useQ && spells[Spells.Q].IsReady())
            {
                spells[Spells.Q].Cast(minions);
            }

            if (useW && spells[Spells.W].IsReady() && spells[Spells.W].IsInRange(minions))
            {
                spells[Spells.W].Cast();
            }

            if (useE && spells[Spells.E].IsReady())
            {
                CastE(minions);
            }
        }

        private void OnLaneclear()
        {
            var useQ = Menu.Item("ElEasy.Katarina.LaneClear.Q").IsActive();
            var useW = Menu.Item("ElEasy.Katarina.LaneClear.W").IsActive();
            var useE = Menu.Item("ElEasy.Katarina.LaneClear.E").IsActive();

            var minions =
                MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spells[Spells.E].Range).FirstOrDefault();

            if (minions == null)
            {
                return;
            }

            if (spells[Spells.W].IsReady() && useW)
            {
                if (minions.Health < spells[Spells.W].GetDamage(minions))
                {
                    spells[Spells.W].Cast();
                }
            }

            if (spells[Spells.Q].IsReady() && useQ)
            {
                if (minions.Health < spells[Spells.Q].GetDamage(minions))
                {
                    spells[Spells.Q].CastOnUnit(minions);
                }
            }

            if (spells[Spells.E].IsReady() && useE)
            {
                if (minions.Health < spells[Spells.E].GetDamage(minions))
                {
                    CastE(minions);
                }
            }
        }

        private void OnLasthit()
        {
            var useQ = Menu.Item("ElEasy.Katarina.Lasthit.Q").IsActive();
            var useW = Menu.Item("ElEasy.Katarina.Lasthit.W").IsActive();
            var useE = Menu.Item("ElEasy.Katarina.Lasthit.E").IsActive();

            var minions =
                MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spells[Spells.E].Range).FirstOrDefault();

            if (minions == null)
            {
                return;
            }

            if (spells[Spells.W].IsReady() && useW)
            {
                if (minions.Health < spells[Spells.W].GetDamage(minions))
                {
                    spells[Spells.W].Cast();
                }
            }

            if (spells[Spells.Q].IsReady() && useQ)
            {
                if (minions.Health < spells[Spells.Q].GetDamage(minions))
                {
                    spells[Spells.Q].CastOnUnit(minions);
                }
            }

            if (spells[Spells.E].IsReady() && useE)
            {
                if (minions.Health < spells[Spells.E].GetDamage(minions))
                {
                    CastE(minions);
                }
            }
        }

        private void OnUpdate(EventArgs args)
        {
            try
            {
                if (Player.IsDead)
                {
                    return;
                }

                if (HasRBuff())
                {
                    Orbwalker.SetAttack(false);
                    Orbwalker.SetMovement(false);
                }
                else
                {
                    Orbwalker.SetAttack(true);
                    Orbwalker.SetMovement(true);
                }


                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        OnCombo();
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        OnHarass();
                        break;

                    case Orbwalking.OrbwalkingMode.LaneClear:
                        OnLaneclear();
                        OnJungleclear();
                        break;

                    case Orbwalking.OrbwalkingMode.LastHit:
                        OnLasthit();
                        break;
                }

                if (Menu.Item("ElEasy.Katarina.Killsteal").IsActive())
                {
                    KillSteal();
                }

                if (Menu.Item("ElEasy.Katarina.AutoHarass.Activated", true).GetValue<KeyBind>().Active)
                {
                    OnAutoHarass();
                }

                if (Menu.Item("ElEasy.Katarina.Wardjump").GetValue<KeyBind>().Active)
                {
                    WardjumpToMouse();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Orbwalk(Vector3 pos, Obj_AI_Hero target = null)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }

        private void WardJump(
            Vector3 pos,
            bool m2M = true,
            bool maxRange = false,
            bool reqinMaxRange = false,
            bool minions = true,
            bool champions = true)
        {
            if (!spells[Spells.E].IsReady())
            {
                return;
            }

            var basePos = Player.Position.To2D();
            var newPos = (pos.To2D() - Player.Position.To2D());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange)
                {
                    JumpPos = pos.To2D();
                }
                else if (maxRange || Player.Distance(pos) > 590)
                {
                    JumpPos = basePos + (newPos.Normalized() * (590));
                }
                else
                {
                    JumpPos = basePos + (newPos.Normalized() * (Player.Distance(pos)));
                }
            }
            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Utility.DelayAction.Add(
                    20,
                    () =>
                        {
                            if (JumpPos != new Vector2())
                            {
                                JumpPos = new Vector2();
                                reCheckWard = true;
                            }
                        });
            }
            if (m2M)
            {
                Orbwalk(pos);
            }
            if (!spells[Spells.E].IsReady() || reqinMaxRange && Player.Distance(pos) > spells[Spells.E].Range)
            {
                return;
            }

            if (minions || champions)
            {
                if (champions)
                {
                    var champs = (from champ in ObjectManager.Get<Obj_AI_Hero>()
                                  where
                                      champ.IsAlly && champ.Distance(Player) < spells[Spells.E].Range
                                      && champ.Distance(pos) < 200 && !champ.IsMe
                                  select champ).ToList();
                    if (champs.Count > 0 && spells[Spells.E].IsReady())
                    {
                        if (500 >= Environment.TickCount - wcasttime || !spells[Spells.E].IsReady())
                        {
                            return;
                        }

                        CastEWard(champs[0]);
                        return;
                    }
                }
                if (minions)
                {
                    var minion2 = (from minion in ObjectManager.Get<Obj_AI_Minion>()
                                   where
                                       minion.IsAlly && minion.Distance(Player) < spells[Spells.E].Range
                                       && minion.Distance(pos) < 200 && !minion.Name.ToLower().Contains("ward")
                                   select minion).ToList();
                    if (minion2.Count > 0)
                    {
                        if (500 >= Environment.TickCount - wcasttime || !spells[Spells.E].IsReady())
                        {
                            return;
                        }

                        CastEWard(minion2[0]);
                        return;
                    }
                }
            }

            var isWard = false;
            foreach (var ward in ObjectManager.Get<Obj_AI_Base>())
            {
                if (ward.IsAlly && ward.Name.ToLower().Contains("ward") && ward.Distance(JumpPos) < 200)
                {
                    isWard = true;
                    if (500 >= Environment.TickCount - wcasttime || !spells[Spells.E].IsReady())
                    {
                        return;
                    }

                    CastEWard(ward);
                    wcasttime = Environment.TickCount;
                }
            }

            if (!isWard && castWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null || !spells[Spells.E].IsReady())
                {
                    return;
                }

                Player.Spellbook.CastSpell(ward.SpellSlot, JumpPos.To3D());
                lastWardPos = JumpPos.To3D();
            }
        }

        private void WardjumpToMouse()
        {
            WardJump(
                Game.CursorPos,
                Menu.Item("ElEasy.Wardjump.Mouse").IsActive(),
                false,
                false,
                Menu.Item("ElEasy.Wardjump.Minions").IsActive(),
                Menu.Item("ElEasy.Wardjump.Champions").IsActive());
        }

        #endregion
    }
    internal class DrawDamage //by xSalice
    {
        #region Constants
        private const int Height = 8;
        private const int Width = 103;
        private const int XOffset = 10;
        private const int YOffset = 20;
        #endregion

        #region Static Fields

        public static Color Color = Color.Lime;
        public static bool Enabled = true;
        public static bool Fill = true;
        public static Color FillColor = Color.YellowGreen;
        private static readonly Render.Text Text = new Render.Text(0, 0, "", 14, SharpDX.Color.Red, "monospace");
        private static DamageToUnitDelegate _damageToUnit;

        #endregion

        #region Delegates
        public delegate float DamageToUnitDelegate(Obj_AI_Hero hero);
        #endregion

        #region Public Properties

        public static DamageToUnitDelegate DamageToUnit
        {
            get
            {
                return _damageToUnit;
            }

            set
            {
                if (_damageToUnit == null)
                {
                    Drawing.OnDraw += Drawing_OnDraw;
                }
                _damageToUnit = value;
            }
        }

        #endregion

        #region Methods

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Enabled || _damageToUnit == null)
            {
                return;
            }

            foreach (var unit in HeroManager.Enemies.Where(h => h.IsValid && h.IsHPBarRendered))
            {
                var barPos = unit.HPBarPosition;
                var damage = _damageToUnit(unit);
                var percentHealthAfterDamage = Math.Max(0, unit.Health - damage) / unit.MaxHealth;
                var yPos = barPos.Y + YOffset;
                var xPosDamage = barPos.X + XOffset + Width * percentHealthAfterDamage;
                var xPosCurrentHp = barPos.X + XOffset + Width * unit.Health / unit.MaxHealth;

                if (damage > unit.Health)
                {
                    Text.X = (int)barPos.X + XOffset;
                    Text.Y = (int)barPos.Y + YOffset - 13;
                    Text.text = "Killable: " + (unit.Health - damage);
                    Text.OnEndScene();
                }

                Drawing.DrawLine(xPosDamage, yPos, xPosDamage, yPos + Height, 1, Color);

                if (Fill)
                {
                    var differenceInHP = xPosCurrentHp - xPosDamage;
                    var pos1 = barPos.X + 9 + (107 * percentHealthAfterDamage);

                    for (var i = 0; i < differenceInHP; i++)
                    {
                        Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + Height, 1, FillColor);
                    }
                }
            }
        }

        #endregion
    }
}