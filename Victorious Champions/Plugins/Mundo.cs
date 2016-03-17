namespace JinxsSupport.Plugins
{
    using System;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.Common;

    internal class Mundo : IPlugin
    {
        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static Menu config;
        private static SebbyLib.Orbwalking.Orbwalker orbwalker;
        private static Spell q, w, e, r;
        private static SpellDataInst ignite;

        #region Load() Function
        public void Load()
        {
            try
            {
                if (ObjectManager.Player.ChampionName != "DrMundo") return;

                q = new Spell(SpellSlot.Q, 975);
                w = new Spell(SpellSlot.W, 325);
                e = new Spell(SpellSlot.E, 150);
                r = new Spell(SpellSlot.R);

                q.SetSkillshot(0.275f, 60, 1850, true, SkillshotType.SkillshotLine);

                ignite = Player.Spellbook.GetSpell(Player.GetSpellSlot("summonerdot"));

                Game.OnUpdate += OnUpdate;
                SebbyLib.Orbwalking.AfterAttack += AfterAttack;
                Drawing.OnDraw += OnDraw;

                Entry.PrintChat("<font color=\"#FFCC66\" >Mundo</font>");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
        #endregion


        #region CreateMenu Function
        public void CreateMenu()
        {
            try
            {
                config = new Menu("Victorious Mundo", Player.ChampionName, true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.GreenYellow); ;

                //Adds the Orbwalker to the main menu
                var orbwalkerMenu = config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
                orbwalker = new SebbyLib.Orbwalking.Orbwalker(orbwalkerMenu);

                //Adds the TS to the main menu
                var tsMenu = config.AddSubMenu(new Menu("Target Selector", "TargetSelector"));
                TargetSelector.AddToMenu(tsMenu);

                var combo = config.AddSubMenu(new Menu("Combo Settings", "Combo"));
                var comboQ = combo.AddSubMenu(new Menu("Q Settings", "Q"));
                comboQ.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
                comboQ.AddItem(new MenuItem("QHealthCombo", "Minimum HP% to use Q").SetValue(new Slider(20, 1)));

                var comboW = combo.AddSubMenu(new Menu("W Settings", "W"));
                comboW.AddItem(new MenuItem("useW", "Use W").SetValue(true));
                comboW.AddItem(new MenuItem("WHealthCombo", "Minimum HP% to use W").SetValue(new Slider(20, 1)));
                var comboE = combo.AddSubMenu(new Menu("E Settings", "E"));
                comboE.AddItem(new MenuItem("useE", "Use E").SetValue(true));

                var harass = config.AddSubMenu(new Menu("Harass Settings", "Harass"));
                var harassQ = harass.AddSubMenu(new Menu("Q Settings", "Q"));
                harassQ.AddItem(new MenuItem("useQHarass", "Use Q").SetValue(true));
                harassQ.AddItem(new MenuItem("useQHarassHP", "Minimum HP% to use Q").SetValue(new Slider(60, 1)));

                var killsteal = config.AddSubMenu(new Menu("KillSteal Settings", "KillSteal"));
                killsteal.AddItem(new MenuItem("killsteal", "Activate KillSteal").SetValue(true));
                killsteal.AddItem(new MenuItem("useQks", "Use Q to KillSteal").SetValue(true));

                var misc = config.AddSubMenu(new Menu("Misc Settings", "Misc"));
/*
                var miscQ = misc.AddSubMenu(new Menu("Q Settings", "Q"));
                miscQ.AddItem(
                    new MenuItem("autoQ", "Auto Q on enemies").SetValue(
                        new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle)));
                miscQ.AddItem(new MenuItem("autoQhp", "Minimum HP% to auto Q").SetValue(new Slider(50, 1)));
                miscQ.AddItem(
                    new MenuItem("hitchanceQ", "Global Q Hitchance").SetValue(
                        new StringList(
                            new[]
                            {
                                HitChance.Low.ToString(), HitChance.Medium.ToString(), HitChance.High.ToString(),
                                HitChance.VeryHigh.ToString()
                            }, 3)));
*/
                var miscW = misc.AddSubMenu(new Menu("W Settings", "W"));
                miscW.AddItem(new MenuItem("handleW", "Automatically handle W").SetValue(true));

                var miscR = misc.AddSubMenu(new Menu("R Settings", "R"));
                miscR.AddItem(new MenuItem("useR", "Use R").SetValue(true));
                miscR.AddItem(new MenuItem("RHealth", "Minimum HP% to use R").SetValue(new Slider(20, 1)));
                miscR.AddItem(new MenuItem("RHealthEnemies", "If enemies nearby").SetValue(true));

                var lasthit = config.AddSubMenu(new Menu("Last Hit Settings", "LastHit"));
                lasthit.AddItem(new MenuItem("useQlh", "Use Q to last hit minions").SetValue(true));
                lasthit.AddItem(new MenuItem("useQlhHP", "Minimum HP% to use Q to lasthit").SetValue(new Slider(50, 1)));
                lasthit.AddItem(new MenuItem("qRange", "Only use Q if far from minions").SetValue(true));

                var clear = config.AddSubMenu(new Menu("Clear Settings", "Clear"));
                clear.AddItem(new MenuItem("spacerLC", "-- Lane Clear --"));
                clear.AddItem(new MenuItem("useQlc", "Use Q to last hit in laneclear").SetValue(true));
                clear.AddItem(new MenuItem("useQlcHP", "Minimum HP% to use Q to laneclear").SetValue(new Slider(40, 1)));
                clear.AddItem(new MenuItem("useWlc", "Use W in laneclear").SetValue(true));
                clear.AddItem(new MenuItem("useWlcHP", "Minimum HP% to use W to laneclear").SetValue(new Slider(40, 1)));
                clear.AddItem(new MenuItem("useWlcMinions", "Minimum minions to W in laneclear").SetValue(new Slider(3, 1, 10)));
                clear.AddItem(new MenuItem("spacerJC", "-- Jungle Clear --"));
                clear.AddItem(new MenuItem("useQj", "Use Q to jungle").SetValue(true));
                clear.AddItem(new MenuItem("useQjHP", "Minimum HP% to use Q in jungle").SetValue(new Slider(20, 1)));
                clear.AddItem(new MenuItem("useWj", "Use W to jungle").SetValue(true));
                clear.AddItem(new MenuItem("useWjHP", "Minimum HP% to use W to jungle").SetValue(new Slider(20, 1)));
                clear.AddItem(new MenuItem("useEj", "Use E to jungle").SetValue(true));

                var drawingMenu = config.AddSubMenu(new Menu("Drawings", "Drawings"));
                drawingMenu.AddItem(new MenuItem("disableDraw", "Disable all drawings").SetValue(true));
                drawingMenu.AddItem(new MenuItem("drawQ", "Q range").SetValue(new Circle(false, System.Drawing.Color.DarkOrange, q.Range)));
                drawingMenu.AddItem(new MenuItem("drawW", "W range").SetValue(new Circle(false, System.Drawing.Color.DarkOrange, w.Range)));
                drawingMenu.AddItem(new MenuItem("width", "Drawings width").SetValue(new Slider(2, 1, 5)));
                drawingMenu.AddItem(new MenuItem("drawAutoQ", "Draw AutoQ status").SetValue(false));

                config.AddToMainMenu();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Could not load the menu - {0}", exception);
            }
        }
        #endregion

        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            // 일단 Mixed Mode 일때는 사용하지 않음
            if ((orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.Combo /*|| orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.Mixed*/) && unit.IsMe)
            {
                if (config.Item("useE").GetValue<bool>() && e.IsReady() && target is Obj_AI_Hero && target.IsValidTarget(e.Range))
                {
                    e.Cast();
                }
            }

            if (orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.LaneClear && unit.IsMe)
            {
                if (config.Item("useEj").GetValue<bool>() && e.IsReady() && target is Obj_AI_Minion && target.IsValidTarget(e.Range))
                {
                    e.Cast();
                }

            }
        }

        private static void OnDraw(EventArgs args)
        {
            // 추후 정비 필요, Q 사거리만 표시하는게...
            if (Player.IsDead || config.Item("disableDraw").GetValue<bool>())
                return;

            var heroPosition = Drawing.WorldToScreen(Player.Position);
            var textDimension = Drawing.GetTextExtent("AutoQ: ON");
            var drawQstatus = config.Item("drawAutoQ").GetValue<bool>();
            var width = config.Item("width").GetValue<Slider>().Value;

            if (config.Item("drawQ").GetValue<Circle>().Active)
            {
                var circle = config.Item("drawQ").GetValue<Circle>();
                Render.Circle.DrawCircle(Player.Position, circle.Radius, circle.Color, width);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            switch (orbwalker.ActiveMode)
            {
                case SebbyLib.Orbwalking.OrbwalkingMode.Combo:
                    ExecuteCombo();
                    break;

                case SebbyLib.Orbwalking.OrbwalkingMode.Mixed:
                    LastHit();
                    ExecuteHarass();
                    break;

                case SebbyLib.Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    break;

                case SebbyLib.Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case SebbyLib.Orbwalking.OrbwalkingMode.None:
                    BurningManager();
                    break;
            }

            AutoR();
            KillSteal();
        }

        private static void ExecuteCombo()
        {
            var target = TargetSelector.GetTarget(q.Range, TargetSelector.DamageType.Magical);

            if (target == null || !target.IsValid)
                return;

            var castQ = config.Item("useQ").GetValue<bool>() && q.IsReady();
            var castW = config.Item("useW").GetValue<bool>() && w.IsReady();

            var qHealth = config.Item("QHealthCombo").GetValue<Slider>().Value;
            var wHealth = config.Item("WHealthCombo").GetValue<Slider>().Value;

            if (castQ && Player.HealthPercent >= qHealth && target.IsValidTarget(q.Range))
            {
                // OKTW Prediction
                var qpred = q.GetPrediction(target);
                if (qpred.CollisionObjects.Count(h => h.IsEnemy && !h.IsDead && h is Obj_AI_Minion) < 1)
                    Entry.OKTWCast_SebbyLib(q, target, false);
            }

            if (castW && Player.HealthPercent >= wHealth && !IsBurning() && target.IsValidTarget(w.Range+50))
            {
                w.Cast();
            }
            else if (castW && IsBurning() && !FoundEnemies(w.Range + 100))
            {
                w.Cast();
            }
        }

        private static void ExecuteHarass()
        {
            var target = TargetSelector.GetTarget(q.Range, TargetSelector.DamageType.Magical);

            if (target == null || !target.IsValid)
                return;

            var castQ = config.Item("useQHarass").GetValue<bool>() && q.IsReady();

            var qHealth = config.Item("useQHarassHP").GetValue<Slider>().Value;

            if (castQ && Player.HealthPercent >= qHealth && target.IsValidTarget(q.Range))
            {
                q.CastIfHitchanceEquals(target, HitChance.VeryHigh);
            }
        }

        private static void LastHit()
        {
            var castQ = config.Item("useQlh").GetValue<bool>() && q.IsReady();

            var qHealth = config.Item("useQlhHP").GetValue<Slider>().Value;

            if (!SebbyLib.Orbwalking.CanMove(40))
                return;

            var minions = MinionManager.GetMinions(Player.ServerPosition, q.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (minions.Count > 0 && castQ && Player.HealthPercent >= qHealth)
            {
                foreach (var minion in minions)
                {
                    if (config.Item("qRange").GetValue<bool>())
                    {
                        if (HealthPrediction.GetHealthPrediction(minion, (int) (q.Delay + (minion.Distance(Player.Position)/q.Speed))) < Player.GetSpellDamage(minion, SpellSlot.Q) && Player.Distance(minion) > Player.AttackRange*2)
                        {
                            q.Cast(minion);
                        }
                    }
                    else
                    {
                        if (HealthPrediction.GetHealthPrediction(minion, (int) (q.Delay + (minion.Distance(Player.Position)/q.Speed))) < Player.GetSpellDamage(minion, SpellSlot.Q))
                        {
                            q.Cast(minion);
                        }
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var castQ = config.Item("useQlc").GetValue<bool>() && q.IsReady();
            var castW = config.Item("useWlc").GetValue<bool>() && w.IsReady();

            var qHealth = config.Item("useQlcHP").GetValue<Slider>().Value;
            var wHealth = config.Item("useWlcHP").GetValue<Slider>().Value;
            var wMinions = config.Item("useWlcMinions").GetValue<Slider>().Value;

            if (!SebbyLib.Orbwalking.CanMove(40))
                return;

            var minions = MinionManager.GetMinions(Player.ServerPosition, q.Range);
            var minionsW = MinionManager.GetMinions(Player.ServerPosition, 400);

            if (minions.Count > 0)
            {
                if (castQ && Player.HealthPercent >= qHealth)
                {
                    foreach (var minion in minions)
                    {
                        if (HealthPrediction.GetHealthPrediction(minion, (int) (q.Delay + (minion.Distance(Player.Position)/q.Speed))) < Player.GetSpellDamage(minion, SpellSlot.Q))
                        {
                            q.Cast(minion);
                        }
                    }
                }
            }

            if (minionsW.Count >= wMinions)
            {
                if (castW && Player.HealthPercent >= wHealth && !IsBurning())
                {
                    w.Cast();
                }
                else if (castW && IsBurning() && minions.Count < wMinions)
                {
                    w.Cast();
                }
            }
        }

        private static void JungleClear()
        {
            var castQ = config.Item("useQj").GetValue<bool>() && q.IsReady();
            var castW = config.Item("useWj").GetValue<bool>() && w.IsReady();

            var qHealth = config.Item("useQjHP").GetValue<Slider>().Value;
            var wHealth = config.Item("useWjHP").GetValue<Slider>().Value;

            if (!SebbyLib.Orbwalking.CanMove(40))
                return;

            var minions = MinionManager.GetMinions(Player.ServerPosition, q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var minionsW = MinionManager.GetMinions(Player.ServerPosition, 400, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (minions.Count > 0)
            {
                var minion = minions[0];

                if (castQ && Player.HealthPercent >= qHealth)
                {
                    q.Cast(minion);
                }
            }

            if (minionsW.Count > 0)
            {
                if (castW && Player.HealthPercent >= wHealth && !IsBurning())
                {
                    w.Cast();
                }
                else if (castW && IsBurning() && minionsW.Count < 1)
                {
                    w.Cast();
                }
            }
            
        }

        private static void KillSteal()
        {
            if (!config.Item("killsteal").GetValue<bool>())
                return;

            if (config.Item("useQks").GetValue<bool>() && q.IsReady())
            {
                foreach (var target in HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(q.Range) && !enemy.HasBuffOfType(BuffType.Invulnerability)).Where(target => target.Health < Player.GetSpellDamage(target, SpellSlot.Q)))
                {
                    q.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                }
            }

        }


        private static void AutoR()
        {
            if (Player.IsDead)
                return;

            var castR = config.Item("useR").GetValue<bool>() && r.IsReady();

            var rHealth = config.Item("RHealth").GetValue<Slider>().Value;
            var rEnemies = config.Item("RHealthEnemies").GetValue<bool>();

            if (rEnemies && castR && Player.HealthPercent <= rHealth && !Player.InFountain())
            {
                if (FoundEnemies(q.Range))
                {
                    r.Cast();
                }
            }
            else if (!rEnemies && castR && Player.HealthPercent <= rHealth && !Player.InFountain())
            {
                r.Cast();
            }
        }

        private static bool IsBurning()
        {
            return Player.HasBuff("BurningAgony");
        }

        private static bool FoundEnemies(float range)
        {
            return HeroManager.Enemies.Any(enemy => enemy.IsValidTarget(range));
        }

        private static void BurningManager()
        {
            if (!config.Item("handleW").GetValue<bool>())
                return;
            
            if (IsBurning() && w.IsReady())
            {
                w.Cast();
            }
        }
    }
}