using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Collections.Generic;

namespace JinxsSupport.Plugins
{
    internal class Blitzcrank : IPlugin
    {
        private Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;

        private Spell E, Q, R, W;

        public Obj_AI_Hero Player {get { return ObjectManager.Player; }}

        public static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>(), Allies = new List<Obj_AI_Hero>();

        #region Load() Function
        public void Load()
        {
            Q = new Spell(SpellSlot.Q, 900);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 550);
            Q.SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                    Enemies.Add(hero);

                if (hero.IsAlly)
                    Allies.Add(hero);
            }

            Entry.PrintChat("<font color=\"#FFCC66\" >Blitzcrank</font>");
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            Config = new Menu("Vicroious Blitzcrank", "menu", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.GreenYellow);
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.SubMenu("Q option").AddItem(new MenuItem("minGrab", "Min range grab", true).SetValue(new Slider(450, 125, (int)Q.Range)));
            Config.SubMenu("Q option").AddItem(new MenuItem("maxGrab", "Max range grab", true).SetValue(new Slider((int)Q.Range, 125, (int)Q.Range)));
            Config.SubMenu("Q option").AddItem(new MenuItem("qTur", "Auto Q under turret", true).SetValue(false));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu("Q option").SubMenu("Grab").AddItem(new MenuItem("grab" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu("R option").AddItem(new MenuItem("rCount", "Auto R if enemies in range", true).SetValue(new Slider(3, 0, 5)));
            Config.SubMenu("R option").AddItem(new MenuItem("afterGrab", "Auto R after grab", true).SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("afterAA", "Auto R befor AA", true).SetValue(true));
            Config.SubMenu("R option").AddItem(new MenuItem("inter", "OnPossibleToInterrupt", true)).SetValue(true);
            Config.SubMenu("R option").AddItem(new MenuItem("Gap", "OnEnemyGapcloser", true)).SetValue(true);

            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy", true).SetValue(true));

            Config.AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.AddItem(new MenuItem("autoE", "Auto E", true).SetValue(true));

            Config.AddToMainMenu();

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
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(Player.Position, (float)Config.Item("maxGrab", true).GetValue<Slider>().Value, System.Drawing.Color.White, 1);
                }
                else
                    Render.Circle.DrawCircle(Player.Position, (float)Config.Item("maxGrab", true).GetValue<Slider>().Value, System.Drawing.Color.White, 1);
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
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (E.IsReady() && args.Target.IsValid<Obj_AI_Hero>() && Config.Item("autoE", true).GetValue<bool>())
                E.Cast();   
        }

        #region Q.Logic
        private void LogicQ()
        {
            float maxGrab = Config.Item("maxGrab", true).GetValue<Slider>().Value;
            float minGrab =  Config.Item("minGrab", true).GetValue<Slider>().Value;
            var qTur = Player.UnderAllyTurret() && Config.Item("qTur", true).GetValue<bool>();  // 내가 아군 미사일 터렛 밑에 있을때

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var t = TargetSelector.GetTarget(maxGrab, TargetSelector.DamageType.Physical);

                if (t.IsValidTarget(maxGrab) && !t.HasBuffOfType(BuffType.SpellImmunity) && !t.HasBuffOfType(BuffType.SpellShield) && 
                    Config.Item("grab" + t.ChampionName).GetValue<bool>() && Player.Distance(t.ServerPosition) > minGrab &&
                    !t.IsZombie )
                {
                    // 테스트 코드
                    var qpred = Q.GetPrediction(t);
                    if (qpred.CollisionObjects.Count(h => h.IsEnemy && !h.IsDead && h is Obj_AI_Minion) < 1)            // 미니언 충돌을 막기 위한 코드
                        Entry.OKTWCast_SebbyLib(Q, t, false);
                }
                    
            }

            if (qTur)       // 미사일 터렛 밑에 있을때는 콤보키와 상관없이 무조건 발동
            {
                foreach (var t in Enemies.Where(t => t.IsValidTarget(maxGrab) && Config.Item("grab" + t.ChampionName).GetValue<bool>()))
                {
                    if (!t.HasBuffOfType(BuffType.SpellImmunity) && !t.HasBuffOfType(BuffType.SpellShield) && 
                        Player.Distance(t.ServerPosition) > minGrab && 
                        Player.HealthPercent > 30 &&                                                            // 내 HP 가 40% 이상 남아있고, 리콜중이 아닐때
                        !t.IsZombie)
                    {
                        Entry.OKTWCast_SebbyLib(Q, t, false);
                    }
                }
            }

        }
        #endregion

        #region R.Logic
        private void LogicR()
        {
            bool afterGrab = Config.Item("afterGrab", true).GetValue<bool>();
            foreach (var target in Enemies.Where(target => target.IsValidTarget(R.Range)))
            {
                if (afterGrab && target.IsValidTarget(400) && target.HasBuff("rocketgrab2"))
                    R.Cast();
            }

            if (Player.CountEnemiesInRange(450) >= Config.Item("rCount", true).GetValue<Slider>().Value && Config.Item("rCount", true).GetValue<Slider>().Value > 0)
                R.Cast();
        }
        #endregion

        private void LogicW()
        {
            foreach (var target in Enemies.Where(target => target.IsValidTarget(450) && target.HasBuff("rocketgrab2")))
                W.Cast();
        }
    }
}
