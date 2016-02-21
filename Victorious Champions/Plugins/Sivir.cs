using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;

namespace JinxsSupport.Plugins
{
    internal class Sivir : IPlugin
    {
        public const string ChampName = "Sivir";
        public const string Menuname = "JustSivir";
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;
        public static int qOff = 0, wOff = 0, eOff = 0, rOff = 0;
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;                // by Jinx (OKTW)

        public struct EShieldDB
        {
            public string strSpellName;
            public bool bEnable;
            public EShieldDB(string strName, bool Enable)
            {
                strSpellName = strName;
                bEnable = Enable;
            }
        }
        private static bool bIniEShieldMenu = false;        // Eshield Menu Configure Flag
        private static int g_nTotalSpellCnt = 0;
        public static EShieldDB[] nEBlockSpellList;

        private static bool g_bWComboEnable = false;        // W Cast Combo Enable
        private static bool g_bWHarassEnable = false;       // W Cast Harass Enable

        #region Load() Function
        public void Load()
        {
            if (ObjectManager.Player.ChampionName != "Sivir")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1250f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 1000f);
            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);

            nEBlockSpellList = new EShieldDB[20];
            bIniEShieldMenu = false;
            g_bWComboEnable = true;
            g_bWHarassEnable = true;
            g_nTotalSpellCnt = 0;

            Entry.PrintChat("<font color=\"#66CCFF\" >Sivir</font>");
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            Config = new Menu("Victorious Sivir", "Sivir", true);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));

            //Harass
            Config.AddSubMenu(new Menu("Harass/Lane", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("hQ", "Use Q").SetValue(true));                   // Q Harass 추천 (보수적인 발동)
            Config.SubMenu("Harass").AddItem(new MenuItem("hW", "Laneclear W").SetValue(true));             // W Laneclear는 8마리 이상 미니언 있을때만 발동
            Config.SubMenu("Harass").AddItem(new MenuItem("harassmana", "Mana Percentage").SetValue(new Slider(40, 0, 100)));   // 마나 40% 이상일때만


            //Draw
            Config.AddSubMenu(new Menu("Draw", "Draw"));
            Config.SubMenu("Draw").AddItem(new MenuItem("Draw_AA", "Draw AA").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("Qdraw", "Draw Q Range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("Rdraw", "Draw R Range").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("combodamage", "Damage Indicator")).SetValue(true);

            //E Shield
            Config.AddSubMenu(new Menu("E Shield", "CastE"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != player.Team))
            {
                var spellQ = enemy.Spellbook.Spells[0];
                var spellW = enemy.Spellbook.Spells[1];
                var spellE = enemy.Spellbook.Spells[2];
                var spellR = enemy.Spellbook.Spells[3];

                bool EnableQ = false;
                bool EnableW = false;
                bool EnableE = false;
                bool EnableR = false;

                if ((spellQ.SData.TargettingType == SpellDataTargetType.Unit) || (spellQ.SData.TargettingType == SpellDataTargetType.SelfAndUnit))
                {
                    EnableQ = true;
                    Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellQ.SData.Name, string.Format("Q: {0} (*)", spellQ.Name)).SetValue(EnableQ));
                }
                else
                    Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellQ.SData.Name, string.Format("Q: {0} ({1})", spellQ.Name, spellQ.SData.TargettingType)).SetValue(EnableQ));

                if ((spellQ.SData.TargettingType == SpellDataTargetType.Unit) || (spellQ.SData.TargettingType == SpellDataTargetType.SelfAndUnit))
                {
                    EnableW = true;
                    Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellW.SData.Name, string.Format("W: {0} (*)", spellW.Name)).SetValue(EnableW));
                }
                else
                    Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellW.SData.Name, string.Format("W: {0} ({1})", spellW.Name, spellW.SData.TargettingType)).SetValue(EnableW));

                if ((spellQ.SData.TargettingType == SpellDataTargetType.Unit) || (spellQ.SData.TargettingType == SpellDataTargetType.SelfAndUnit))
                {
                    EnableE = true;
                    Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellE.SData.Name, string.Format("E: {0} (*)", spellE.Name)).SetValue(EnableE));
                }
                else
                    Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellE.SData.Name, string.Format("E: {0} ({1})", spellE.Name, spellE.SData.TargettingType)).SetValue(EnableE));

                if ((spellQ.SData.TargettingType == SpellDataTargetType.Unit) || (spellQ.SData.TargettingType == SpellDataTargetType.SelfAndUnit))
                {
                    EnableR = true;
                    Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellR.SData.Name, string.Format("R: {0} (*)", spellR.Name)).SetValue(EnableR));
                }
                else
                    Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spellR.SData.Name, string.Format("R: {0} ({1})", spellR.Name, spellR.SData.TargettingType)).SetValue(EnableR));

                nEBlockSpellList[g_nTotalSpellCnt++] = new EShieldDB(spellQ.SData.Name, EnableQ);
                nEBlockSpellList[g_nTotalSpellCnt++] = new EShieldDB(spellW.SData.Name, EnableW);
                nEBlockSpellList[g_nTotalSpellCnt++] = new EShieldDB(spellE.SData.Name, EnableE);
                nEBlockSpellList[g_nTotalSpellCnt++] = new EShieldDB(spellR.SData.Name, EnableR);
            }

            bIniEShieldMenu = true;

            Config.AddToMainMenu();

            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;                                 // Q/R Logic for Combo / Harass Mode
            Orbwalking.AfterAttack += Orbwalking_OnAfterAttack;                 // W Logic
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;   // Cast E 발동조건

        }
        #endregion

        #region Combo/Harass
        private static void combo()
        {
            if (Q.IsReady() && !player.IsWindingUp) ComboLogicQ();
            if (R.IsReady() && Config.Item("UseR").GetValue<bool>()) ComboLogicR();
        }
        private static void harass()
        {
            var harassmana = Config.Item("harassmana").GetValue<Slider>().Value;

            if (Q.IsReady() && Config.Item("hQ").GetValue<bool>() && player.ManaPercent >= harassmana)
                HarassLogicQ();
        }
        #endregion

        #region EventHandler
        // Cast W 
        private void Orbwalking_OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {

            var harassmana = Config.Item("harassmana").GetValue<Slider>().Value;

            if (!W.IsReady())                                                       return;
            if (!unit.IsMe && Orbwalker.GetTarget().IsValidTarget())                return;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)          return;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)            return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && g_bWComboEnable)
            {
                var minions = MinionManager.GetMinions(player.Position, player.AttackRange, MinionTypes.All);
                // 적군이 2명이상 혹은 주변에 미니언 4마리 이상 (최소한 맞고 튕길 것은 있어야...)
                if ((ObjectManager.Player.CountEnemiesInRange(Q.Range) > 1) || ((ObjectManager.Player.CountEnemiesInRange(Q.Range/2) == 1) && (minions.Count>3)) )
                {
                    W.Cast();
                    Orbwalking.ResetAutoAttackTimer();      // 평타캔슬
                }
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && g_bWHarassEnable &&
                     player.ManaPercent > harassmana)
            {
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
                
            }

        }
        // Cast E
        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                // 적 챔피언이 보낸 기술이 아니거나, 타겟이 내가 아니거나... 논타겟 기술이면 아래 조건문을 빠져나갈 수 없음.
                if (!E.IsReady() || !sender.IsEnemy || sender.IsMinion || args.Target == null || !args.Target.IsMe || !sender.IsValid<Obj_AI_Hero>() || args.SData.Name == "TormentedSoil")
                    return;

                //Console.WriteLine("Under Attack: '{0}'", args.SData.Name);

                bool bFlag = false;
                bool bEnable = false;       // 최종 스킬을 사용할지 말지
                for (var i = 0; i < g_nTotalSpellCnt; i++)
                    if (nEBlockSpellList[i].strSpellName == args.SData.Name)
                    {
                        bFlag = true;
                        bEnable = nEBlockSpellList[i].bEnable;
                        break;
                    }

                if (!bFlag) return;

                var dmg = sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);     // 예상 데미지 예측
                double HpPercentage = (dmg * 100) / player.MaxHealth;                          // 예상 데미지% 예측 

                Entry.PrintChat(string.Format("Spell Attack:{0}({1:F2}) = {2}", args.SData.Name, HpPercentage, bEnable));

                if (HpPercentage >= 30 && args.SData.IsAutoAttack())
                {
                    Entry.PrintChat("CastE AA Sucess:" + args.SData.Name);                  // 나를 대상으로 하는 AA 데미지가 HP의 35%가 넘는 스킬은 일단 무조건 막고 보자
                    Utility.DelayAction.Add(new Random().Next(50, 125), () => E.Cast());    
                }
                else if (HpPercentage >= 0 && !args.SData.IsAutoAttack() && bEnable)
                {
                    Entry.PrintChat("CastE Sucess:" + args.SData.Name);                     // 이미 지정된 기술인 경우 무조건 방어 (데미지는 없고 CC만 걸리는 기술도 있음.)
                    Utility.DelayAction.Add(new Random().Next(50, 125), () => E.Cast());    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Sivir Obj_AI_Base_OnProcessSpellCast Exception '{0}'", e);
            }
        }

        private static T GetItemValue<T>(string item)
        {
            return Config.Item(item).GetValue<T>();
        }

        private static void Refresh_EShiledMenuFlag()
        {
            if(bIniEShieldMenu)
            {
                for (var i = 0; i < g_nTotalSpellCnt; i++)
                   nEBlockSpellList[i].bEnable = GetItemValue<bool>("Spell" + nEBlockSpellList[i].strSpellName);

                g_bWComboEnable = GetItemValue<bool>("UseW");
                g_bWHarassEnable = GetItemValue<bool>("hW");
            }

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (player.IsDead || MenuGUI.IsChatOpen || player.IsRecalling())
            {
                return;
            }

            Refresh_EShiledMenuFlag();

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    {
                        SetMana();              // by Jinx
                        combo();
                    }
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    harass();
                    break;
            }

            QCastKillSteal();


        }
        #endregion

        #region Method/Logic
        // 현재 마나 잔량에 따라 기술조절 하는 구문! from OKTW
        private static void SetMana()
        {
            if (player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
        }
        // 현재 Q 사거리내 죽일 수 있는 녀석을 우선 처리
        private static bool QCastKillSteal()
        {
            if(Q.IsReady())
            {
                // Q 사거리에 Q Cast로 마물할수 있는 녀석 검색
                var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(enemy => enemy.IsEnemy && !enemy.IsDead && enemy.IsValidTarget(Q.Range) && enemy.Health < player.GetSpellDamage(enemy, SpellSlot.Q));
                if (target != null)
                {
                    QCastOKTW(target, false);
                    return true;
                }
            }

            return false;
        }

        // 움직일 수 없는 타겟을 확인하는 로직
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
        public static bool QCastOKTW(Obj_AI_Hero target, bool bAoe)
        {
            var spell = Q;
            var OKTWPlayer = player;

            OKTWPrediction.SkillshotType CoreType2 = OKTWPrediction.SkillshotType.SkillshotLine;
            bool aoe2 = bAoe;

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

            // Multi Targets 모드일때는 2명 이상 맞을 확률이 High 이상일때 기술 시전 / Single Target 모드일때는 VeryHigh 로 시전
            if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 && poutput2.Hitchance >= OKTWPrediction.HitChance.High)
            {
                return spell.Cast(poutput2.CastPosition);
            }
            else if (!predInput2.Aoe && poutput2.Hitchance >= OKTWPrediction.HitChance.VeryHigh)
            {
                return spell.Cast(poutput2.CastPosition);
            }

            return false;
        }

        // Combo시 사용되는 Q Logic
        private static void ComboLogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget()&Q.IsReady())
            {
                // 현재 타게팅에 관계없이 제일 먼저 Q 거리내 죽일 수 있는 녀석부터 먼저 죽임.
                if (QCastKillSteal()) return;

                // 타게팅이 없고, 사거리내 불능인 녀석이 있으면 Q 시전
                if (player.Mana > RMANA + WMANA)
                {
                    foreach (var enemy in HeroManager.Enemies.Where(x => x.IsValidTarget(Q.Range) && !OKTWCanMove(x)))
                    {
                        QCastOKTW(enemy, false);
                        return;
                    }
                }

                // 타게팅에 Q 시전 
                if (Q.IsReady()) QCastOKTW(t, false);

            }
        }
        // Harass시 사용되는 Q Logic
        private static void HarassLogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget() & Q.IsReady())
            {
                // 한번의 시전으로 2명 이상 맞출 수 있으면 기술 시전, 챔피언이 Q사거리 끝선에 있어 무조건 2번 맞고 내려올 수 있을때
                if ((player.CountEnemiesInRange(Q.Range) >= 2) && ((t.Distance(player)) >= 950f))
                {
                    if (QCastOKTW(t, true)) return;
                }

                /*
                // 
                if ((t.Distance(player)) >= 950f)       // 사거리 950f는 튜닝 필요 (Q 사거리 1250f)
                {
                    if (QCastOKTW(t, false)) return;
                }
                */
            }

        }

        // Combo시 사용되는 R Logic: 발동조건이 조금 까다로움.
        private static void ComboLogicR()
        {
            // 1250f 범위내 적군이 2명 이상이고, 1000f 범위내 아군이 3명 이상이면 자동 발동 (그 외에는 수동으로 조작 필요)
            if ((player.CountEnemiesInRange(Q.Range) > 1) && (player.CountAlliesInRange(R.Range) > 2)) R.Cast();
        }
        #endregion

        #region Drawing
        // For Drawing Function
        private static float GetComboDamage(Obj_AI_Hero Target)
        {
            if (Target != null)
            {
                float ComboDamage = new float();

                ComboDamage = Q.IsReady() ? Q.GetDamage(Target) : 0;
                ComboDamage += W.IsReady() ? W.GetDamage(Target) : 0;
                ComboDamage += player.TotalAttackDamage;
                return ComboDamage;
            }
            return 0;
        }
        // For Drawing Function
        private static float[] GetLength()
        {
            var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (Target != null)
            {
                float[] Length =
                {
                    GetComboDamage(Target) > Target.Health
                        ? 0
                        : (Target.Health - GetComboDamage(Target))/Target.MaxHealth,
                    Target.Health/Target.MaxHealth
                };
                return Length;
            }
            return new float[] { 0, 0 };
        }

        private static void OnDraw(EventArgs args)
        {
            var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (Config.Item("Draw_AA").GetValue<bool>())
                Render.Circle.DrawCircle(player.Position, player.AttackRange + player.BoundingRadius, System.Drawing.Color.White, 1);
            if (Config.Item("Qdraw").GetValue<bool>())
                Render.Circle.DrawCircle(player.Position, Q.Range, System.Drawing.Color.White, 3);
            if (Config.Item("Rdraw").GetValue<bool>())
                Render.Circle.DrawCircle(player.Position, R.Range, System.Drawing.Color.White, 3);

            if (Config.Item("combodamage").GetValue<bool>() && Q.IsInRange(Target))
            {
                float[] Positions = GetLength();
                Drawing.DrawLine
                    (
                        new Vector2(Target.HPBarPosition.X + 10 + Positions[0]*104, Target.HPBarPosition.Y + 20),
                        new Vector2(Target.HPBarPosition.X + 10 + Positions[1]*104, Target.HPBarPosition.Y + 20),
                        9,
                        Color.GreenYellow
                    );
            }

        }
        #endregion

    }
}
