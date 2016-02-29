﻿using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;
using System.Collections.Generic;

namespace JinxsSupport.Plugins
{
    internal class Blitzcrank : IPlugin
    {
        private Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;

        private Spell E, Q, R, W;

        private int grab = 0 , grabS = 0;

        private float grabW = 0;

        public Obj_AI_Hero Player {get { return ObjectManager.Player; }}

        public static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>(), Allies = new List<Obj_AI_Hero>();

        #region Load() Function
        public void Load()
        {
            Q = new Spell(SpellSlot.Q, 920);
            W = new Spell(SpellSlot.W, 200);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 600);
            Q.SetSkillshot(0.25f, 90f, 2100f, true, SkillshotType.SkillshotLine);

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                    Enemies.Add(hero);

                if (hero.IsAlly)
                    Allies.Add(hero);
            }
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            Config = new Menu("Vicroious Blitzcrank", "menu", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.GreenYellow);
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoE", "Auto E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("showgrab", "Show statistics", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("ts", "Use common TargetSelector", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("ts1", "ON - only one target"));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("ts2", "OFF - all grab-able targets"));

            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("qTur", "Auto Q under turret", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("qCC", "Auto Q cc & dash enemy", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("minGrab", "Min range grab", true).SetValue(new Slider(250, 125, (int)Q.Range)));
            Config.SubMenu(Player.ChampionName).SubMenu("Q option").AddItem(new MenuItem("maxGrab", "Max range grab", true).SetValue(new Slider((int)Q.Range, 125, (int)Q.Range)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Q option").SubMenu("Grab").AddItem(new MenuItem("grab" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("rCount", "Auto R if enemies in range", true).SetValue(new Slider(3, 0, 5)));
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("afterGrab", "Auto R after grab", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("afterAA", "Auto R befor AA", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("rKs", "R ks", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("inter", "OnPossibleToInterrupt", true)).SetValue(true);
            Config.SubMenu(Player.ChampionName).SubMenu("R option").AddItem(new MenuItem("Gap", "OnEnemyGapcloser", true)).SetValue(true);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy", true).SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }
        #endregion

        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if(Config.Item("afterAA", true).GetValue<bool>() && R.IsReady() && target is Obj_AI_Hero )
            {
                R.Cast();
            }
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (R.IsReady() && Config.Item("inter", true).GetValue<bool>() && sender.IsValidTarget(R.Range))
                R.Cast();
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "RocketGrabMissile")
            {
                Utility.DelayAction.Add(500, Orbwalking.ResetAutoAttackTimer);
                grab++;
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("showgrab", true).GetValue<bool>())
            {
                var percent = 0f;
                if (grab > 0)
                    percent = ((float)grabS / (float)grab) * 100f;
                Drawing.DrawText(Drawing.Width * 0f, Drawing.Height * 0.4f, System.Drawing.Color.YellowGreen, " grab: " + grab + " grab successful: " + grabS + " grab successful % : " + percent + "%");
            }
            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(Player.Position, (float)Config.Item("maxGrab", true).GetValue<Slider>().Value, System.Drawing.Color.Cyan, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, (float)Config.Item("maxGrab", true).GetValue<Slider>().Value, System.Drawing.Color.Cyan, 1);
            }
            if (Config.Item("rRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1);
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() && Config.Item("Gap", true).GetValue<bool>() && gapcloser.Sender.IsValidTarget(R.Range))
                R.Cast();
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Entry.LagFree(1) && Q.IsReady())
                LogicQ();
            if (Entry.LagFree(2) && R.IsReady())
                LogicR();
            if (Entry.LagFree(3) && W.IsReady() && Config.Item("autoW", true).GetValue<bool>())
                LogicW();

            if (!Q.IsReady() && Game.Time - grabW > 2)
            {
                foreach (var t in Enemies.Where(t => t.HasBuff("rocketgrab2")))
                {
                    grabS++;
                    grabW = Game.Time;
                }
            }
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (E.IsReady() && args.Target.IsValid<Obj_AI_Hero>() && Config.Item("autoE", true).GetValue<bool>())
                E.Cast();   
        }

        private void LogicQ()
        {
            float maxGrab = Config.Item("maxGrab", true).GetValue<Slider>().Value;
            float minGrab =  Config.Item("minGrab", true).GetValue<Slider>().Value;
            var ts = Config.Item("ts", true).GetValue<bool>();
            var qTur = Player.UnderAllyTurret() && Config.Item("qTur", true).GetValue<bool>();
            var qCC = Config.Item("qCC", true).GetValue<bool>();

            if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && ts)
            {
                var t = TargetSelector.GetTarget(maxGrab, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget(maxGrab) && !t.HasBuffOfType(BuffType.SpellImmunity) && !t.HasBuffOfType(BuffType.SpellShield) && Config.Item("grab" + t.ChampionName).GetValue<bool>() && Player.Distance(t.ServerPosition) > minGrab)
                    Entry.OKTWCast_SebbyLib(Q, t, false);
            }

            foreach (var t in Enemies.Where(t => t.IsValidTarget(maxGrab) && Config.Item("grab" + t.ChampionName).GetValue<bool>()))
            {
                if (!t.HasBuffOfType(BuffType.SpellImmunity) && !t.HasBuffOfType(BuffType.SpellShield) && Player.Distance(t.ServerPosition) > minGrab)
                {
                    if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && !ts)
                        Entry.OKTWCast_SebbyLib(Q, t, false);
                    else if (qTur)
                        Entry.OKTWCast_SebbyLib(Q, t, false);

                    if (qCC)
                    {
                        if(!OktwCommon.CanMove(t))
                            Q.Cast(t, true);
                        Q.CastIfHitchanceEquals(t, HitChance.Dashing);
                        Q.CastIfHitchanceEquals(t, HitChance.Immobile);
                    }
                }
            }
        }

        private void LogicR()
        {
            bool rKs = Config.Item("rKs", true).GetValue<bool>();
            bool afterGrab = Config.Item("afterGrab", true).GetValue<bool>();
            foreach (var target in Enemies.Where(target => target.IsValidTarget(R.Range)))
            {
                if (rKs && R.GetDamage(target) > target.Health)
                    R.Cast();
                if (afterGrab && target.IsValidTarget(400) && target.HasBuff("rocketgrab2"))
                    R.Cast();
            }
            if (Player.CountEnemiesInRange(R.Range) >= Config.Item("rCount", true).GetValue<Slider>().Value && Config.Item("rCount", true).GetValue<Slider>().Value > 0)
                R.Cast();
        }
        private void LogicW()
        {
            foreach (var target in Enemies.Where(target => target.IsValidTarget(R.Range) && target.HasBuff("rocketgrab2")))
                W.Cast();
        }
    }
}
