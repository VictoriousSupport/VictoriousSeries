/// <summary>
///  [Jinx's Support: Alistar]
///  
///     [2016.02.07]
///         1. 메인콤보의 WQ 콤보 로직 튜닝
///            - 튜닝불가: 현상태 유지
///            - W 최대 사거리 근처에서 시전하면 성공확률이 높음
///         2. QW 콤보 로직 추가
///            - 지정한 사거리(320) 이내 적이 있을 경우, 일단 띄우고 지정된 딜레이(400) 이후 W를 시전
///            - QW 콤보는 즉발 시전이기 때문에, 이동할 여유가 없음. 즉, 방향을 먼저 잡고 시전해야 함.
///        
/// </summary>


namespace JinxsSupport.Plugins
{
    using System;
    using System.Drawing;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    internal class Alistar : IPlugin
    {
        #region Properties

        /// <summary>
        ///     Gets or sets the E spell
        /// </summary>
        /// <value>
        ///     The E spell
        /// </value>
        private static Spell E { get; set; }

        /// <summary>
        ///     Gets or sets the menu
        /// </summary>
        /// <value>
        ///     The menu
        /// </value>
        private static Menu Menu { get; set; }

        /// <summary>
        ///     Gets or sets the orbwalker
        /// </summary>
        /// <value>
        ///     The orbwalker
        /// </value>
        private static Orbwalking.Orbwalker Orbwalker { get; set; }

        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        private static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        /// <summary>
        ///     Gets or sets the Q spell
        /// </summary>
        /// <value>
        ///     The Q spell
        /// </value>
        private static Spell Q { get; set; }

        /// <summary>
        ///     Gets or sets the R spell.
        /// </summary>
        /// <value>
        ///     The R spell
        /// </value>
        private static Spell R { get; set; }

        /// <summary>
        ///     Gets or sets the W spell
        /// </summary>
        /// <value>
        ///     The W spell
        /// </value>
        private static Spell W { get; set; }

        /// <summary>
        ///     Gets or sets the slot.
        /// </summary>
        /// <value>
        ///     The IgniteSpell
        /// </value>
        public static Spell IgniteSpell { get; set; }

        /// <summary>
        ///     FlashSlot
        /// </summary>
        public static SpellSlot FlashSlot;


        #endregion

        #region Public Methods and Operators

        #region Load() Function
        public void Load()
        {
            try
            {
                if (Player.ChampionName != "Alistar")
                {
                    return;
                }

                var igniteSlot = Player.GetSpell(SpellSlot.Summoner1).Name.ToLower().Contains("summonerdot")
                                    ? SpellSlot.Summoner1
                                    : Player.GetSpell(SpellSlot.Summoner2).Name.ToLower().Contains("summonerdot")
                                          ? SpellSlot.Summoner2
                                          : SpellSlot.Unknown;

                if (igniteSlot != SpellSlot.Unknown)
                {
                    IgniteSpell = new Spell(igniteSlot, 600f);
                }

                FlashSlot = Player.GetSpellSlot("summonerflash");

                Q = new Spell(SpellSlot.Q, 365f);
                W = new Spell(SpellSlot.W, 650f);
                E = new Spell(SpellSlot.E, 575f);
                R = new Spell(SpellSlot.R);

                Game.OnUpdate += OnUpdate;
                Drawing.OnDraw += OnDraw;
                AttackableUnit.OnDamage += AttackableUnit_OnDamage;
                Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
                AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;

                Entry.PrintChat("<font color=\"#FFCC66\" >Alistar</font>");
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
                Menu = new Menu("Victorious Alistar", "ElAlistar", true);

                var targetselectorMenu = new Menu("Target Selector", "Target Selector");
                {
                    TargetSelector.AddToMenu(targetselectorMenu);
                }

                Menu.AddSubMenu(targetselectorMenu);

                var orbwalkMenu = new Menu("Orbwalker", "Orbwalker");
                {
                    Orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);
                }

                Menu.AddSubMenu(orbwalkMenu);

                var comboMenu = new Menu("Combo Settings", "Combo");
                {
                    comboMenu.AddItem(new MenuItem("ElAlistar.Combo.Q", "Use Q").SetValue(true));
                    comboMenu.AddItem(new MenuItem("ElAlistar.Combo.W", "Use W").SetValue(true));
                    comboMenu.AddItem(new MenuItem("ElAlistar.Combo.R", "Use R").SetValue(true));
                    comboMenu.AddItem(new MenuItem("ElAlistar.Combo.RHeal.HP", "R on My Health %").SetValue(new Slider(50, 1)));
                    comboMenu.AddItem(new MenuItem("ElAlistar.Combo.RHeal.Damage", "R on Incomming Damage %").SetValue(new Slider(40, 1)));
                }

                Menu.AddSubMenu(comboMenu);

                var flashMenu = new Menu("QW Combo Auto Key", "Flash");
                {
                    flashMenu.AddItem(new MenuItem("ElAlistar.QW.Combo", "QW Custom Combo").SetValue(true));
                    flashMenu.AddItem(new MenuItem("ElAlistar.QW.Combo.Range", "QW Combo Start Range").SetValue(new Slider(320, 250, 350)));    // new by Jinx
                    flashMenu.AddItem(new MenuItem("ElAlistar.QW.Combo.Delay", "Q -> W Delay").SetValue(new Slider(400, 200, 800)));           // new by Jinx
                    flashMenu.AddItem(
                    new MenuItem("ElAlistar.Combo.QWkey", "QW HotKey").SetValue(
                        new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                }

                Menu.AddSubMenu(flashMenu);

                var healMenu = new Menu("Heal Settings", "Heal");
                {
                    healMenu.AddItem(new MenuItem("ElAlistar.Heal.E", "Use heal").SetValue(true));
                    healMenu.AddItem(new MenuItem("Heal.HP", "Health percentage").SetValue(new Slider(50, 1)));
                    healMenu.AddItem(new MenuItem("Heal.Damage", "Heal on damage dealt %").SetValue(new Slider(40, 1)));
                    healMenu.AddItem(
                            new MenuItem("ElAlistar.Heal.Mana", "Minimum mana").SetValue(
                                new Slider(20, 0, 100)));
                    healMenu.AddItem(new MenuItem("seperator21", ""));
                    foreach (var x in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly))
                    {
                        healMenu.AddItem(new MenuItem("healon" + x.ChampionName, "Cast E: " + x.ChampionName))
                            .SetValue(true);
                    }
                }

                Menu.AddSubMenu(healMenu);

                var interrupterMenu = new Menu("Interrupter Settings", "Interrupter");
                {
                    interrupterMenu.AddItem(new MenuItem("ElAlistar.Interrupter.Q", "Use Q").SetValue(true));
                    interrupterMenu.AddItem(new MenuItem("ElAlistar.Interrupter.W", "Use W").SetValue(true));
                    interrupterMenu.AddItem(new MenuItem("ElAlistar.GapCloser", "Anti gapcloser").SetValue(false)); // Anti GapCloser는 일단 수작업으로 진행
                }

                Menu.AddSubMenu(interrupterMenu);

                var miscellaneousMenu = new Menu("Miscellaneous", "Misc");
                {
                    //miscellaneousMenu.AddItem(new MenuItem("ElAlistar.Ignite", "Use Ignite").SetValue(false));
                    miscellaneousMenu.AddItem(new MenuItem("ElAlistar.Drawings.W", "Draw W range").SetValue(true));

                }

                Menu.AddSubMenu(miscellaneousMenu);
                Menu.AddToMainMenu();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
        #endregion

        #endregion

        #region Methods
        /// <summary>
        ///     Gets the active menu item
        /// </summary>
        /// <value>
        ///     The menu item
        /// </value>
        private static bool IsActive(string menuName)
        {
            return Menu.Item(menuName).IsActive();
        }
        /// <summary>
        ///     Called when the game draws itself.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (IsActive("ElAlistar.Drawings.W"))
                {
                    if (W.Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, Color.DeepSkyBlue, 3);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }


        /// <summary>
        ///     The ignite killsteal logic
        /// </summary>
        private static void HandleIgnite()
        {
            try
            {
                var kSableEnemy =
                    HeroManager.Enemies.FirstOrDefault(
                        hero =>
                        hero.IsValidTarget(550) && ShieldCheck(hero) && !hero.HasBuff("summonerdot") && !hero.IsZombie
                        && Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite) >= hero.Health);

                if (kSableEnemy != null && IgniteSpell.Slot != SpellSlot.Unknown)
                {
                    Player.Spellbook.CastSpell(IgniteSpell.Slot, kSableEnemy);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     The shield checker
        /// </summary>
        private static bool ShieldCheck(Obj_AI_Base hero)
        {
            try
            {
                return !hero.HasBuff("summonerbarrier") || !hero.HasBuff("BlackShield")
                       || !hero.HasBuff("SivirShield") || !hero.HasBuff("BansheesVeil")
                       || !hero.HasBuff("ShroudofDarkness");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return false;
        }


        /// <summary>
        ///     Returns the mana
        /// </summary>
        private static bool HasEnoughMana()
        {
            return Player.Mana
                   > Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost + Player.Spellbook.GetSpell(SpellSlot.W).ManaCost;
        }

        /// <summary>
        ///     Combo logic
        /// </summary>
        private static void OnCombo()
        {
            try
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (target == null)
                {
                    return;
                }

                // WQ 콤보
                if (IsActive("ElAlistar.Combo.Q") && IsActive("ElAlistar.Combo.W") && Q.IsReady() && W.IsReady())
                {
                    if (target.IsValidTarget(W.Range) && HasEnoughMana())
                    {
                        // 만약 Q 사거리 안에 있으면 그냥 Q 날리고 맘.
                        if (target.IsValidTarget(Q.Range))
                        {
                            Q.Cast();
                            return;
                        }

                        if (W.Cast(target).IsCasted())
                        {
                            /* 6.2 패치까지 쿵쾅콤보
                            var comboTime = Math.Max(0, Player.Distance(target) - 365) / 1.2f - 25;
                            Utility.DelayAction.Add((int)comboTime, () => Q.Cast());
                            */
                            /// 6.3 패치: 쿵쾅콤보 로직
                            if (Player.Distance(target) > 150)
                            {
                                Utility.DelayAction.Add(50, () => Q.Cast());
                                // 150의 거리를 1200의 투사체 속도로 날아가므로, 도달까지 약 125ms 필요, 네트워크 핑지연 포함하더라도 50ms 지연후 Q 발동하면 쿵쾅콤보 성공
                            }
                            else
                            {
                                if(Q.IsReady()) Q.Cast();
                                // 그 이하 거리면 그냥 바로 시전
                            }
                        }
                    }
                }

                if (IsActive("ElAlistar.Combo.Q") && target.IsValidTarget(Q.Range))
                {
                    Q.Cast();
                }

                if (IsActive("ElAlistar.Combo.W"))
                {
                    if (target.IsValidTarget(W.Range) && W.GetDamage(target) > target.Health)
                    {
                        W.Cast(target);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static void OnInterruptableTarget(
            Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (args.DangerLevel != Interrupter2.DangerLevel.High || sender.Distance(Player) > W.Range)
            {
                return;
            }

            if (sender.IsValidTarget(Q.Range) && Q.IsReady() && IsActive("ElAlistar.Interrupter.Q"))
            {
                Q.Cast();
            }

            if (sender.IsValidTarget(W.Range) && W.IsReady() && IsActive("ElAlistar.Interrupter.W"))
            {
                W.Cast(sender);
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (IsActive("ElAlistar.GapCloser"))
            {
                if (Q.IsReady()
                    && gapcloser.Sender.Distance(Player) < Q.Range)
                {
                    Q.Cast();
                }

                if (W.IsReady() && gapcloser.Sender.Distance(Player) < W.Range)
                {
                    W.Cast(gapcloser.Sender);
                }
            }
        }

        private static void AttackableUnit_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            var obj = ObjectManager.GetUnitByNetworkId<GameObject>(args.TargetNetworkId);

            if (obj.Type != GameObjectType.obj_AI_Hero)
            {
                return;
            }

            var hero = (Obj_AI_Hero)obj;

            if (hero.IsEnemy)
            {
                return;
            }

            // 공격 가능한 유닛으로 부터 데미지를 받을때... 이게 데미지의 단위가 있을텐데...  단위 시간당 데미지인지, 한번에 받은 데미지인지...
            if (Menu.Item("ElAlistar.Combo.R").IsActive())
            {
                if (ObjectManager.Get<Obj_AI_Hero>()
                        .Any(
                            x =>
                            x.IsAlly && x.IsMe && !x.IsDead && ((int)(args.Damage / x.MaxHealth * 100)
                                > Menu.Item("ElAlistar.Combo.RHeal.Damage").GetValue<Slider>().Value
                                || x.HealthPercent < Menu.Item("ElAlistar.Combo.RHeal.HP").GetValue<Slider>().Value && x.CountEnemiesInRange(600) >= 2)))
                {
                    R.Cast();
                }
            }

            if (Menu.Item("ElAlistar.Heal.E").IsActive() && Player.ManaPercent > Menu.Item("ElAlistar.Heal.Mana").GetValue<Slider>().Value)
            {
                if (
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Any(
                            x =>
                            x.IsAlly && !x.IsDead && Menu.Item(string.Format("healon{0}", x.ChampionName)).IsActive()
                            && ((int)(args.Damage / x.MaxHealth * 100)
                                > Menu.Item("Heal.Damage").GetValue<Slider>().Value
                                || x.HealthPercent < Menu.Item("Heal.HP").GetValue<Slider>().Value)
                            && x.Distance(Player) < E.Range && x.CountEnemiesInRange(1000) >= 1))
                {
                    E.Cast();
                }
            }
        }

        /// <summary>
        ///     Called when the game updates
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnUpdate(EventArgs args)
        {
            try
            {
                if (Player.IsDead || Player.IsRecalling() || Player.InFountain())
                {
                    return;
                }

                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        OnCombo();
                        break;
                }

                if (Menu.Item("ElAlistar.Combo.QWkey").GetValue<KeyBind>().Active)
                {
                    QWCombo();
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static void QWCombo()
        {

            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);  // 일단 커서 방향으로 움직임
            if (!Q.IsReady() && !W.IsReady()) return;

            // 단축키(T) 눌렸을때 QW 순차적 발동, Q가 발동되면 x msec 안에 이동한 반대방향으로 던짐
            // 적이 접근하면 띄워 던지는거보다 그냥 던지는게 빠르므로 그냥 두면 되고
            // 적에게 충분히 접근하여 띄운후 이동하여 던지는게 목표 (사실 손으로 하는게 더 좋지 않을까 계속 고민중임)
            // 항상 아군쪽으로 던져야 한다는 생각을 하기는 하는데... 그게 지금 기술로 코드상으로 구현하기 쉽지 않음.
            // 정상적인  Q사거리보다 짧게 설정함.
            var QWStartRange = Menu.Item("ElAlistar.QW.Combo.Range").GetValue<Slider>().Value;
            var target = TargetSelector.GetTarget(QWStartRange, TargetSelector.DamageType.Magical);
            if (target != null && W.IsReady() && Q.IsReady() && IsActive("ElAlistar.QW.Combo") && target.IsValidTarget(QWStartRange) && !target.IsDead && HasEnoughMana())
            {
                var QWStartDelay = Menu.Item("ElAlistar.QW.Combo.Delay").GetValue<Slider>().Value;
                Q.Cast();
                Utility.DelayAction.Add((int)QWStartDelay, () => W.Cast(target));
                // 먼저 Q를 시전하고, 지정된 시간 이후에 W를 타겟에 시전함.
            }
            else return;

        }

        #endregion
    }
}