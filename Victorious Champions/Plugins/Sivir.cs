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

            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;                                 // Q/R Logic for Combo / Harass Mode
            Orbwalking.AfterAttack += Orbwalking_OnAfterAttack;                 // W Logic
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;   // Cast E 발동조건

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
            Config.SubMenu("Draw").AddItem(new MenuItem("Draw_Disabled", "Disable All Spell Drawings").SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("Qdraw", "Draw Q Range").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("Rdraw", "Draw R Range").SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("combodamage", "Damage on HPBar")).SetValue(true);

            //Misc
            Config.AddSubMenu(new Menu("E Shield", "CastE"));
            Config.SubMenu("CastE").AddItem(new MenuItem("autoE", "Enable E Shield").SetValue(true));
            Config.SubMenu("CastE").AddItem(new MenuItem("Edmg", "Attacted DMG HP%", true).SetValue(new Slider(0, 100, 0)));      // 몇% 짜리 데미지인지 설정
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != player.Team))
            {
                for (int i = 0; i < 4; i++)     // 기술은 최대 4개지...
                {
                    var spell = enemy.Spellbook.Spells[i];
                    if (spell.SData.TargettingType == SpellDataTargetType.Unit)
                        Config.SubMenu("CastE").SubMenu(enemy.ChampionName).AddItem(new MenuItem("Spell" + spell.SData.Name, spell.Name).SetValue(true));
                }
            }

            Config.AddToMainMenu();

        }
        #endregion

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

        // Cast W 
        private static void Orbwalking_OnAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var harassmana = Config.Item("harassmana").GetValue<Slider>().Value;
            if (!W.IsReady() || !unit.IsMe || !(target is Obj_AI_Hero))
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Config.Item("UseW", true).GetValue<bool>())
            {
                W.Cast();
                Orbwalking.ResetAutoAttackTimer();      // 평타캔슬
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Config.Item("hW", true).GetValue<bool>() &&
                     player.ManaPercent > harassmana)
            {
                var minions = MinionManager.GetMinions(player.Position, player.AttackRange, MinionTypes.All);
                if (minions == null || minions.Count == 0)
                    return;

                int countMinions = 0;
                foreach (var minion in minions.Where(minion => minion.Health < player.GetAutoAttackDamage(minion) + W.GetDamage(minion)))
                    countMinions++;

                // AA 사거리내 미니언이 8마리 이상 있고, AA+W Damage로 죽일 수 있는 미니언이 2마리 이상 있을때 기술 발동
                if ((minions.Count > 8) && (countMinions>1))
                {
                    W.Cast();
                    Orbwalking.ResetAutoAttackTimer();
                }
                
            }
        }
        // Cast E
        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            // 적 챔피언이 보낸 기술이 아니거나, 타겟이 내가 아니거나, 모르가나 장판이 아닌 경우...
            if (!E.IsReady() || !sender.IsEnemy || sender.IsMinion || args.Target == null || !args.Target.IsMe || !sender.IsValid<Obj_AI_Hero>() || args.SData.Name == "TormentedSoil")
                return;

            // 해당 기술의 메뉴가 없거나, Off로 되어 있으면 리턴
            if (Config.Item("spell" + args.SData.Name) != null && !Config.Item("spell" + args.SData.Name).GetValue<bool>())
                return;

            var dmg = sender.GetSpellDamage(ObjectManager.Player, args.SData.Name);     // 예상 데미지 예측
            double HpPercentage = (dmg * 100) / player.Health;                          // 예상 데미지% 예측 
            //double HpLeft = ObjectManager.Player.Health - dmg;                        // 기술 맞고 남은 HP 계산

            if (HpPercentage >= Config.Item("Edmg", true).GetValue<Slider>().Value &&   // 설정 기술로 받는 데미지가 설정 수준 이상이면 (0이면 무조건 발동): 이거 삭제하자! 막을건지 말건지만 선택하면 됨.
                sender.IsEnemy && args.Target.IsMe && !args.SData.IsAutoAttack() && 
                Config.Item("autoE", true).GetValue<bool>())
            {
                E.Cast();
            }
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (player.IsDead || MenuGUI.IsChatOpen || player.IsRecalling())
            {
                return;
            }

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
                    break;
            }

            QCastKillSteal();


        }

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
            return new float[] {0, 0};
        }

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
                // 한번의 시전으로 2명 이상 맞출 수 있으면 기술 시전
                if (player.CountEnemiesInRange(Q.Range) >= 2)
                {
                    if (QCastOKTW(t, true)) return;
                }

                // 챔피언이 Q사거리 끝선에 있어 무조건 2번 맞고 내려올 수 있을때
                if ((t.Distance(player)) >= 950f)       // 사거리 950f는 튜닝 필요 (Q 사거리 1250f)
                {
                    if (QCastOKTW(t, false)) return;
                }
            }

        }

        // Combo시 사용되는 R Logic: 발동조건이 조금 까다로움.
        private static void ComboLogicR()
        {
            // 1250f 범위내 적군이 2명 이상이고, 1000f 범위내 아군이 3명 이상이면 자동 발동 (그 외에는 수동으로 조작 필요)
            if ((player.CountEnemiesInRange(Q.Range) > 1) && (player.CountAlliesInRange(R.Range) > 2)) R.Cast();
        }

        private static void OnDraw(EventArgs args)
        {
            var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;

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
                        Color.DarkRed
                    );
            }
        }

    }
}
