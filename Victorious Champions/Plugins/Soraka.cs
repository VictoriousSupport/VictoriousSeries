/// <summary>
///  [Jinx's Support: Soraka]
///  
///     [2016.02.06]
///         1. Q 사거리 최신 패치 반영
///         2. Q 드로잉 조정(Q.Range+60)
///         3. E GapCloser 변경 
///            - CC기 있는 챔프가 접근할때 동작
///         4. Q Logic 변경
///            - Harass: Hitchance very high
///            - Combo: Hitchance high
///         5. R Logic 변경
///            - 설정값 이하, 근처 900거리 안에 적챔프가 있을 것
///            - ON 설정된 챔프만 동작
///            - 나한테는 사용하지 않음
///         6. W Logic 변경
///            - 귀환중(나/아군챔프) 에는 사용하지 않음.
///         
///     [2016.02.07]
///         1. Q 로직 변경: OKTW Very.High
///         2. E 로직 변경: OKTW Very.High
///         
///     [2016.02.08]
///         1. 미니언/챔피언 어택 금지 구문 삭제 (Main 통합)
///     
/// </summary>


namespace JinxsSupport.Plugins
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;

    using LeagueSharp;
    using LeagueSharp.Common;

    internal class Soraka : IPlugin
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the e.
        /// </summary>
        /// <value>
        ///     The e.
        /// </value>
        public static Spell E { get; set; }

        /// <summary>
        ///     Gets or sets the menu.
        /// </summary>
        /// <value>
        ///     The menu.
        /// </value>
        private static Menu Menu { get; set; }

        /// <summary>
        ///     Gets or sets the orbwalker.
        /// </summary>
        /// <value>
        ///     The orbwalker.
        /// </value>
        public static Orbwalking.Orbwalker Orbwalker { get; set; }

        /// <summary>
        ///     Gets a value indicating whether to use packets.
        /// </summary>
        public static bool Packets
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///     Gets or sets the q.
        /// </summary>
        /// <value>
        ///     The q.
        /// </value>
        public static Spell Q { get; set; }

        /// <summary>
        ///     Gets or sets the r.
        /// </summary>
        /// <value>
        ///     The r.
        /// </value>
        public static Spell R { get; set; }

        /// <summary>
        ///     Gets or sets the w.
        /// </summary>
        /// <value>
        ///     The w.
        /// </value>
        public static Spell W { get; set; }

        #endregion

        #region Load() Function
        public void Load()
        {
            if (ObjectManager.Player.ChampionName != "Soraka")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 800);
            W = new Spell(SpellSlot.W, 550);
            E = new Spell(SpellSlot.E, 925);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.3f, 160, 1600, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.5f, 70f, 1600, false, SkillshotType.SkillshotCircle);

            Interrupter2.OnInterruptableTarget += InterrupterOnOnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloserOnOnEnemyGapcloser;
            Game.OnUpdate += GameOnOnGameUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;


            Entry.PrintChat("<font color=\"#66CCFF\" >Soraka</font>");
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            Menu = new Menu("Victorious Soraka", "sSoraka", true);
            {
                // Target Selector
                var tsMenu = new Menu("Target Selector", "ssTS");
                TargetSelector.AddToMenu(tsMenu);
                Menu.AddSubMenu(tsMenu);

                // Orbwalking
                var orbwalkingMenu = new Menu("Orbwalking", "ssOrbwalking");
                Orbwalker = new Orbwalking.Orbwalker(orbwalkingMenu);
                Menu.AddSubMenu(orbwalkingMenu);

                // Combo
                var comboMenu = new Menu("Combo", "ssCombo");
                comboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
                Menu.AddSubMenu(comboMenu);

                // Harass
                var harassMenu = new Menu("Harass", "ssHarass");
                harassMenu.AddItem(new MenuItem("useQHarass", "Use Q").SetValue(true));
                harassMenu.AddItem(new MenuItem("useEHarass", "Use E").SetValue(false));
                Menu.AddSubMenu(harassMenu);

                // Healing
                var healingMenu = new Menu("Healing", "ssHeal");

                var wMenu = new Menu("W Settings", "WSettings");
                wMenu.AddItem(new MenuItem("autoW", "Use W").SetValue(true));
                wMenu.AddItem(new MenuItem("autoWPercent", "Ally Health Percent").SetValue(new Slider(64, 1)));
                wMenu.AddItem(new MenuItem("autoWHealth", "My Health Percent").SetValue(new Slider(20, 1)));
                wMenu.AddItem(new MenuItem("DontWInFountain", "Dont W in Fountain").SetValue(true));
                wMenu.AddItem(
                    new MenuItem("HealingPriority", "Healing Priority").SetValue(
                        new StringList(
                            new[] { "Most AD", "Most AP", "Least Health", "Least Health (Prioritize Squishies)" },
                            3)));
                healingMenu.AddSubMenu(wMenu);

                var rMenu = new Menu("R Settings", "RSettings");
                rMenu.AddItem(new MenuItem("autoR", "Use R").SetValue(true));
                rMenu.AddItem(new MenuItem("autoRPercent", "% Percent").SetValue(new Slider(18, 1)));
                healingMenu.AddSubMenu(rMenu);

                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe))
                {
                    var championName = ally.CharData.BaseSkinName;
                    healingMenu.AddItem(new MenuItem("HealR" + championName, "Cast R:" + championName).SetValue(true));
                }

                Menu.AddSubMenu(healingMenu);

                // Drawing
                var drawingMenu = new Menu("Drawing", "ssDrawing");
                drawingMenu.AddItem(new MenuItem("drawQ", "Draw Q").SetValue(true));
                drawingMenu.AddItem(new MenuItem("drawW", "Draw W").SetValue(false));
                drawingMenu.AddItem(new MenuItem("drawE", "Draw E").SetValue(false));
                Menu.AddSubMenu(drawingMenu);

                // Misc
                var miscMenu = new Menu("Misc", "ssMisc");
                miscMenu.AddItem(new MenuItem("useQGapcloser", "Q on Gapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("useEGapcloser", "E on Gapcloser").SetValue(true));
                miscMenu.AddItem(new MenuItem("eInterrupt", "Use E to Interrupt").SetValue(true));
                Menu.AddSubMenu(miscMenu);
            }

            Menu.AddToMainMenu();
        }
        #endregion

        #region Methods

        /// <summary>
        ///     The on enemy gapcloser event.
        /// </summary>
        /// <param name="gapcloser">
        ///     The gapcloser.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
            Justification = "Reviewed. Suppression is OK here.")]
        private static void AntiGapcloserOnOnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var unit = gapcloser.Sender;

            if (Menu.Item("useQGapcloser").GetValue<bool>() && unit.IsValidTarget(Q.Range) && Q.IsReady())
            {
                //Q.Cast(unit, Packets);
                QCastOKTW(unit, OKTWPrediction.HitChance.VeryHigh);
            }

            var cc = unit.HasBuffOfType(BuffType.Snare) ||
                     unit.HasBuffOfType(BuffType.Suppression) || unit.HasBuffOfType(BuffType.Taunt) ||
                     unit.HasBuffOfType(BuffType.Stun) || unit.HasBuffOfType(BuffType.Charm) ||
                     unit.HasBuffOfType(BuffType.Fear);

            if (Menu.Item("useEGapcloser").GetValue<bool>() && unit.IsValidTarget(E.Range) && E.IsReady() && cc)
            {
                ECastOKTW(unit, OKTWPrediction.HitChance.VeryHigh);
            }
        }

        /// <summary>
        ///     Automatics the ultimate.
        /// </summary>
        private static void AutoR()
        {
            if (!R.IsReady())
            {
                return;
            }
            /*
            if (
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(x => x.IsAlly && x.IsValidTarget(float.MaxValue, false) && !x.IsMe)  // by Jinx 나는 제외: && !x.IsMe  추가
                    .Select(x => (int)x.Health / x.MaxHealth * 100)
                    .Select(
                        friendHealth =>
                        new { friendHealth, health = Menu.Item("autoRPercent").GetValue<Slider>().Value })
                    .Where(x => x.friendHealth <= x.health)
                    .Select(x => x.friendHealth)
                    .Any())
            {
                R.Cast(Packets);
            }
            */

            var minAllyHealth = Menu.Item("autoRPercent").GetValue<Slider>().Value;
            if (minAllyHealth < 1) return;
            foreach (var ally in HeroManager.Allies)
            {
                if (ally.CountEnemiesInRange(950) >= 1 && ally.HealthPercent <= minAllyHealth && !ally.IsZombie && !ally.IsDead)
                {
                    if (Menu.Item("HealR" + ally.CharData.BaseSkinName) != null &&
                       Menu.Item("HealR" + ally.CharData.BaseSkinName).GetValue<bool>())
                    {
                        R.Cast();
                    }
                }
            }
        


    }

        /// <summary>
        ///     Automatics the W heal.
        /// </summary>

        private static void AutoW()
        {
             
            
            if (!W.IsReady())
            {
                return;
            }

            // 내 HP가 설정양 이하이면
            var autoWHealth = Menu.Item("autoWHealth").GetValue<Slider>().Value;
            if (ObjectManager.Player.HealthPercent < autoWHealth)
            {
                return;
            }

            // 내가 우물에 있거나 집에 가는 중이면
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            var healthPercent = Menu.Item("autoWPercent").GetValue<Slider>().Value;

            var canidates = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(W.Range, false) 
                                                                    && !x.IsMe
                                                                    && x.IsAlly 
                                                                    && x.HealthPercent < healthPercent
                                                                    && !x.IsRecalling()                                                  // by Jinx 아군이 집에 가고 있다면
                                                                    );      
            var wMode = Menu.Item("HealingPriority").GetValue<StringList>().SelectedValue;

            switch (wMode)
            {
                case "Most AD":
                    canidates = canidates.OrderByDescending(x => x.TotalAttackDamage);
                    break;
                case "Most AP":
                    canidates = canidates.OrderByDescending(x => x.TotalMagicalDamage);
                    break;
                case "Least Health":
                    canidates = canidates.OrderBy(x => x.Health);
                    break;
                case "Least Health (Prioritize Squishies)":
                    canidates = canidates.OrderBy(x => x.Health).ThenBy(x => x.MaxHealth);
                    
                    break;
            }

            // 내가 밖에 있고, 아군이 우물에 있는 경우는 제외
            var dontWInFountain = Menu.Item("DontWInFountain").GetValue<bool>();
            var target = dontWInFountain ? canidates.FirstOrDefault(x => !x.InFountain()) : canidates.FirstOrDefault();

            if (target != null)
            {
                    W.CastOnUnit(target);
         
            }
        }

        /// <summary>
        ///     The combo.
        /// </summary>
        private static void Combo()
        {
            var useQ = Menu.Item("useQ").GetValue<bool>();
            var useE = Menu.Item("useE").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
                /*
                if (Q.GetPrediction(target).Hitchance >= HitChance.High)
                {
                    Q.Cast(target, Packets);
                }
                */
                QCastOKTW(target, OKTWPrediction.HitChance.VeryHigh);

            }

            if (useE && E.IsReady())
            {
                //E.Cast(target, Packets);
                ECastOKTW(target, OKTWPrediction.HitChance.VeryHigh);
            }
        }

        /// <summary>
        ///     The on draw event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private static void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = Menu.Item("drawQ").GetValue<bool>();
            var drawW = Menu.Item("drawW").GetValue<bool>();
            var drawE = Menu.Item("drawE").GetValue<bool>();

            var p = ObjectManager.Player.Position;

            if (drawQ)
            {
                Render.Circle.DrawCircle(p, Q.Range+60, Q.IsReady() ? Color.White : Color.Red, 3);
            }

            if (drawW)
            {
                Render.Circle.DrawCircle(p, W.Range, W.IsReady() ? Color.White : Color.Red, 3);
            }

            if (drawE)
            {
                Render.Circle.DrawCircle(p, E.Range, E.IsReady() ? Color.White : Color.Red, 3);
            }
        }

        /// <summary>
        ///     The  on game update event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private static void GameOnOnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
            }

            if (Menu.Item("autoW").GetValue<bool>())
            {
                AutoW();
            }

            if (Menu.Item("autoR").GetValue<bool>())
            {
                AutoR();
            }
        }

        public static bool QCastOKTW(Obj_AI_Hero target, OKTWPrediction.HitChance hitChance)
        {
            var spell = Q;
            var OKTWPlayer = ObjectManager.Player;

            OKTWPrediction.SkillshotType CoreType2 = OKTWPrediction.SkillshotType.SkillshotCircle;
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

        public static bool ECastOKTW(Obj_AI_Hero target, OKTWPrediction.HitChance hitChance)
        {
            var spell = E;
            var OKTWPlayer = ObjectManager.Player;

            OKTWPrediction.SkillshotType CoreType2 = OKTWPrediction.SkillshotType.SkillshotCircle;
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

        /// <summary>
        ///     The harass.
        /// </summary>
        private static void Harass()
        {
            var useQ = Menu.Item("useQHarass").GetValue<bool>();
            var useE = Menu.Item("useEHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
                /*
                if (Q.GetPrediction(target).Hitchance > HitChance.High)
                {
                    Q.Cast(target, Packets);
                }
                */
                QCastOKTW(target, OKTWPrediction.HitChance.VeryHigh);

            }

            if (useE && E.IsReady())
            {
                //E.Cast(target, Packets);
                ECastOKTW(target, OKTWPrediction.HitChance.VeryHigh);
            }
        }

        /// <summary>
        ///     The on possible to interrupt event.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="args">
        ///     The args.
        /// </param>
        private static void InterrupterOnOnPossibleToInterrupt(
            Obj_AI_Hero sender, 
            Interrupter2.InterruptableTargetEventArgs args)
        {
            var unit = sender;
            var spell = args;

            if (Menu.Item("eInterrupt").GetValue<bool>() == false || spell.DangerLevel != Interrupter2.DangerLevel.High)
            {
                return;
            }

            if (!unit.IsValidTarget(E.Range))
            {
                return;
            }

            if (!E.IsReady())
            {
                return;
            }

            //E.Cast(unit, Packets);
            ECastOKTW(unit, OKTWPrediction.HitChance.VeryHigh);
        }


        #endregion
    }
}