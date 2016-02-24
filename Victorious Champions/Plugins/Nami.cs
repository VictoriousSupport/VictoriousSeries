/// <summary>
///  [Jinx's Support: Nami]
///  
///     [2016.02.07]
///         1. Q Logic: OKTW Prediction 적용
///        
/// </summary>


namespace JinxsSupport.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using LeagueSharp;
    using LeagueSharp.Common;
    using System.Drawing;

    internal enum Spells
    {
        Q,
        W,
        E,
        R
    }

    internal class Nami : IPlugin
    {

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public static Orbwalking.Orbwalker Orbwalker;
        static SpellSlot _ignite;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>() {
            { Spells.Q, new Spell (SpellSlot.Q, 875) },
            { Spells.W, new Spell (SpellSlot.W, 725) },
            { Spells.E, new Spell (SpellSlot.E, 800) },
            { Spells.R, new Spell (SpellSlot.R, 2750) },
        };

        private static Menu _menu;

        #region hitchance

        static HitChance CustomHitChance
        {
            get { return GetHitchance(); }
        }

        static HitChance GetHitchance()
        {
            switch (_menu.Item("ElNamiReborn.hitChance").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.Medium;
            }
        }

        #endregion

        #region Load() Function
        public void Load()
        {
            if (!Player.ChampionName.Equals("Nami", StringComparison.CurrentCultureIgnoreCase))
                return;

            spells[Spells.Q].SetSkillshot(1f, 150f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            spells[Spells.R].SetSkillshot(0.5f, 260f, 850f, false, SkillshotType.SkillshotLine);

            _ignite = Player.GetSpellSlot("summonerdot");

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            Entry.PrintChat("<font color=\"#66CCFF\" >Nami</font>");
        }
        #endregion

        #region CreateMenu
        public void CreateMenu()
        {
            _menu = new Menu("Victorious Nami", "menu", true);

            //ElNamiReborn.Orbwalker
            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            Nami.Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            _menu.AddSubMenu(orbwalkerMenu);

            //ElNamiReborn.TargetSelector
            var targetSelector = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelector);
            _menu.AddSubMenu(targetSelector);

            //ElNamiReborn.Combo
            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.AddItem(new MenuItem("ElNamiReborn.Combo.Q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElNamiReborn.Combo.W", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElNamiReborn.Combo.E", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElNamiReborn.Combo.R", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElNamiReborn.Combo.R.Count", "Minimum targets R")).SetValue(new Slider(3, 1, 5));
            //comboMenu.AddItem(new MenuItem("ElNamiReborn.Combo.Ignite", "Use ignite").SetValue(false));

            _menu.AddSubMenu(comboMenu);

            //ElNamiReborn.Harass
            var harassMenu = new Menu("Harass", "Harass");
            harassMenu.AddItem(new MenuItem("ElNamiReborn.Harass.Q", "Use Q").SetValue(false));
            harassMenu.AddItem(new MenuItem("ElNamiReborn.Harass.W", "Use W").SetValue(true));
            harassMenu.AddItem(new MenuItem("ElNamiReborn.Harass.E", "Use E").SetValue(false));
            harassMenu.AddItem(new MenuItem("ElNamiReborn.Harass.Mana", "Minimum mana for harass")).SetValue(new Slider(40));

            _menu.AddSubMenu(harassMenu);

            //ElNamiReborn.E
            var castEMenu = _menu.AddSubMenu(new Menu("E settings", "ESettings"));
            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(champ => champ.IsAlly))
            {
                castEMenu.AddItem(new MenuItem("ElNamiReborn.Settings.E1" + ally.CharData.BaseSkinName, string.Format("Cast E: {0}", ally.CharData.BaseSkinName)).SetValue(true));
            }

            //ElNamiReborn.Heal
            var healMenu = new Menu("Heal settings", "HealSettings");
            healMenu.AddItem(new MenuItem("ElNamiReborn.Heal.Activate", "Use heal").SetValue(true));
            healMenu.AddItem(new MenuItem("ElNamiReborn.Heal.Player.HP", "HP percentage").SetValue(new Slider(25, 1, 100)));
            healMenu.AddItem(new MenuItem("ElNamiReborn.Heal.Ally.HP", "Use heal on ally's").SetValue(true));
            healMenu.AddItem(new MenuItem("ElNamiReborn.Heal.Ally.HP.Percentage", "HP percentage ally's").SetValue(new Slider(40, 1, 100)));
            healMenu.AddItem(new MenuItem("ElNamiReborn.Heal.Mana", "Mininum mana needed")).SetValue(new Slider(45));

            _menu.AddSubMenu(healMenu);

            //ElNamiReborn.Interupt
            var interuptMenu = new Menu("Interupt settings", "interuptsettings");
            interuptMenu.AddItem(new MenuItem("ElNamiReborn.Interupt.Q", "Use Q").SetValue(true));
            interuptMenu.AddItem(new MenuItem("ElNamiReborn.Interupt.R", "Use R").SetValue(false));

            _menu.AddSubMenu(interuptMenu);

            //ElNamiReborn.Misc
            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.AddItem(new MenuItem("ElNamiReborn.Draw.Q", "Draw Q").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElNamiReborn.Draw.W", "Draw W").SetValue(true));
            miscMenu.AddItem(new MenuItem("ElNamiReborn.Draw.E", "Draw E").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElNamiReborn.Draw.R", "Draw R").SetValue(false));
            //miscMenu.AddItem(new MenuItem("ElNamiReborn.Draw.Text", "Draw Text").SetValue(true));

            _menu.AddSubMenu(miscMenu);

            _menu.AddToMainMenu();

        }
        #endregion

        public static void Drawing_OnDraw(EventArgs args)
        {

            var drawQ = _menu.Item("ElNamiReborn.Draw.Q").GetValue<bool>();
            var drawW = _menu.Item("ElNamiReborn.Draw.W").GetValue<bool>();
            var drawE = _menu.Item("ElNamiReborn.Draw.E").GetValue<bool>();
            var drawR = _menu.Item("ElNamiReborn.Draw.R").GetValue<bool>();

            var playerPos = Drawing.WorldToScreen(ObjectManager.Player.Position);

            if (drawQ)
                if (spells[Spells.Q].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Nami.spells[Spells.Q].Range, Color.White, 2);

            if (drawE)
                if (spells[Spells.E].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Nami.spells[Spells.E].Range, Color.White, 2);

            if (drawW)
                if (spells[Spells.W].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Nami.spells[Spells.W].Range, Color.White, 2);

            if (drawR)
                if (spells[Spells.R].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Nami.spells[Spells.R].Range, Color.White);

        }

        #region OnGameUpdate

        static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo(target);
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass(target);
                    break;
            }

            PlayerHealing();
            AllyHealing();
            //AutoHarass();
        }

        #endregion

        #region PlayerHealing

        static void PlayerHealing()
        {
            if (Player.IsRecalling() || Player.InFountain())
                return;

            var useHeal = _menu.Item("ElNamiReborn.Heal.Activate").GetValue<bool>();
            var playerHp = _menu.Item("ElNamiReborn.Heal.Player.HP").GetValue<Slider>().Value;
            var minumumMana = _menu.Item("ElNamiReborn.Heal.Mana").GetValue<Slider>().Value;

            if (useHeal && (Player.Health / Player.MaxHealth) * 100 <= playerHp && spells[Spells.W].IsReady() && ObjectManager.Player.ManaPercent >= minumumMana)
            {
                spells[Spells.W].Cast(ObjectManager.Player);
            }
        }

        #endregion

        #region AllyHealing

        static void AllyHealing()
        {
            if (ObjectManager.Player.IsRecalling() || ObjectManager.Player.InFountain())
                return;

            var useHeal = _menu.Item("ElNamiReborn.Heal.Ally.HP").GetValue<bool>();
            var allyHp = _menu.Item("ElNamiReborn.Heal.Ally.HP.Percentage").GetValue<Slider>().Value;
            var minumumMana = _menu.Item("ElNamiReborn.Heal.Mana").GetValue<Slider>().Value;

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe))
            {
                if (hero.IsRecalling() || hero.InFountain())
                    return;

                if (useHeal && (hero.Health / hero.MaxHealth) * 100 <= allyHp && spells[Spells.W].IsReady() && hero.Distance(Player.ServerPosition) <= spells[Spells.W].Range && ObjectManager.Player.ManaPercent >= minumumMana)
                {
                    spells[Spells.W].Cast(hero);
                }
            }
        }

        #endregion
        public static bool QCastOKTW(Obj_AI_Hero target, OKTWPrediction.HitChance hitChance)
        {
            var spell = spells[Spells.Q];// Q;
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

            // 단일 타겟일때는 VeryHigh, 다중 타겟을때는 High 기준으로 기술 시전
            if (poutput2.Hitchance >= OKTWPrediction.HitChance.VeryHigh)
            {
                return spell.Cast(poutput2.CastPosition);
            }

            return false;
        }
        #region Harass

        static void Harass(Obj_AI_Base target)
        {
            if (target == null || !target.IsValidTarget() || target.IsMinion)
                return;

            var useQ = _menu.Item("ElNamiReborn.Harass.Q").GetValue<bool>();
            var useW = _menu.Item("ElNamiReborn.Harass.W").GetValue<bool>();
            var useE = _menu.Item("ElNamiReborn.Harass.E").GetValue<bool>();
            var checkMana = _menu.Item("ElNamiReborn.Harass.Mana").GetValue<Slider>().Value;

            if (Player.ManaPercent < checkMana)
                return;

            if (useQ && spells[Spells.Q].IsReady())
            {
                var harassQtarget = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
                QCastOKTW(harassQtarget, OKTWPrediction.HitChance.VeryHigh);       // by Jinx
            }

            if (useE && spells[Spells.E].IsReady())
            {
                // 선택된 챔프중 가장 가까이 있는 챔프 선택
                var selectedAlly =
                            HeroManager.Allies.Where(hero => hero.IsAlly && _menu.Item("ElNamiReborn.Settings.E1" + hero.CharData.BaseSkinName).GetValue<bool>())
                                .OrderBy(closest => closest.Distance(target))
                                .FirstOrDefault();

                if (spells[Spells.E].IsInRange(selectedAlly) && spells[Spells.E].IsReady())
                {
                    spells[Spells.E].CastOnUnit(selectedAlly);
                }
            }

            if (useW && spells[Spells.W].IsReady())
            {
                if(target.IsValidTarget(spells[Spells.W].Range))
                    spells[Spells.W].Cast(target);
            }
        }

        #endregion

        #region Combo

        static void Combo(Obj_AI_Base target)
        {
            if (target == null || !target.IsValidTarget())
                return;

            var useQ = _menu.Item("ElNamiReborn.Combo.Q").GetValue<bool>();
            var useW = _menu.Item("ElNamiReborn.Combo.W").GetValue<bool>();
            var useE = _menu.Item("ElNamiReborn.Combo.E").GetValue<bool>();
            var useR = _menu.Item("ElNamiReborn.Combo.R").GetValue<bool>();
            //var useIgnite = _menu.Item("ElNamiReborn.Combo.Ignite").GetValue<bool>();
            var countR = _menu.Item("ElNamiReborn.Combo.R.Count").GetValue<Slider>().Value;

            if (useQ && spells[Spells.Q].IsReady())
            {
                var harassQtarget = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
                QCastOKTW(harassQtarget, OKTWPrediction.HitChance.VeryHigh);       // by Jinx
            }

            if (useE && spells[Spells.E].IsReady())
            {
                var selectedAlly =
                       HeroManager.Allies.Where(hero => hero.IsAlly && _menu.Item("ElNamiReborn.Settings.E1" + hero.CharData.BaseSkinName).GetValue<bool>())
                           .OrderBy(closest => closest.Distance(target))            // 적 타겟에서 가까운 아군 챔프를 정렬함.
                           .FirstOrDefault();

                if (spells[Spells.E].IsInRange(selectedAlly) && spells[Spells.E].IsReady()) 
                {
                    spells[Spells.E].CastOnUnit(selectedAlly);
                }

            }

            if (useW && spells[Spells.W].IsReady())
            {
                if (target.IsValidTarget(spells[Spells.W].Range))
                    spells[Spells.W].Cast(target);
            }

            /*
            if (useR && spells[Spells.R].IsReady()
                && ObjectManager.Player.CountEnemiesInRange(spells[Spells.R].Range) >= countR
                && spells[Spells.R].IsInRange(target.ServerPosition))
            {
                spells[Spells.R].CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
                //spells[Spells.R].CastIfWillHit(target, 2);
            }
            */

            /// 라인전(?)
            float nSumHPPercent = 0;
            int nEnemyCount = 0;

            /// 한타시(?)
            float nTotalSumHPPercent = 0;
            int nTotalenemyCount = 0;

            if (useR && spells[Spells.R].IsReady())
            {
                foreach (var enemy in HeroManager.Enemies.Where(x => x.IsValidTarget(spells[Spells.R].Range)))
                {
                    // 라인전 6랩 혹은 갱을 고려하였을때 궁극기 시전 조건 추가: 2명인데 두명의 합산 HP%가 100이 되지 않을 경우 (Max: 200%)
                    if (ObjectManager.Player.CountEnemiesInRange(spells[Spells.R].Range) == 2)
                    {
                        nEnemyCount++;
                        nSumHPPercent += enemy.Health / enemy.MaxHealth * 90;
                        // 두명 모두 더한 HP%값이 100% 이하이며, Q 사거리 이내 아군이 1명이라도 함께 있는 경우 궁극기 시전 (라인전에서 아군 원딜과 함께 있는데 적군2명의 피가 평균 반피 이하인 경우 자동 발동)
                        if ((nEnemyCount==2)&&(nSumHPPercent<100)&&ObjectManager.Player.CountAlliesInRange(spells[Spells.Q].Range) > 0)       
                        {
                            spells[Spells.R].CastIfWillHit(enemy, 2);
                        }
                    }

                    // 한타시 3인(Default) 이상이고, 평균 HP% = 75% 이하일 경우, 즉 그 풀피 이니시는 그냥 손으로 하셈.
                    if (ObjectManager.Player.CountEnemiesInRange(spells[Spells.R].Range) >= countR)
                    {
                        nTotalenemyCount++;
                        nTotalSumHPPercent += enemy.Health / enemy.MaxHealth * 100;
                        if ((nTotalenemyCount>=countR)&&(nTotalSumHPPercent<(nTotalenemyCount*100*0.75)))
                        {
                            spells[Spells.R].CastIfWillHit(enemy, countR);
                        }
                            
                    }
                }
            }

        }

        #endregion

        #region Intterupt

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (args.DangerLevel != Interrupter2.DangerLevel.High || sender.Distance(Player) > spells[Spells.Q].Range)
                return;

            if (sender.IsValidTarget(spells[Spells.Q].Range) && args.DangerLevel == Interrupter2.DangerLevel.High && spells[Spells.Q].IsReady())
            {
                spells[Spells.Q].Cast(sender);
            }
        }

        #endregion

        #region GapCloser

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsValidTarget(spells[Spells.Q].Range))
                return;

            if (gapcloser.Sender.Distance(Player) > spells[Spells.Q].Range)
                return;


            var useQ = _menu.Item("ElNamiReborn.Interupt.Q").GetValue<bool>();
            var useR = _menu.Item("ElNamiReborn.Interupt.R").GetValue<bool>();


            if (gapcloser.Sender.IsValidTarget(spells[Spells.Q].Range))
            {
                if (useQ && spells[Spells.Q].IsReady())
                {
                    spells[Spells.Q].Cast(gapcloser.Sender);
                }

                if (useR && !spells[Spells.Q].IsReady() && spells[Spells.R].IsReady())
                {
                    spells[Spells.R].Cast(gapcloser.Sender);
                }
            }
        }

        #endregion

        #region IgniteDamage

        static float IgniteDamage(Obj_AI_Base target)
        {
            if (_ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(_ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        #endregion
    }
}

