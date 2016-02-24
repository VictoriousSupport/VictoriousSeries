namespace JinxsSupport.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Drawing;
    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;
    using Color = System.Drawing.Color;
    using ItemData = LeagueSharp.Common.Data.ItemData;

    internal class Tristana : IPlugin
    {
        #region Static Fields & Properties

        public static Menu Menu;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>()
                                                             {
                                                                 { Spells.Q, new Spell(SpellSlot.Q, 550) },
                                                                 { Spells.W, new Spell(SpellSlot.W, 900) },
                                                                 { Spells.E, new Spell(SpellSlot.E, 625) },
                                                                 { Spells.R, new Spell(SpellSlot.R, 700) },
                                                             };

        private static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        /// <summary>
        ///     Gets or sets the random.
        /// </summary>
        /// <value>
        ///     The random.
        /// </value>
        private static Random Random { get; set; }
        private static Vector3? LastHarassPos { get; set; }

        #endregion

        #region Load() Function
        public void Load()
        {
            try
            {
                spells[Spells.W].SetSkillshot(0.35f, 250f, 1400f, false, SkillshotType.SkillshotCircle);
                Entry.PrintChat("<font color=\"#66CCFF\" >Tristana</font>");
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            Menu = new Menu("Vicroious Tristana", "menu", true).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);

            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            Menu.AddSubMenu(orbwalkerMenu);

            var targetSelector = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelector);

            Menu.AddSubMenu(targetSelector);

            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.AddItem(new MenuItem("ElTristana.Combo.Q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElTristana.Combo.E", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElTristana.Combo.R", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElTristana.Combo.Focus.E", "Focus E target").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElTristana.Combo.Always.RE", "Use E + R finisher").SetValue(true));

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
                comboMenu.SubMenu("E Combo Enable")
                    .AddItem(
                        new MenuItem("ElTristana.E.On" + hero.ChampionName, hero.ChampionName)
                            .SetValue(true));

            Menu.AddSubMenu(comboMenu);

#if false
            var suicideMenu = new Menu("W settings", "Suicide menu").SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
            suicideMenu.AddItem(new MenuItem("ElTristana.W", "Use this special feature").SetValue(false));
            suicideMenu.AddItem(new MenuItem("ElTristana.W.Jump.kill", "Only jump when killable").SetValue(false));
            suicideMenu.AddItem(new MenuItem("ElTristana.W.Jump.tower", "Check under tower").SetValue(true));
            suicideMenu.AddItem(new MenuItem("ElTristana.W.Jump", "W to enemy with 4 stacks").SetValue(false));
            suicideMenu.AddItem(new MenuItem("ElTristana.W.Enemies", "Only jump when enemies in range")).SetValue(new Slider(1, 1, 5));
            suicideMenu.AddItem(new MenuItem("ElTristana.W.Enemies.Range", "Enemies in range distance check")).SetValue(new Slider(1500, 800, 2000));

            Menu.AddSubMenu(suicideMenu);
#endif

            var harassMenu = new Menu("Harass", "Harass");
            harassMenu.AddItem(new MenuItem("ElTristana.Harass.Q", "Use Q").SetValue(false));
            harassMenu.AddItem(new MenuItem("ElTristana.Harass.E", "Use E").SetValue(true));
            harassMenu.AddItem(new MenuItem("ElTristana.Harass.QE", "Use Q only with E").SetValue(false));
            harassMenu.AddItem(new MenuItem("ElTristana.Harass.E.Mana", "Minimum mana for E")).SetValue(new Slider(50));

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
                harassMenu.SubMenu("E Harass Enable")
                    .AddItem(
                        new MenuItem("ElTristana.E.On.Harass" + hero.CharData.BaseSkinName, hero.CharData.BaseSkinName)
                            .SetValue(true));

            Menu.AddSubMenu(harassMenu);


            var laneClearMenu = new Menu("Laneclear", "Laneclear");
            laneClearMenu.AddItem(new MenuItem("ElTristana.LaneClear.Q", "Use Q").SetValue(false));
            laneClearMenu.AddItem(new MenuItem("ElTristana.LaneClear.E", "Use E").SetValue(true));
            //laneClearMenu.AddItem(new MenuItem("ElTristana.LaneClear.Tower", "Use E on tower").SetValue(false));
            laneClearMenu.AddItem(new MenuItem("ElTristana.LaneClear.E.Mana", "Minimum mana for E")).SetValue(new Slider(30));

            Menu.AddSubMenu(laneClearMenu);

            var jungleClearMenu = new Menu("Jungleclear", "Jungleclear");
            jungleClearMenu.AddItem(new MenuItem("ElTristana.JungleClear.Q", "Use Q").SetValue(true));
            jungleClearMenu.AddItem(new MenuItem("ElTristana.JungleClear.E", "Use E").SetValue(true));
            jungleClearMenu.AddItem(new MenuItem("ElTristana.JungleClear.E.Mana", "Minimum mana for E")).SetValue(new Slider(20));

            Menu.AddSubMenu(jungleClearMenu);

            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.AddItem(new MenuItem("ElTristana.DrawStacks", "Draw E stacks").SetValue(true));
            miscMenu.AddItem(new MenuItem("ElTristana.Draw.Q", "Draw AA").SetValue(true));
            miscMenu.AddItem(new MenuItem("ElTristana.Draw.E", "Draw E").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElTristana.Draw.R", "Draw R").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElTristana.Antigapcloser", "Antigapcloser").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElTristana.Interrupter", "Interrupter").SetValue(false));

            var dmgAfterE = new MenuItem("ElTristana.DrawComboDamage", "Draw combo damage").SetValue(true);
            var drawFill =
                new MenuItem("ElTristana.DrawColour", "Fill colour", true).SetValue(new Circle(true, Color.Goldenrod));
            miscMenu.AddItem(drawFill);
            miscMenu.AddItem(dmgAfterE);

            DamageIndicator.DamageToUnit = GetComboDamage;
            DamageIndicator.Enabled = dmgAfterE.GetValue<bool>();
            DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
            DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;

            dmgAfterE.ValueChanged +=
                delegate (object sender, OnValueChangeEventArgs eventArgs)
                {
                    DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };

            drawFill.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
            {
                DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
            };

            Menu.AddSubMenu(miscMenu);


            Menu.AddToMainMenu();

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;

        }
#endregion


#region Methods

        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                if (!args.Unit.IsMe)
                {
                    return;
                }
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                {
                    if (!(args.Target is Obj_AI_Hero))
                    {
                        return;
                    }

                    // E Bomb 올라간 타겟으로 강제 변경
                    var target = HeroManager.Enemies.Find(x => x.HasBuff("TristanaECharge") && x.IsValidTarget(spells[Spells.E].Range));
                    if (target == null)
                    {
                        return;
                    }
                    if (Orbwalking.InAutoAttackRange(target))
                    {
                        Orbwalker.ForceTarget(target);
                    }
                }

                // V/X 키 누르고 있을때는 E Bomb이 올라간 미니언으로 강제 타겟 변경
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    var minion = args.Target as Obj_AI_Minion;
                    if (minion != null && minion.HasBuff("TristanaECharge"))
                    {
                        Orbwalker.ForceTarget(minion);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (IsActive("ElTristana.Antigapcloser"))
            {
                // 실제 R 사거리 보다는 상당히 짧네...
                if (gapcloser.Sender.IsValidTarget(250f) && spells[Spells.R].IsReady())
                {
                    spells[Spells.R].Cast(gapcloser.Sender);
                }
            }
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender,Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!IsActive("ElTristana.Interrupter"))
            {
                return;
            }

            if (sender.Distance(Player) <= spells[Spells.R].Range)
            {
                spells[Spells.R].Cast(sender);
            }
        }

        private static bool IsActive(string menuItem)
        {
            return Menu.Item(menuItem).GetValue<bool>();
        }

        private static bool IsECharged(Obj_AI_Base target)
        {
            return target.Buffs.Find(x => x.DisplayName == "TristanaECharge") != null;
        }

        private static void OnCombo()
        {
#region Combo Target Selection 가급적 E가 올려 있는 녀석을 선택함.
            var eTarget =
                HeroManager.Enemies.Find(x => x.HasBuff("TristanaECharge") && x.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)));
            var target = eTarget ?? TargetSelector.GetTarget(spells[Spells.E].Range, TargetSelector.DamageType.Physical);                 // x ?? y: x가 null인 경우 y로 계산하고, 그렇지 않으면 x로 계산합니다.

            if (!target.IsValidTarget())
            {
                return;
            }

            if (IsActive("ElTristana.Combo.Focus.E"))
            {
                // ElTristana.Combo.Focus.E
                var passiveTarget = HeroManager.Enemies.Find(x => x.HasBuff("TristanaECharge") && x.IsValidTarget(spells[Spells.E].Range));
                if (passiveTarget != null)
                {
                    Orbwalker.ForceTarget(passiveTarget);
                }
                else
                {
                    Orbwalker.ForceTarget(null);
                }
            }
#endregion

#region Combo Cast E Logic
            if (spells[Spells.E].IsReady() && IsActive("ElTristana.Combo.E"))
            {
                foreach (var hero in HeroManager.Enemies.OrderByDescending(x => x.Health))      
                {
                    // 범위내 E Cast Enblbe 된 적군중 HP가 가장 낮은 녀석을 찾음
                    if (hero.IsEnemy)
                    {
                        var getEnemies = Menu.Item("ElTristana.E.On" + hero.ChampionName);
                        if (getEnemies != null && getEnemies.GetValue<bool>())
                        {
                            spells[Spells.E].Cast(hero);
                            Orbwalker.ForceTarget(hero);
                        }

                        // 이건 뭐야? 만약 Enbale Off 되어 있어도, 주변 1500 반경내 적이 1명(독고다이)이면 E Cast
                        if (getEnemies != null && !getEnemies.GetValue<bool>() && Player.CountEnemiesInRange(1500) == 1)
                        {
                            spells[Spells.E].Cast(hero);
                            Orbwalker.ForceTarget(hero);
                        }
                    }
                }
            }
#endregion

            //UseItems(target);

#region Combo Cast R Logic
            if (spells[Spells.R].IsReady() && IsActive("ElTristana.Combo.R"))
            {
                // R 용도:  Kill Steal
                if (spells[Spells.R].GetDamage(target) > target.Health)
                {
                    spells[Spells.R].Cast(target);
                }
            }

            if (IsECharged(target) && IsActive("ElTristana.Combo.Always.RE"))
            {
                // RE 콤보가 활성화 되어 있고, R 데미지 + E 누적스택 데미지를 합한 결과가 Kill 이 될때.... 즉 R만으로는 못죽이는데 E가 올려 있는 녀석이면... 혹시 시전 가능
                if (spells[Spells.R].GetDamage(target) + spells[Spells.E].GetDamage(target) * ((0.3 * target.GetBuffCount("TristanaECharge") + 1))> target.Health)
                {
                    spells[Spells.R].Cast(target);
                }
            }
#endregion

#region Combo Cast Q Logic
            // 일단 E 사거리내 적이 있으면 무조건 Q 발동
            if (spells[Spells.Q].IsReady() && IsActive("ElTristana.Combo.Q") && target.IsValidTarget(spells[Spells.E].Range))
            {
                spells[Spells.Q].Cast();
            }
#endregion
        }

        private static bool HasEBuff(Obj_AI_Base target)
        {
            return target.HasBuff("TristanaECharge");           // 이건 E Full Charge를 의미하는 것 같음.
        }

        private static int GetEStacks(Obj_AI_Base target)
        {
            return HasEBuff(target) ? 3 : target.GetBuffCount("TristanaECharge");
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            if (Menu.Item("ElTristana.Draw.Q").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.Q].Range + ObjectManager.Player.BoundingRadius, Color.White, 1);
                //Render.Circle.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius, System.Drawing.Color.White, 1);
            }

            if (Menu.Item("ElTristana.Draw.E").GetValue<bool>())
            {
                if (spells[Spells.E].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.E].Range + ObjectManager.Player.BoundingRadius, Color.White);
            }

            if (Menu.Item("ElTristana.Draw.R").GetValue<bool>())
            {
                if (spells[Spells.R].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spells[Spells.R].Range, Color.White);
            }

            var target = HeroManager.Enemies.Find(x => x.HasBuff("TristanaECharge") && x.IsValidTarget(2000));
            // E Charge 적이 없으면 무조건 리턴됨?
            if (!target.IsValidTarget()) return;

#region Draw E Statkcs
            if (IsActive("ElTristana.DrawStacks"))
            {
                if (LastHarassPos == null)
                {
                    LastHarassPos = Player.ServerPosition;
                }

                var x = target.HPBarPosition.X + 45;
                var y = target.HPBarPosition.Y - 25;

                if (spells[Spells.E].Level > 0)
                {
                    if (HasEBuff(target)) //Credits to lizzaran 
                    {
                        int stacks = target.GetBuffCount("TristanaECharge");
                        if (stacks > -1)
                        {
                            for (var i = 0; 4 > i; i++)
                                Drawing.DrawLine( x + i * 20, y, x + i * 20 + 10, y, 10, i > stacks ? Color.DarkGray : Color.DeepSkyBlue);
                        }
#if false
                        if (stacks == 3)    // Max
                        {
                            if (IsActive("ElTristana.W"))
                            {
                                if (IsActive("ElTristana.W.Jump"))
                                {
                                    if (IsActive("ElTristana.W.Jump.kill"))
                                    {
                                        if (target.IsValidTarget(spells[Spells.W].Range) && Player.CountEnemiesInRange(Menu.Item("ElTristana.W.Enemies.Range").GetValue<Slider>().Value) <= Menu.Item("ElTristana.W.Enemies").GetValue<Slider>().Value)
                                        {
                                            if (IsActive("ElTristana.W.Jump.tower"))
                                            {
                                                if (target.UnderTurret()) return;
                                            }

                                            if (spells[Spells.W].GetDamage(target) > target.Health + 15)
                                                spells[Spells.W].Cast(target.ServerPosition);
                                        }
                                    }
                                    else
                                    {
                                        if (target.IsValidTarget(spells[Spells.W].Range) && Player.CountEnemiesInRange(Menu.Item("ElTristana.W.Enemies.Range").GetValue<Slider>().Value) <= Menu.Item("ElTristana.W.Enemies").GetValue<Slider>().Value)
                                        {
                                            if (IsActive("ElTristana.W.Jump.tower"))
                                            {
                                                bool underTower = target.UnderTurret();
                                                if (underTower)
                                                {
                                                    return;
                                                }
                                            }
                                            spells[Spells.W].Cast(target.ServerPosition);
                                        }
                                    }
                                }
                            }
                        }
#endif
                    }
                }
            }
#endregion

        }

        public static bool OKTWCanMove(Obj_AI_Hero target)
        {
            if (target.MoveSpeed < 50 || target.IsStunned || target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) || (target.IsChannelingImportantSpell() && !target.IsMoving))
            {
                return false;
            }
            else
                return true;
        }

        private static void OnHarass()      // Mixed Key (C)
        {
            var target = TargetSelector.GetTarget(spells[Spells.E].Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (spells[Spells.E].IsReady() && IsActive("ElTristana.Harass.E") && Player.ManaPercent > Menu.Item("ElTristana.Harass.E.Mana").GetValue<Slider>().Value)
            {
                foreach (var hero in HeroManager.Enemies.OrderByDescending(x => x.Health))  // HP 낮은 순서부터 Loop
                {
                    if (hero.IsEnemy)
                    {
                        var getEnemies = Menu.Item("ElTristana.E.On.Harass" + hero.CharData.BaseSkinName);
                        if (getEnemies != null && getEnemies.GetValue<bool>() && !OKTWCanMove(hero))    // 아군 서폿이 CC 걸어준 경우에만 Harass... 아니면 말고...
                        {
                            spells[Spells.E].Cast(hero);
                            Orbwalker.ForceTarget(hero);
                        }
                    }
                }
            }

            if (spells[Spells.Q].IsReady() && IsActive("ElTristana.Harass.Q") && target.IsValidTarget(spells[Spells.E].Range))
            {
                if (IsECharged(target) && IsActive("ElTristana.Harass.QE"))
                {
                    spells[Spells.Q].Cast();
                }
                else if (!IsActive("ElTristana.Harass.QE"))
                {
                    spells[Spells.Q].Cast();
                }
            }
        }

        private static void OnJungleClear()
        {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spells[Spells.E].Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            /*
            MinionManager.GetMinions(
                    spells[Spells.E].Range,
                    MinionTypes.All,
                    MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth).FirstOrDefault();
*/
            if (minions.Count > 0)
            {
                var mob = minions[0];
                if (spells[Spells.E].IsReady() && IsActive("ElTristana.JungleClear.E")
                    && Player.ManaPercent > Menu.Item("ElTristana.JungleClear.E.Mana").GetValue<Slider>().Value)
                {
                    spells[Spells.E].CastOnUnit(mob);
                }

                if (spells[Spells.Q].IsReady() && IsActive("ElTristana.JungleClear.Q"))
                {
                    spells[Spells.Q].Cast();
                }

            }
        }

        private static void OnLaneClear()
        {
#if false
            if (IsActive("ElTristana.LaneClear.Tower"))
            {
                foreach (var tower in ObjectManager.Get<Obj_AI_Turret>())
                {
                    if (!tower.IsDead && tower.Health > 100 && tower.IsEnemy && tower.IsValidTarget()
                        && Player.ServerPosition.Distance(tower.ServerPosition)
                        < Orbwalking.GetRealAutoAttackRange(Player))
                    {
                        spells[Spells.E].Cast(tower);
                    }
                }
        }
#endif
            var minions = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition,
                spells[Spells.E].Range,
                MinionTypes.All,
                MinionTeam.NotAlly,
                MinionOrderTypes.MaxHealth);

            if (minions.Count <= 0)
            {
                return;
            }
            /*
                var minions = MinionManager.GetMinions(player.Position, Q.Range, MinionTypes.All);
                if (minions == null || minions.Count == 0)
                    return;

                // Q 사거리내 미니언이 11마리 이상 있고, AA+W Damage로 죽일 수 있는 미니언이 2마리 이상 있을때 기술 발동
                if ((minions.Count > 10))
                {
                    Entry.PrintChat("Case W: LaneClearMode");
                    W.Cast();
                    Orbwalking.ResetAutoAttackTimer();
                }
    */
            if (spells[Spells.E].IsReady() && IsActive("ElTristana.LaneClear.E") && minions.Count > 10
                && Player.ManaPercent > Menu.Item("ElTristana.LaneClear.E.Mana").GetValue<Slider>().Value)
            {
                foreach (var minion in
                    ObjectManager.Get<Obj_AI_Minion>().OrderByDescending(m => m.Health))
                {
                    spells[Spells.E].Cast(minion);
                    Orbwalker.ForceTarget(minion);
                }
            }

            var eminion =
                minions.Find(x => x.HasBuff("TristanaECharge") && x.IsValidTarget(1000));

            if (eminion != null)
            {
                Orbwalker.ForceTarget(eminion);
            }

            if (spells[Spells.Q].IsReady() && IsActive("ElTristana.LaneClear.Q"))
            {
                var eMob = minions.FindAll(x => x.IsValidTarget() && x.HasBuff("TristanaECharge")).FirstOrDefault();
                if (eMob != null)
                {
                    Orbwalker.ForceTarget(eMob);
                    spells[Spells.Q].Cast();
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            try
            {
                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        OnCombo();
                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        OnHarass();
                        OnLaneClear();
                        OnJungleClear();
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        OnHarass();
                        break;
                }

                spells[Spells.Q].Range = 550 + 7 * (Player.Level - 1);          // Q 사거리가 기본 사거리, 사거리 증가 7
                spells[Spells.E].Range = 625 + 7 * (Player.Level - 1);
                spells[Spells.R].Range = 517 + 7 * (Player.Level - 1);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        public static float GetComboDamage(Obj_AI_Base enemy)
        {
            float damage = 0;

            if (!Player.IsWindingUp)
            {
                damage += (float)ObjectManager.Player.GetAutoAttackDamage(enemy, true);
            }

            if (enemy.HasBuff("tristanaecharge"))
            {
                damage += (float)(spells[Spells.E].GetDamage(enemy) * (enemy.GetBuffCount("tristanaecharge") * 0.30)) +
                          spells[Spells.E].GetDamage(enemy);
            }

            if (spells[Spells.R].IsReady())
            {
                damage += spells[Spells.R].GetDamage(enemy);
            }

            return damage;
        }

#endregion
    }

    internal class DamageIndicator
    {
        public delegate float DamageToUnitDelegate(Obj_AI_Hero hero);

        private const int XOffset = 10;
        private const int YOffset = 20;
        private const int Width = 103;
        private const int Height = 8;

        public static Color Color = Color.Lime;
        public static Color FillColor = Color.Goldenrod;
        public static bool Fill = true;

        public static bool Enabled = true;
        private static DamageToUnitDelegate _damageToUnit;

        private static readonly Render.Text Text = new Render.Text(0, 0, "", 14, SharpDX.Color.Red, "monospace");

        public static DamageToUnitDelegate DamageToUnit
        {
            get { return _damageToUnit; }

            set
            {
                if (_damageToUnit == null)
                {
                    Drawing.OnDraw += Drawing_OnDraw;
                }
                _damageToUnit = value;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Enabled || _damageToUnit == null)
            {
                return;
            }

            foreach (var unit in HeroManager.Enemies.Where(h => h.IsValid && h.IsHPBarRendered))
            {
                var damage = _damageToUnit(unit);

                if (damage > 2)
                {
                    var barPos = unit.HPBarPosition;

                    var percentHealthAfterDamage = Math.Max(0, unit.Health - damage) / unit.MaxHealth;
                    var yPos = barPos.Y + YOffset;
                    var xPosDamage = barPos.X + XOffset + Width * percentHealthAfterDamage;
                    var xPosCurrentHp = barPos.X + XOffset + Width * unit.Health / unit.MaxHealth;

                    if (damage > unit.Health)
                    {
                        Text.X = (int)barPos.X + XOffset;
                        Text.Y = (int)barPos.Y + YOffset - 13;
                        Text.text = "KILLABLE: " + (unit.Health - damage);
                        Text.OnEndScene();
                    }

                    Drawing.DrawLine(xPosDamage, yPos, xPosDamage, yPos + Height, 1, Color);

                    if (Fill)
                    {
                        var differenceInHp = xPosCurrentHp - xPosDamage;
                        var pos1 = barPos.X + 9 + 107 * percentHealthAfterDamage;

                        for (var i = 0; i < differenceInHp; i++)
                        {
                            Drawing.DrawLine(pos1 + i, yPos, pos1 + i, yPos + Height, 1, FillColor);
                        }
                    }
                }
            }

        }
    }
}
 