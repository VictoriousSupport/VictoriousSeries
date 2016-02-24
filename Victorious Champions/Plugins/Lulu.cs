/// <summary>
///  [Jinx's Support: Lulu]
///  
///     [2016.02.09]
///         1. 기본 하레스는 E+평타로 진행 (C Key)
///         2. 원거리 견제가 필요할 경우 E+Q 콤보 사용 (V Key)
///         3. 이동속도 버프(W 사거리 이하에 적이 없을때) or HP 20% 이하일때 궁극기를 나에게 시전 (T Key)
///         4. E Shield 는 Activator의 것을 사용 - 기본적으로 공격에 우선 사용함.
///         5. Q 로직 개선 - OKTW Prediction 사용
///         6. W 변이 우선 진행 + 타게팅 가능 변경 - 아군 속도 버프는 메뉴얼로 작업
///         7. EQ 콤보 로직 신규 함수 작업
///         8. 체력비례 궁극기 제거 (필요시 Activator 사용할 것)
///         9. Pix 드로잉 추가
///         10. Q2 변수 제거
///         
///         
///        
/// </summary>

namespace JinxsSupport.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using LeagueSharp;
    using LeagueSharp.Common;
    using LeagueSharp.Common.Data;
    using ItemData = LeagueSharp.Common.Data.ItemData;
    using SharpDX;
    using Color = System.Drawing.Color;

    public class Lulu : IPlugin
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell _q, _w, _e, _r;
        private static Menu _menu;
        private static Obj_AI_Base pix;

        private static bool g_bIniEShieldMenu = false;      // Eshield Menu Configure Flag
        private static bool g_bPassiveModeE = false;        // Lulu Passive Mode
        //private static int g_nDamagePercent;                // Dagame % for Defence
        private static int g_nDamageCritical;               // Critical Damage
        private static int g_nMyADCDamagePercent;           // My ADC Damage % for Defence
        private static string g_strMyADC = null;

        private static string[] Spells =
        {
            "incinerate",                   // 애니 W
            "infiniteduress",               // 워윅 R
            "velkozr",                      // 벨코즈 R
            "crowstorm",                    // 피들 R
            "ezrealtrueshotbarrage",        // 이즈리얼 R
            "luxmalicecannon",              // 럭스 R
            "missfortunebullettime",        // 미포 R
            "caitlynaceinthehole",          // 케이틀린 R
            "brandwildfire",                // 브랜드 R
            "monkeykingspintowin",          // 오공 R
            "garenr",                       // 가렌 R
            "goldcardpreattack",            // 트페 골드카드
            "nocturneUnspeakablehorror",    // 녹턴 E
            "katarinar",                    // 카타리나 R
            "skarnerimpale",                // 스카너 R
            "bustershot",                   // 트타 R
            "trundlePain",                  // 트런들 R
            "vir",                          // 바이 R
            "ZedR",                         // 제드 R
            "Terrify",                      // 피들스틱 Q
            "BlindMonkRKick",               // 리신 R
            "LissandraR",                   // 리산드라 R
            "MordekaiserChildrenOfTheGrave",// 모데카이저 R
            "InfernalGuardian",             // 애니 R
            "BraumR",                       // 브라움 R
            "FizzMarinerDoomMissile",       // 피즈 R
            "UFSlash",                      // 말파이트 R
            "AlZaharNetherGrasp",           // 말자하 R
            "NamiR",                        // 나미 R
            "NautilusGrandLine"             // 노틸러스 R
        };

        #region Load() Function
        public void Load()
        {
            if (Player.ChampionName != "Lulu")
                return;

            //Spells
            _q = new Spell(SpellSlot.Q,925);
            _q.MinHitChance = HitChance.High;
            _w = new Spell(SpellSlot.W,650);
            _e = new Spell(SpellSlot.E,650);
            _r = new Spell(SpellSlot.R,900);
            _q.SetSkillshot(0.25f, 70, 1450, false, SkillshotType.SkillshotLine);

            //print chat as game loaded
            Entry.PrintChat("<font color=\"#FF8844\" >Lulu</font>");
            Entry.PrintChat("Actviator >> Auto Spells >> Config & Help Pix!!");
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            //Menu instance
            _menu = new Menu("Victorious Lulu", Player.ChampionName, true);
            //Orbwalker
            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            //Targetsleector
            _menu.AddSubMenu(orbwalkerMenu);
            Menu ts = _menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);
            
            //spell menu
            Menu spellMenu = _menu.AddSubMenu(new Menu("Spells", "Spells"));
            
            //harass
            Menu Harass = spellMenu.AddSubMenu(new Menu("Harass", "Harass"));
            Harass.AddItem(new MenuItem("QH", "Q (C Key)").SetValue(false));
            Harass.AddItem(new MenuItem("EH", "E (C Key)").SetValue(true));
            Harass.AddItem(new MenuItem("QEH", "E+Q (V Key)").SetValue(true));
            Harass.AddItem(new MenuItem("ManaH", "Min. Mana").SetValue(new Slider(40, 0, 100)));
            
            //combo 
            Menu Combo = spellMenu.AddSubMenu(new Menu("Combo", "Combo"));
            Combo.AddItem(new MenuItem("QC", "Q").SetValue(true));
            Combo.AddItem(new MenuItem("EC", "E").SetValue(true));
            Combo.AddItem(new MenuItem("QEC", "E+Q").SetValue(true));
            Combo.AddItem(new MenuItem("RC", "R").SetValue(true));
            Combo.AddItem(new MenuItem("RHC", "R if will hit").SetValue(new Slider(3, 1, 5)));

            //Misc
            Menu Auto = spellMenu.AddSubMenu(new Menu("Misc", "Misc"));
            Auto.AddItem(new MenuItem("QA", "Ks Q").SetValue(false));
            Auto.AddItem(new MenuItem("EA", "Ks E").SetValue(false));
            Auto.AddItem(new MenuItem("WG", "W Anti GapCloser").SetValue(true));
            Auto.AddItem(new MenuItem("WI", "W Interrupt").SetValue(true));
            Auto.AddItem(new MenuItem("RI", "R Interrupt").SetValue(false));

            Auto.AddItem(new MenuItem("SafeKey", "Safety Key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            //Drawing
            Menu Draw = _menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Draw.AddItem(new MenuItem("DQ", "Draw Q").SetValue(false));
            Draw.AddItem(new MenuItem("DW", "Draw W").SetValue(false));
            Draw.AddItem(new MenuItem("DE", "Draw E").SetValue(true));
            Draw.AddItem(new MenuItem("DR", "Draw R").SetValue(false));
            Draw.AddItem(new MenuItem("DEQ", "Draw E + Q").SetValue(false));
            Draw.AddItem(new MenuItem("DPIX", "Draw Pix").SetValue(true));
            //Draw.AddItem(new MenuItem("DStatus", "Draw Status").SetValue(true));

            // W:Polymorph (적군 원딜/정글)
            Menu Wcombo = _menu.AddSubMenu(new Menu("Polymorph | W", "W"));
            foreach (var hero in HeroManager.Enemies)
                Wcombo.AddItem(new MenuItem("WC" + hero.ChampionName, hero.ChampionName).SetValue(true));

            // Passive Mode (자기방어 모드/모든 E Shield를 나에게만 사용하는 모드)
            Menu PassiveMode = _menu.AddSubMenu(new Menu("Passive Mode | E", "PassiveModeE"));

            PassiveMode.AddItem(new MenuItem("Victoious.Lulu.PassviceMode.Enable", "Self-Defence Enable").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle, false)));
            PassiveMode.AddItem(new MenuItem("Victoious.Lulu.PassviceMode.Critical", "Damage Threshold HP(%)").SetValue(new Slider(10, 0, 30)));
            //PassiveMode.AddItem(new MenuItem("Victoious.Lulu.PassviceMode.DamageP", "Damage > My HP(%)").SetValue(new Slider(4, 0, 30)));

            var MyADCMenu = new Menu("My ADC (Select One)", "SelectMyADC");
            {
                MyADCMenu.AddItem(new MenuItem("Victoious.Lulu.PassviceMode.DamageADC", "Damage > Ally HP(%)").SetValue(new Slider(8, 0, 30)));
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe))
                {
                    var championName = ally.CharData.BaseSkinName;
                    MyADCMenu.AddItem(new MenuItem("HelpPix" + championName, "Help Pix:" + championName).SetValue(false));
                }
            }
            PassiveMode.AddSubMenu(MyADCMenu);

            var MyEnemySpellBook = new Menu("Enemy Spell Book", "EnemySpellBook");
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
            {
                var spellQ = enemy.Spellbook.Spells[0];
                var spellW = enemy.Spellbook.Spells[1];
                var spellE = enemy.Spellbook.Spells[2];
                var spellR = enemy.Spellbook.Spells[3];

                MyEnemySpellBook.SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellQ.SData.Name, string.Format("Q: {0} ({1})", spellQ.Name, spellQ.SData.TargettingType)));
                MyEnemySpellBook.SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellW.SData.Name, string.Format("W: {0} ({1})", spellW.Name, spellW.SData.TargettingType)));
                MyEnemySpellBook.SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellE.SData.Name, string.Format("E: {0} ({1})", spellE.Name, spellE.SData.TargettingType)));
                MyEnemySpellBook.SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellR.SData.Name, string.Format("R: {0} ({1})", spellR.Name, spellR.SData.TargettingType)));
            }
            PassiveMode.AddSubMenu(MyEnemySpellBook);

            //Attach to root
            _menu.AddToMainMenu();

            g_bIniEShieldMenu = true;

            //Listen to events
            Game.OnUpdate += Game_OnGameUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;   // 6.3.2.7
        }
        #endregion

        // 말도 많고 탈도 많은 Spell E Passvie Mode (FPS Drop)
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            bool bCriticalSpell = false;
            if ( !sender.IsEnemy || sender.IsMinion || !sender.IsValid<Obj_AI_Hero>() || Player.Distance(sender.ServerPosition) > 1800) return;

            var foundSpell = Spells.Find(x => args.SData.Name.ToLower() == x.ToLower());
            if (foundSpell != null)
            {
                Entry.PrintChat(string.Format("<font color=\"#FFAA00\" >Critical Skill ({0})!!!!!!</font>", args.SData.Name));
                bCriticalSpell = true;
            }
               
            if (args.Target.IsMe)
            {
                var dmg = sender.GetSpellDamage(Player, args.SData.Name);     // 예상 데미지
                double HpPercentage = (dmg * 100) / Player.MaxHealth;         // 예상 데미지% (전체HP)

                Entry.PrintChat(string.Format("{0}({1}): {2:F2}/{3}({4})", args.SData.Name, args.SData.TargettingType, HpPercentage, g_nDamageCritical, _e.IsReady()));

                if (!g_bPassiveModeE || !_e.IsReady()) return;

                // PassiveMode가 활성화 되어 있을때
                if(bCriticalSpell)
                {
                    if (args.Target != null && args.Target.NetworkId == Player.NetworkId)
                    {
                        Entry.PrintChat(string.Format("Help Pix Lulu! Targeted Critical = {0}({1:F2})", args.SData.Name, HpPercentage));
                        _e.CastOnUnit(Player);
                    }
                    else
                    {
                        var castArea = Player.Distance(args.End) * (args.End - Player.ServerPosition).Normalized() + Player.ServerPosition;
                        if (castArea.Distance(Player.ServerPosition) <= _e.Range/2) 
                        {
                            Entry.PrintChat(string.Format("Help Pix Lulu! Location Critical = {0}({1:F2})", args.SData.Name, HpPercentage));
                            _e.CastOnUnit(Player);
                        }
                    }
                }
                else if (HpPercentage > g_nDamageCritical)
                {
                    Entry.PrintChat(string.Format("Help Pix Lulu! High Damage = {0:F2} > {1} / {2}", HpPercentage, g_nDamageCritical, args.SData.Name));
                    _e.CastOnUnit(Player);
                }

            }
            else if (args.Target.IsAlly && args.Target.IsValid<Obj_AI_Hero>() && !string.IsNullOrEmpty(g_strMyADC))         // My ADC만 보호함
            {
                var MyADC = HeroManager.Allies.Where(ally => ally.IsValid && Player.Distance(ally.ServerPosition) < _e.Range).Find(ally => ally.CharData.BaseSkinName == g_strMyADC);

                var dmg = sender.GetSpellDamage(MyADC, args.SData.Name);     // 예상 데미지
                double HpPercentage = (dmg * 100) / MyADC.MaxHealth;         // 예상 데미지% (전체HP)

                Entry.PrintChat(string.Format("{0}: {1:F2}/{2}({3})", MyADC.CharData.BaseSkinName, HpPercentage, g_nMyADCDamagePercent, _e.IsReady()));

                if (!g_bPassiveModeE || !_e.IsReady()) return;

                if (MyADC != null && MyADC.NetworkId == args.Target.NetworkId)
                {
                    if (bCriticalSpell)
                    {
                        Entry.PrintChat(string.Format("Help Pix {0}! Targeted Critical = {1}({2:F2})", MyADC.CharData.BaseSkinName, args.SData.Name, HpPercentage));
                        _e.CastOnUnit(MyADC);
                    }
                    else if (HpPercentage > g_nMyADCDamagePercent)
                    {
                        Entry.PrintChat(string.Format("Help Pix {0}! = {1:F2} > {2} / {3}", MyADC.CharData.BaseSkinName, HpPercentage, g_nMyADCDamagePercent, args.SData.Name));
                        _e.CastOnUnit(MyADC);
                    }
                }
                else
                {
                    if (bCriticalSpell)
                    {
                        var castArea = Player.Distance(args.End) * (args.End - Player.ServerPosition).Normalized() + Player.ServerPosition;
                        if (castArea.Distance(Player.ServerPosition) <= Player.BoundingRadius / 2)
                        {
                            Entry.PrintChat(string.Format("Help Pix {0}! Location Critical = {1}({2:F2})", MyADC.CharData.BaseSkinName, args.SData.Name, HpPercentage));
                            _e.CastOnUnit(Player);
                        }
                    }
                }


            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            // use W against gap closer
            var target = gapcloser.Sender;
            if (_w.IsReady() && target.IsValidTarget(_w.Range) && _menu.Item("WG").GetValue<bool>())
            {
                _w.CastOnUnit(target);
            }
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            // interrupt with W
            if (_w.IsReady() && sender.IsValidTarget(_w.Range) && !sender.IsZombie && _menu.Item("WI").GetValue<bool>())
            {
                _w.CastOnUnit(sender);
            }
            // interrupt with R
            if (_r.IsReady() && sender.IsValidTarget() && !sender.IsZombie && _menu.Item("RI").GetValue<bool>())
            {
                var target = HeroManager.Allies.Where(x => x.IsValidTarget(_r.Range, false)).OrderByDescending(x => 1 - x.Distance(sender.Position))
                    .Find(x => x.Distance(sender.Position) <= 350);
                if (target != null)
                    _r.CastOnUnit(target);
            }
        }

        public static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            if (_menu.Item("DQ").GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, _q.Range, Color.Aqua, 3);
            if (_menu.Item("DW").GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, _w.Range, Color.Purple);
            if (_menu.Item("DE").GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, _e.Range, Color.White, 3);
            if (_menu.Item("DR").GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, _r.Range, Color.Violet);
            if (_menu.Item("DEQ").GetValue<bool>())
                Render.Circle.DrawCircle(Player.Position, _q.Range + _e.Range, Color.YellowGreen);
            if (_menu.Item("DPIX").GetValue<bool>() && pix != null) 
                Render.Circle.DrawCircle(pix.Position + new Vector3(0, 0, 15), 75, Color.Yellow, 5, true);
            if(g_bPassiveModeE)
            {
                var playerPos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(playerPos.X-30, playerPos.Y - 115, Color.AliceBlue, "(E)Passive");
            }
        }

        public static void Game_OnGameUpdate (EventArgs args)
        {
            Getpixed();             // set value for pix
            Auto();                 // Kill Steal 용도
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();           // E 견제용! (C Key: E + 평타 사용)
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                Harass2();           // E+Q 견제용! (V Key: EQ 사용)

            if (_menu.Item("SafeKey", true).GetValue<KeyBind>().Active) SafetyKey();

        }

        /// <summary>
        /// OKTW Q 함수
        /// </summary>
        public static bool QCastOKTW(Obj_AI_Hero target, OKTWPrediction.HitChance hitChance)
        {
            var spell = _q;
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

            // 단일 타겟일때는 VeryHigh, 다중 타겟을때는 High 기준으로 기술 시전
            if (poutput2.Hitchance >= OKTWPrediction.HitChance.VeryHigh)
            {
                return spell.Cast(poutput2.CastPosition);
            }
            return false;
        }

        /// <summary>
        /// 자동 수행 함수
        ///  - 킬스틸 함수는 좀 보완이 필요할 듯
        ///  
        /// </summary>
        public static void Auto()
        {
            #region AutoFunction-KillSteal

            // 모든 콤보에서 가장 우선적으로 수행되는 구문... Kill Still 수행
            #region Q KS: 실제 사용되지 않음... 
            if (_q.IsReady() && _menu.Item("QA").GetValue<bool>())
            {
                foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget() && _q.GetDamage(x) >= x.Health 
                    && (x.Distance(Player.Position) > x.Distance(pix.Position) ? 925 >= x.Distance(pix.Position): 925 >= x.Distance(Player.Position))
                    ))
                    {
                        QCastOKTW(hero, OKTWPrediction.HitChance.VeryHigh);
                    }
            }
            #endregion

            #region  E KS: 실제 사용되지 않음...
            if (_e.IsReady() && _menu.Item("EA").GetValue<bool>())
            {
                foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(_e.Range) && _e.GetDamage(x) >= x.Health))
                {
                    if (_e.CanCast(hero) && !g_bPassiveModeE) _e.CastOnUnit(hero);
                }
            }
            #endregion

            #endregion
        }

        public static void PixEQCombo(bool useE = true)
        {
            Obj_AI_Base pixTarget = null;
            if (pix != null)
            {
                pixTarget = TargetSelector.GetTarget(pix, _q.Range, TargetSelector.DamageType.Magical);
            }

            Obj_AI_Base luluTarget = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);

            var pixTargetEffectiveHealth = pixTarget != null ? pixTarget.Health * (1 + pixTarget.SpellBlock / 100f) : float.MaxValue;
            var luluTargetEffectiveHealth = luluTarget != null ? luluTarget.Health * (1 + luluTarget.SpellBlock / 100f) : float.MaxValue;
            var target = pixTargetEffectiveHealth * 1.2f > luluTargetEffectiveHealth ? luluTarget : pixTarget;          // HP를 가지고 타겟을 정하나?

            var bflag = false;
            Spell.CastStates qCastState = Spell.CastStates.OutOfRange;

            // 일단 Q 사거리내에 있는 적챔피언을 검색한 후...
            if (target != null)
            {
                var distanceToTargetFromPlayer = Player.Distance(target, true);
                var distanceToTargetFromPix = pix != null ? pix.Distance(target, true) : float.MaxValue;
                var source = pix == null ? Player : (distanceToTargetFromPix < distanceToTargetFromPlayer ? pix : Player);      // Pix 타겟에 더 가까우면

                _q.From = source.ServerPosition;
                _q.RangeCheckFrom = source.ServerPosition;

                // E가 준비되지 않거나, 사거리내 있으면... 일단 시전
                if (!useE || !_e.IsReady() || _q.From.Distance(target.ServerPosition) < _q.Range - 100)
                {
                    qCastState = _q.Cast(target);
                }
                bflag = true;
            }

            // 일단 했으나, 결과가 사거리 밖이라는 표식이 뜨면... 혹은 타겟이 Null 이면
            if (qCastState == Spell.CastStates.OutOfRange)
            {
                if (useE && _e.IsReady())
                {
                    var eqTarget = TargetSelector.GetTarget(_q.Range + _e.Range, TargetSelector.DamageType.Magical);
                    if (eqTarget != null)
                    {
                        // E Range 안에 있는 적에게 일단 E Cast
                        var eTarget =
                            ObjectManager.Get<Obj_AI_Base>()
                                .Where(t => t.IsValidTarget(_e.Range) && t.Distance(eqTarget, true) < _q.RangeSqr && !_e.IsKillable(eqTarget)).MinOrDefault(t => t.Distance(eqTarget, true));
                        if (eTarget != null && _q.IsReady())
                        {
                            _e.Cast(eTarget);
                            return;
                        }
                    }
                }

                if (bflag)
                {
                    qCastState = _q.Cast(target);
                }
            }

        }

        public static void SafetyKey()
        {

            if (_w.IsReady() && Player.CountEnemiesInRange(_e.Range) < 1 && !Player.IsDead)
            {
                _w.CastOnUnit(Player);
                return;
            }

            if (_r.IsReady() && !Player.IsDead && Player.HealthPercent < 30 && Player.CountEnemiesInRange(_r.Range) > 0)
            {
                _r.CastOnUnit(Player);
                return;
            }

        }

        public static void Combo()
        {

            #region Cast: W
            // W 스킬은 메뉴에서 지정이 되어 있어야 하고, 또한 타게팅이 되어 있어야 함.
            // 타게팅은 기본설정을 따라가게 되고, 미리 클릭해서 설정해놓으면 됨. (정글링 대응)
            if (_w.IsReady())
            {
                var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Magical);
                foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(_w.Range) && _menu.Item("WC" + x.ChampionName).GetValue<bool>()))
                {
                    if ((target == hero) && _w.CanCast(hero))
                    {
                        _w.CastOnUnit(hero);
                    }
                }
            }
            #endregion

            // cast Q
            if (_q.IsReady() && _menu.Item("QC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                if (target != null && target.IsValidTarget())
                {
                    QCastOKTW(target, OKTWPrediction.HitChance.VeryHigh);
                }
            }

            // cast E
            if (_e.IsReady() && _menu.Item("EC").GetValue<bool>() && !g_bPassiveModeE) 
            {
                var target = TargetSelector.GetTarget(_e.Range, TargetSelector.DamageType.Magical);
                if (target != null && _e.CanCast(target))
                {
                    _e.CastOnUnit(target);
                }
            }

            //cast R if will hit 
            if (_r.IsReady() && _menu.Item("RC").GetValue<bool>())
            {
                foreach (var hero in HeroManager.Allies.Where(x => x.IsValidTarget(_r.Range,false)))
                {
                    if (hero.CountEnemiesInRange(350) >= _menu.Item("RHC").GetValue<Slider>().Value)
                        _r.CastOnUnit(hero);
                }
            }
            // QE combo 분리필요
            if (_q.IsReady() && _e.IsReady() && Player.Mana >= _q.Instance.ManaCost + _e.Instance.ManaCost && _menu.Item("QEC").GetValue<bool>() && _menu.Item("EC").GetValue<bool>() && !g_bPassiveModeE)
            {
                PixEQCombo();
            }
        }

        public static void Harass2()
        {
            if((Player.Mana * 100 / Player.MaxMana >= _menu.Item("ManaH").GetValue<Slider>().Value) && _menu.Item("QEH").GetValue<bool>() && !g_bPassiveModeE)
            {
                PixEQCombo();
            }
        }

        public static void Harass()
        {
            if (Player.Mana * 100 / Player.MaxMana >= _menu.Item("ManaH").GetValue<Slider>().Value)
            {
                // cast Q
                if (_q.IsReady() && _menu.Item("QH").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
                    if (target != null && target.IsValidTarget())
                        QCastOKTW(target, OKTWPrediction.HitChance.High);
                }

                // Cast E
                if (_e.IsReady() && _menu.Item("EH").GetValue<bool>())
                {
                    var target = TargetSelector.GetTarget(_e.Range, TargetSelector.DamageType.Magical);
                    if (target != null && _e.CanCast(target) && !g_bPassiveModeE)
                    {
                        _e.CastOnUnit(target);
                    }
                        
                }
            }
        }
        // get Pix!
        public static void Getpixed()
        {
            if(g_bIniEShieldMenu)
            {
                g_bPassiveModeE = _menu.Item("Victoious.Lulu.PassviceMode.Enable").GetValue<KeyBind>().Active;
                //g_nDamagePercent = _menu.Item("Victoious.Lulu.PassviceMode.DamageP").GetValue<Slider>().Value;
                g_nDamageCritical = _menu.Item("Victoious.Lulu.PassviceMode.Critical").GetValue<Slider>().Value;
                g_nMyADCDamagePercent = _menu.Item("Victoious.Lulu.PassviceMode.DamageADC").GetValue<Slider>().Value;
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe))
                {
                    var championName = ally.CharData.BaseSkinName;
                    if (_menu.Item("HelpPix" + championName).GetValue<bool>())
                    {
                        g_strMyADC = championName;
                        break;
                    }
                    else g_strMyADC = null;
                }
            }

            if (Player.IsDead)
                pix = Player;
            if (!Player.IsDead)
                pix = ObjectManager.Get<Obj_AI_Base>().Find(x => x.IsAlly && x.Name == "RobotBuddy") == null ? 
                    Player : ObjectManager.Get<Obj_AI_Base>().Find(x => x.IsAlly && x.Name == "RobotBuddy");
        }

        // undertower from BrianSharp
        private static bool UnderTower(Vector3 pos)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>()
                    .Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) < 850 + Player.BoundingRadius);
        }

        public static bool IsActive(Menu Menu, string item)
        {
            return Menu.Item(item, true).GetValue<KeyBind>().Active;
        }
    }
}
