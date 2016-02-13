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

            //Listen to events
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            //print chat as game loaded
            Entry.PrintChat("<font color=\"#FF8844\" >Lulu</font>");
            Entry.PrintChat("Actviator >> Auto Spells >> Config & Help Pix!!");
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            //Menu instance
            _menu = new Menu(Player.ChampionName, Player.ChampionName, true);
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
            Harass.AddItem(new MenuItem("ManaH", "Min Mana Harass").SetValue(new Slider(40, 0, 100)));
            
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
            Draw.AddItem(new MenuItem("DStatus", "Draw Status").SetValue(true));

            // W:Polymorph (적군 원딜/정글)
            Menu Wcombo = _menu.AddSubMenu(new Menu("Polymorph | W", "W"));
            Wcombo.AddItem(new MenuItem("WC", "[Enable]").SetValue(true));
            foreach (var hero in HeroManager.Enemies)
            {
                Wcombo.AddItem(new MenuItem("WC" + hero.ChampionName, hero.ChampionName).SetValue(true));
            }

            //Attach to root
            _menu.AddToMainMenu();
        }
        #endregion
         
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

        public static void Drawing_OnDraw (EventArgs args)
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
            if (_menu.Item("DPIX").GetValue<bool>())
                Render.Circle.DrawCircle(pix.Position + new Vector3(0, 0, 15), 75, Color.Yellow, 5, true);

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

            if (_menu.Item("SafeKey", true).GetValue<KeyBind>().Active)
            {
                SafetyKey();
            }

        }

        /// <summary>
        /// OKTW Q 함수, Q1/Q2 기술을 다르게 처리 
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

            if (poutput2.Hitchance >= hitChance)
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
                    if (_e.CanCast(hero)) _e.CastOnUnit(hero);
                }
            }
            #endregion

            #endregion

            #region Auto R Cast 구문: 단순 체력비례 용도로는 사용되지 않음. 만약 굳히 쓴다면 Activator 사용함.
            /*
            if (_r.IsReady() && _menu.Item("AR").GetValue<bool>())
            {
                foreach (var hero in HeroManager.Allies.Where(x => x.IsValidTarget(_r.Range, false) && _menu.Item("R" + x.ChampionName).GetValue<bool>()))
                {
                    if (hero.Health * 100 / hero.MaxHealth <=  _menu.Item("HPR").GetValue<Slider>().Value
                        && hero.CountEnemiesInRange(900) >= 1)
                        _r.Cast(hero);
                }
            }
            */
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
            
            var target = pixTargetEffectiveHealth * 1.2f > luluTargetEffectiveHealth ? luluTarget : pixTarget;
            var bflag = false;
            Spell.CastStates qCastState = Spell.CastStates.OutOfRange;

            // 일단 Q 사거리내에 있는 적챔피언을 검색한 후...
            if (target != null)
            {
                var distanceToTargetFromPlayer = Player.Distance(target, true);
                var distanceToTargetFromPix = pix != null ? pix.Distance(target, true) : float.MaxValue;
                var source = pix == null ? Player : (distanceToTargetFromPix < distanceToTargetFromPlayer ? pix : Player);

                _q.From = source.ServerPosition;
                _q.RangeCheckFrom = source.ServerPosition;

                if (!useE || !_e.IsReady() || _q.From.Distance(target.ServerPosition) < _q.Range - 100)
                {
                    qCastState = _q.Cast(target);
                }
                bflag = true;
            }

            // 만약 Q 사거리내에 적이 없으면 확장 사거리를 찾는다.
            if (qCastState == Spell.CastStates.OutOfRange)
            {
                if (useE && _e.IsReady())
                {
                    var eqTarget = TargetSelector.GetTarget(_q.Range + _e.Range, TargetSelector.DamageType.Magical);
                    if (eqTarget != null)
                    {
                        var eTarget =
                            ObjectManager.Get<Obj_AI_Base>()
                                .Where(t => t.IsValidTarget(_e.Range) && t.Distance(eqTarget, true) < _q.RangeSqr && !_e.IsKillable(eqTarget)).MinOrDefault(t => t.Distance(eqTarget, true));
                        if (eTarget != null)
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

            if (_r.IsReady() && !Player.IsDead && Player.HealthPercent < 20)
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
            if (_w.IsReady() && _menu.Item("WC").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Magical);
                foreach (var hero in HeroManager.Enemies.Where(x => x.IsValidTarget(_w.Range) && _menu.Item("WC" + x.ChampionName).GetValue<bool>()))
                {
                    if((target == hero)&&_w.CanCast(hero))
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
            if (_e.IsReady() && _menu.Item("EC").GetValue<bool>())
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
            if (_q.IsReady() && _e.IsReady() && Player.Mana >= _q.Instance.ManaCost + _e.Instance.ManaCost && _menu.Item("QEC").GetValue<bool>() && _menu.Item("EC").GetValue<bool>())
            {
                PixEQCombo();
            }
        }

        public static void Harass2()
        {
            if((Player.Mana * 100 / Player.MaxMana >= _menu.Item("ManaH").GetValue<Slider>().Value)&& _menu.Item("QEH").GetValue<bool>())
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
                    if (target != null && _e.CanCast(target))
                    {
                        _e.CastOnUnit(target);
                    }
                        
                }
            }
        }
        // get Pix!
        public static void Getpixed()
        {
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
