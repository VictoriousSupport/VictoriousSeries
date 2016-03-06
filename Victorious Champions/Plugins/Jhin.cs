
using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;
using System.Collections.Generic;

#pragma warning disable 618 //,612

namespace JinxsSupport.Plugins
{
    internal class Jhin : IPlugin
    {
        private Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;

        private Spell E, Q, R, W;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        //private bool Ractive = false;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Vector3 rPosLast;
        private Obj_AI_Hero rTargetLast;
        private Vector3 rPosCast;

        public static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>(), Allies = new List<Obj_AI_Hero>();

        private Items.Item
                    FarsightOrb = new Items.Item(3342, 4000f),
                    ScryingOrb = new Items.Item(3363, 3500f);

        private static string[] Spells =
        {
            "katarinar","drain","consume","absolutezero", "staticfield","reapthewhirlwind","jinxw","jinxr","shenstandunited","threshe","threshrpenta","threshq","meditate","caitlynpiltoverpeacemaker", "volibearqattack",
            "cassiopeiapetrifyinggaze","ezrealtrueshotbarrage","galioidolofdurand","luxmalicecannon", "missfortunebullettime","infiniteduress","alzaharnethergrasp","lucianq","velkozr","rocketgrabmissile"
        };

        #region Load() Function
        public void Load()
        {
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 2500);
            E = new Spell(SpellSlot.E, 760);
            R = new Spell(SpellSlot.R, 3500);

            W.SetSkillshot(0.75f, 40, float.MaxValue, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.3f, 200, 1600, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.2f, 80, 5000, false, SkillshotType.SkillshotLine);

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                    Enemies.Add(hero);

                if (hero.IsAlly)
                    Allies.Add(hero);
            }

            Entry.PrintChat("<font color=\"#66CCFF\" >Jhin</font>");
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            Config = new Menu("Vicroious Jhin", "menu", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.GreenYellow);
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.SubMenu("Draw").AddItem(new MenuItem("Draw_AA", "Draw AA", true).SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu("Draw").AddItem(new MenuItem("rRangeMini", "R Range Minimap", true).SetValue(true));

            Config.SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu("Q Config").AddItem(new MenuItem("Qminion", "Q on minion", true).SetValue(true));

            Config.SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("harrasW", "Harass W", true).SetValue(false));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wstun", "W stun, marked only", true).SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("Waoe", "W aoe (above 2 enemy)", true).SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("autoWcc", "Auto W CC enemy or marked", true).SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("MaxRangeW", "Max W range", true).SetValue(new Slider(2500, 2500, 0)));

            Config.SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E on hard CC", true).SetValue(true));
            Config.SubMenu("E Config").AddItem(new MenuItem("bushE", "Auto E bush", true).SetValue(true));
            Config.SubMenu("E Config").AddItem(new MenuItem("Espell", "E on special spell detection", true).SetValue(true));
            Config.SubMenu("E Config").AddItem(new MenuItem("EmodeCombo", "E combo mode", true).SetValue(new StringList(new[] { "always", "run - cheese" }, 1)));
            Config.SubMenu("E Config").AddItem(new MenuItem("Eaoe", "Auto E x enemies", true).SetValue(new Slider(3, 5, 0)));
            Config.SubMenu("E Config").SubMenu("E Gap Closer").AddItem(new MenuItem("EmodeGC", "Gap Closer position mode", true).SetValue(new StringList(new[] { "Dash end position", "My hero position" }, 0)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu("E Config").SubMenu("E Gap Closer").AddItem(new MenuItem("EGCchampion" + enemy.ChampionName, enemy.ChampionName, true).SetValue(true));

            Config.SubMenu("R Config").AddItem(new MenuItem("autoR", "Enable R", true).SetValue(true));
            Config.SubMenu("R Config").AddItem(new MenuItem("Rvisable", "Don't shot if enemy is not visable", true).SetValue(false));
            Config.SubMenu("R Config").AddItem(new MenuItem("Rks", "Auto R if can kill in 2 hits", true).SetValue(true));
            Config.SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space
            Config.SubMenu("R Config").AddItem(new MenuItem("MaxRangeR", "Max R range", true).SetValue(new Slider(2800, 3500, 0)));
            Config.SubMenu("R Config").AddItem(new MenuItem("MinRangeR", "Min R range", true).SetValue(new Slider(1000, 3500, 0)));
            Config.SubMenu("R Config").AddItem(new MenuItem("Rsafe", "R safe area", true).SetValue(new Slider(1200, 2000, 0))); // 주변에 적이 없을때
            Config.SubMenu("R Config").AddItem(new MenuItem("trinkiet", "Auto blue trinkiet", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q", true).SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("farmW", "Lane clear W", true).SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("farmE", "Lane clear E", true).SetValue(false));
            Config.SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("Farm").AddItem(new MenuItem("LCminions", "LaneClear minimum minions", true).SetValue(new Slider(7, 10, 0)));
            Config.SubMenu("Farm").AddItem(new MenuItem("jungleE", "Jungle clear E", true).SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q", true).SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("jungleW", "Jungle clear W", true).SetValue(true));

            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }
        #endregion

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.R)
            {
                if (Config.Item("trinkiet", true).GetValue<bool>() && !IsCastingR)
                {
                    if (Player.Level < 9)
                        ScryingOrb.Range = 2500;
                    else
                        ScryingOrb.Range = 3500;

                    if (ScryingOrb.IsReady())
                        ScryingOrb.Cast(rPosLast);
                    if (FarsightOrb.IsReady())
                        FarsightOrb.Cast(rPosLast);
                }
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if(sender.IsMe && args.SData.Name == "JhinR")
            {
                rPosCast = args.End;
            }
            if (!E.IsReady() || sender.IsMinion || !sender.IsEnemy || !Config.Item("Espell", true).GetValue<bool>() || !sender.IsValid<Obj_AI_Hero>() || !sender.IsValidTarget(E.Range))
                return;

            var foundSpell = Spells.Find(x => args.SData.Name.ToLower() == x);
            if (foundSpell != null)
            {
                E.Cast(sender.Position);
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && Player.Mana > RMANA + WMANA)
            {
                var t = gapcloser.Sender;
                if (t.IsValidTarget(W.Range) && Config.Item("EGCchampion" + t.ChampionName, true).GetValue<bool>())
                {
                    if (Config.Item("EmodeGC", true).GetValue<StringList>().SelectedIndex == 0)
                        E.Cast(gapcloser.End);
                    else
                        E.Cast(Player.ServerPosition);
                }
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Entry.LagFree(0))
            {
                SetMana();
                Jungle();
            }

            if (Entry.LagFree(1) && R.IsReady() && Config.Item("autoR", true).GetValue<bool>())
                LogicR();

            if (IsCastingR)
            {
                OktwCommon.blockMove = true;
                OktwCommon.blockAttack = true;
                Orbwalking.Attack = false;
                Orbwalking.Move = false;
                return;
            }
            else
            {
                OktwCommon.blockMove = false;
                OktwCommon.blockAttack = false;
                Orbwalking.Attack = true;
                Orbwalking.Move = true;
            }


            if (Entry.LagFree(4) && E.IsReady())
                LogicE();

            if (Entry.LagFree(2) && Q.IsReady() && Config.Item("autoQ", true).GetValue<bool>())
                LogicQ();

            if (Entry.LagFree(3) && W.IsReady() && !Player.IsWindingUp && Config.Item("autoW", true).GetValue<bool>())
                LogicW();
        }

        private void LogicR()
        {
            if (!IsCastingR)                // 쏘는 도중이 아니면... 즉 R 한방 나가면 최대 사거리는 3500로 재조정
                R.Range = Config.Item("MaxRangeR", true).GetValue<Slider>().Value;
            else
                R.Range = 3500;

            var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                rPosLast = R.GetPrediction(t).CastPosition;
                if (Config.Item("useR", true).GetValue<KeyBind>().Active && !IsCastingR)        // 메뉴얼 R 발동
                {
                    R.Cast(rPosLast);
                    rTargetLast = t;
                }
                
                if (!IsCastingR && Config.Item("Rks", true).GetValue<bool>() 
                    && GetRdmg(t) * 3 > t.Health && t.CountAlliesInRange(500) == 0 && Player.CountEnemiesInRange(Config.Item("Rsafe", true).GetValue<Slider>().Value) == 0 
                    && Player.Distance(t) > Config.Item("MinRangeR", true).GetValue<Slider>().Value
                    && !Player.UnderTurret(true) && OktwCommon.ValidUlt(t) && !OktwCommon.IsSpellHeroCollision(t, R))
                {
                    R.Cast(rPosLast);
                    rTargetLast = t;
                }

                if (IsCastingR) // 2타 이후에 로직
                {
                    if(InCone(t))       // R 사정거리 안에 있으면
                        R.Cast(t);
                    else
                    {
                        // 만약 타겟을 놓치면 다른 적을 찾아냄.
                        foreach(var enemy in Enemies.Where(enemy => enemy.IsValidTarget(R.Range) && InCone(enemy)).OrderBy(enemy => enemy.Health))
                        {
                            R.Cast(t);
                            rPosLast = R.GetPrediction(enemy).CastPosition;
                            rTargetLast = enemy;
                        }
                    }
                }
            }
            else if (IsCastingR && rTargetLast != null && !rTargetLast.IsDead)
            {
                if(!Config.Item("Rvisable", true).GetValue<bool>())
                    R.Cast(rPosLast);
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                var wDmg = GetWdmg(t);
                if (wDmg > t.Health - OktwCommon.GetIncomingDamage(t))
                    Entry.OKTWCast_SebbyLib(W, t, false);

                if (Player.CountEnemiesInRange(400) > 1 || Player.CountEnemiesInRange(250) > 0)
                    return;

                if (t.HasBuff("jhinespotteddebuff") || !Config.Item("Wstun", true).GetValue<bool>() )   // 일단 W는 스턴 목적으로만 동작함.
                {
                    if (Player.Distance(t) < Config.Item("MaxRangeW", true).GetValue<Slider>().Value)
                    {
                        if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && Player.Mana > RMANA + WMANA)
                        {
                            // 콤보모드에 모두 통합시킴. 우선 CC 가능 타겟 / AOE / 그래도 없으면 일반 타겟
                            if (W.IsReady() && Config.Item("autoWcc", true).GetValue<bool>())
                                foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && (!OktwCommon.CanMove(enemy) || enemy.HasBuff("jhinespotteddebuff"))))
                                    W.Cast(enemy);

                            if (W.IsReady() && Config.Item("Waoe", true).GetValue<bool>())
                                W.CastIfWillHit(t, 2);

                            if (W.IsReady() && !Player.IsWindingUp)
                                Entry.OKTWCast_SebbyLib(W, t, false);
                        }
                    }
                }
            }

        }

        private void LogicE()
        {
            if (Config.Item("autoE", true).GetValue<bool>())
                foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                    E.Cast(enemy.ServerPosition);

            var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget())
            {
                if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && !Player.IsWindingUp)
                {
                    if (Config.Item("EmodeCombo", true).GetValue<StringList>().SelectedIndex == 1)
                    {
                        if (E.GetPrediction(t).CastPosition.Distance(t.Position) > 100)
                        {
                            if (Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                            {
                                if (t.Position.Distance(Player.ServerPosition) < t.Position.Distance(Player.Position))
                                    Entry.OKTWCast_SebbyLib(E, t, true);
                            }
                            else
                            {
                                if (t.Position.Distance(Player.ServerPosition) > t.Position.Distance(Player.Position))
                                    Entry.OKTWCast_SebbyLib(E, t, true);
                            }
                        }
                    }
                    else
                    {
                        Entry.OKTWCast_SebbyLib(E, t, true);
                    }
                }

                E.CastIfWillHit(t, Config.Item("Eaoe", true).GetValue<Slider>().Value);
            }
        }

        private void LogicQ()
        {
            var torb = Orbwalker.GetTarget();

            if (torb == null || torb.Type != GameObjectType.obj_AI_Hero)
            {
                if (Config.Item("Qminion", true).GetValue<bool>())
                {
                    var t = TargetSelector.GetTarget(Q.Range + 400, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                    {
                        var minion = MinionManager.GetMinions(Prediction.GetPrediction(t, 0.4f).CastPosition, 350, MinionTypes.All , MinionTeam.Enemy, MinionOrderTypes.MaxHealth).Where(minion2 => minion2.IsValidTarget(Q.Range)).FirstOrDefault();
                        if (minion.IsValidTarget())
                        {
                            if (t.Health < GetQdmg(t))
                                Q.CastOnUnit(minion);
                            if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && Player.Mana > RMANA + EMANA)
                                Q.CastOnUnit(minion);
                        }
                    }
                }

            }
            else if(!Orbwalking.CanAttack() && !Player.IsWindingUp)
            {
                var t = torb as Obj_AI_Hero;
                if (t.Health < GetQdmg(t) + GetWdmg(t))
                    Q.CastOnUnit(t);
                if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && Player.Mana > RMANA + QMANA)       // 콤보 동작시
                    Q.CastOnUnit(t);
            }

            if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmQ", true).GetValue<bool>())
            {
                var minionList = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

                if (minionList.Count > Config.Item("LCminions", true).GetValue<Slider>().Value)
                    Q.CastOnUnit(minionList[0]);
            }
        }


        private bool InCone(Obj_AI_Hero enemy)
        {
            var range = R.Range;
            var angle = 70f * (float)Math.PI / 180;
            var end2 = rPosCast.To2D() - Player.Position.To2D();
            var edge1 = end2.Rotated(-angle / 2);
            var edge2 = edge1.Rotated(angle);

            var point = enemy.Position.To2D() - Player.Position.To2D();
            if (point.Distance(new Vector2(), true) < range * range && edge1.CrossProduct(point) > 0 && point.CrossProduct(edge2) > 0)
                return true;

            return false;
        }

        private void Jungle()
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];

                    if (W.IsReady() && Config.Item("jungleW", true).GetValue<bool>())
                    {
                        W.Cast(mob.ServerPosition);
                        return;
                    }
                    if (E.IsReady() && Config.Item("jungleE", true).GetValue<bool>())
                    {
                        E.Cast(mob.ServerPosition);
                        return;
                    }
                    if (Q.IsReady() && Config.Item("jungleQ", true).GetValue<bool>())
                    {
                        Q.CastOnUnit(mob);
                        return;
                    }
                }
            }
        }

        private bool IsCastingR { get { return R.Instance.Name == "JhinRShot"; } }

        private double GetRdmg(Obj_AI_Base target)
        {
            var damage = ( -25 + 75 * R.Level + 0.2 * Player.FlatPhysicalDamageMod) * (1 + (100 - target.HealthPercent) * 0.02);

            return Player.CalcDamage(target, Damage.DamageType.Physical, damage);
        }

        private double GetWdmg(Obj_AI_Base target)
        {
            var damage = 55 + W.Level * 35 + 0.7 * Player.FlatPhysicalDamageMod;

            return Player.CalcDamage(target, Damage.DamageType.Physical, damage);
        }

        private double GetQdmg(Obj_AI_Base target)
        {
            var damage = 35 + Q.Level * 25 + 0.4 * Player.FlatPhysicalDamageMod;

            return Player.CalcDamage(target, Damage.DamageType.Physical, damage);
        }
        private void SetMana()
        {
            if ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) || Player.HealthPercent < 20)
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
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
        }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (Config.Item("rRangeMini", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1, 20, true);
                }
                else
                    Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1, 20, true);
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            if (Config.Item("Draw_AA", true).GetValue<bool>())
                Render.Circle.DrawCircle(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius, System.Drawing.Color.White, 1);

            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
            }
            if (Config.Item("wRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (W.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
            }
            if (Config.Item("eRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (E.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
            }
            if (Config.Item("rRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
            }
        }
    }
}
