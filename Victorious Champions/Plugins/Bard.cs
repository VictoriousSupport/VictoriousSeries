/// <summary>
///  [Victorious Series: Bard]
///  
///     [2016.02.07]
///         1. 
///        
/// </summary>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace JinxsSupport.Plugins
{
    internal class Bard : IPlugin
    {
        public static Menu BardMenu;

        public static Orbwalking.Orbwalker BardOrbwalker { get; set; }

        //DO YOU HAVE A MOMENT TO TALK ABOUT DIKTIONARIESS!=!=!=!==??!?!? -Everance 2k15
        public static Dictionary<SpellSlot, Spell> spells = new Dictionary<SpellSlot, Spell>()
        {
            {SpellSlot.Q, new Spell(SpellSlot.Q, 950f)},
            {SpellSlot.W, new Spell(SpellSlot.W, 1000f)},
            {SpellSlot.E, new Spell(SpellSlot.E, float.MaxValue)},
            {SpellSlot.R, new Spell(SpellSlot.R, 3400f)}
        };

        public static float LastMoveC;
        public static int TunnelNetworkID;
        public static Vector3 TunnelEntrance = Vector3.Zero;
        public static Vector3 TunnelExit = Vector3.Zero;


        #region Load() Function
        public void Load()
        {
            spells[SpellSlot.Q].SetSkillshot(0.25f, 65f, 1600f, false, SkillshotType.SkillshotLine);
            spells[SpellSlot.R].SetSkillshot(0.5f, 325, 1800, false, SkillshotType.SkillshotCircle);

            Entry.PrintChat("<font color=\"#FFCC66\" >Bard</font>");
        }
        #endregion

        #region CreateMenu() Function
        public void CreateMenu()
        {
            BardMenu = new Menu("Victorious Bard", "dz191.bard", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.GreenYellow);

            var OrbwalkerMenu = new Menu("Orbwalker", "dz191.bard.orbwalker");
            BardOrbwalker = new Orbwalking.Orbwalker(OrbwalkerMenu);
            BardMenu.AddSubMenu(OrbwalkerMenu);

            var TSMenu = new Menu("TargetSelector", "dz191.bard.ts");

            TargetSelector.AddToMenu(TSMenu);

            BardMenu.AddSubMenu(TSMenu);

            var comboMenu = new Menu("Combo", "dz191.bard.combo");
            {
                comboMenu.AddItem(new MenuItem("dz191.bard.combo.useq", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("dz191.bard.combo.usew", "Use W").SetValue(true));

                comboMenu.AddItem(new MenuItem("dz191.bard.combo.user", "R Manual Cast").SetValue(new KeyBind('B', KeyBindType.Press)));
                comboMenu.AddItem(new MenuItem("dz191.bard.combo.usercount", "Enemies for R").SetValue(new Slider(2, 5, 1)));

                BardMenu.AddSubMenu(comboMenu);
            }

            var harassMenu = new Menu("Harass", "dz191.bard.mixed");
            {
                var QMenu = new Menu("Q Targets (Stun Harass)", "dz191.bard.mixed");
                {
                    foreach (var hero in HeroManager.Enemies)
                    {
                        QMenu.AddItem(
                            new MenuItem(string.Format("dz191.bard.qtarget.{0}", hero.ChampionName.ToLower()),
                                hero.ChampionName).SetValue(true));

                    }
                }
                harassMenu.AddItem(new MenuItem("dz191.bard.mixed.useq", "Enable Q").SetValue(true));
                harassMenu.AddSubMenu(QMenu);
                BardMenu.AddSubMenu(harassMenu);
            }

            var fleeMenu = new Menu("Magical Journey", "dz191.bard.flee");
            {
                fleeMenu.AddItem(new MenuItem("dz191.bard.flee.e", "Enable").SetValue(true));
                fleeMenu.AddItem(new MenuItem("dz191.bard.flee.flee", "HotKey").SetValue(new KeyBind('T', KeyBindType.Press)));
                BardMenu.AddSubMenu(fleeMenu);
            }

            var miscMenu = new Menu("Misc", "dz191.bard.misc");
            {
                var DontWMenu = new Menu("W Settings", "dz191.bard.wtarget");
                {
                    foreach (var hero in HeroManager.Allies)
                    {
                        DontWMenu.AddItem(new MenuItem(string.Format("dz191.bard.wtarget.{0}", hero.ChampionName.ToLower()), hero.ChampionName).SetValue(true));
                    }
                    DontWMenu.AddItem(new MenuItem("dz191.bard.wtarget.healthpercent", "Health % for W").SetValue(new Slider(25, 1)));
                    miscMenu.AddSubMenu(DontWMenu);
                }

                miscMenu.AddItem(new MenuItem("dz191.bard.misc.sep1", "                     Q - Cosmic Binding          "));
                miscMenu.AddItem(new MenuItem("dz191.bard.misc.distance", "Calculation distance").SetValue(new Slider(250, 100, 450)));
                miscMenu.AddItem(new MenuItem("dz191.bard.misc.accuracy", "Accuracy").SetValue(new Slider(20, 1, 50)));
                miscMenu.AddItem(new MenuItem("dz191.bard.Drawings.Q", "Draw Q range").SetValue(true));
                BardMenu.AddSubMenu(miscMenu);
            }

            BardMenu.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Drawing.OnDraw += OnDraw;                       // by Jinx
        }
        #endregion

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("BardDoor_EntranceMinion") && sender.NetworkId == TunnelNetworkID)
            {
                TunnelNetworkID = -1;
                TunnelEntrance = Vector3.Zero;
                TunnelExit = Vector3.Zero;
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("BardDoor_EntranceMinion"))
            {
                TunnelNetworkID = sender.NetworkId;
                TunnelEntrance = sender.Position;
            }

            if (sender.Name.Contains("BardDoor_ExitMinion"))
            {
                TunnelExit = sender.Position;
            }
        }

        static void Game_OnUpdate(EventArgs args)
        {
            var ComboTarget = TargetSelector.GetTarget(spells[SpellSlot.Q].Range / 1.3f, TargetSelector.DamageType.Magical);        // 왜 1.3을 나눌까? 정확도 때문에?
            var OKTWTarget = TargetSelector.GetTarget(spells[SpellSlot.Q].Range, TargetSelector.DamageType.Magical);
            switch (BardOrbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:

                    if (spells[SpellSlot.Q].IsReady() && GetItemValue<bool>(string.Format("dz191.bard.{0}.useq", BardOrbwalker.ActiveMode.ToString().ToLower())) &&
                        ComboTarget.IsValidTarget())
                    {
                        if(!QCastOKTW(OKTWTarget, OKTWPrediction.HitChance.VeryHigh))           // 일단 확정 스턴 기술 시도, 실패시 기존 로직 진행
                            HandleQ(ComboTarget);                                               
                    }

                    if (GetItemValue<bool>(string.Format("dz191.bard.{0}.usew", BardOrbwalker.ActiveMode.ToString().ToLower())))
                    {
                        HandleW();
                    }

                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    if (spells[SpellSlot.Q].IsReady() && GetItemValue<bool>(string.Format("dz191.bard.{0}.useq", BardOrbwalker.ActiveMode.ToString().ToLower())) &&
                        OKTWTarget.IsValidTarget() && GetItemValue<bool>(string.Format("dz191.bard.qtarget.{0}", OKTWTarget.ChampionName.ToLower())))
                    {
                        QCastOKTW(OKTWTarget, OKTWPrediction.HitChance.VeryHigh);
                    }
                    break;
            }

            if (GetItemValue<KeyBind>("dz191.bard.flee.flee").Active)   CastE();

            if (GetItemValue<KeyBind>("dz191.bard.combo.user").Active)  CastR();

        }

        public static bool QCastOKTW(Obj_AI_Hero target, OKTWPrediction.HitChance hitChance)
        {
            var spell = spells[SpellSlot.Q];
            var OKTWPlayer = ObjectManager.Player;

            OKTWPrediction.SkillshotType CoreType2 = OKTWPrediction.SkillshotType.SkillshotLine;
            bool aoe2 = true;

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
                // 확정스턴 확률 기대: 주변에 벽이 있거나, 2명이상 맞거나...
                if (CheckWall(target) || poutput2.AoeTargetsHitCount > 1)
                    return spell.Cast(poutput2.CastPosition);
            }
            return false;
        }

        private static bool CheckWall(Obj_AI_Hero target)
        {
            if (!target.IsValidTarget(spells[SpellSlot.Q].Range)) return false;
            if ((target.Distance(ObjectManager.Player)) < (spells[SpellSlot.Q].Range / 2)) return false;

            var prediction = spells[SpellSlot.Q].GetPrediction(target);
            var pushdistance = spells[SpellSlot.Q].Range - target.Distance(ObjectManager.Player);
            if (pushdistance < 20) pushdistance = 20;
            var finalPosition = prediction.UnitPosition.Extend(ObjectManager.Player.Position, -pushdistance);

            if (finalPosition.IsWall())
                return true;
            else
                for (var i = 1; i < pushdistance; i += 20)
                {
                    var loc3 = prediction.UnitPosition.Extend(ObjectManager.Player.Position, -i);
                    if (loc3.IsWall())
                    {
                        return true;
                    }
                }

            return false;
        }

        private static void CastR()
        {
            if (!spells[SpellSlot.R].IsReady()) return;

            var targetR = TargetSelector.GetTarget(spells[SpellSlot.R].Range, TargetSelector.DamageType.Magical);
            int EnemiesCount = BardMenu.Item("dz191.bard.combo.usercount").GetValue<Slider>().Value;
            if (targetR.IsValidTarget(spells[SpellSlot.R].Range) && GetRCastEnemies(targetR) >= EnemiesCount)
            {
                RCastOKTW(targetR, OKTWPrediction.HitChance.VeryHigh, EnemiesCount);
            }
        }

        public static bool RCastOKTW(Obj_AI_Hero target, OKTWPrediction.HitChance hitChance, int minCount)
        {
            var spell = spells[SpellSlot.R];
            var OKTWPlayer = ObjectManager.Player;

            OKTWPrediction.SkillshotType CoreType2 = OKTWPrediction.SkillshotType.SkillshotCircle;
            bool aoe2 = true;

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
                if (poutput2.AoeTargetsHitCount >= minCount)
                    return spell.Cast(poutput2.CastPosition);
            }
            return false;
        }

        private static int GetRCastEnemies(Obj_AI_Hero target)
        {
            int Enemies = 0;
            foreach (Obj_AI_Hero enemys in ObjectManager.Get<Obj_AI_Hero>())
            {
                var pred = spells[SpellSlot.R].GetPrediction(enemys, true);
                if (pred.Hitchance >= HitChance.High && !enemys.IsMe && enemys.IsEnemy && Vector3.Distance(ObjectManager.Player.Position, pred.UnitPosition) <= spells[SpellSlot.R].Range)
                {
                    Enemies = Enemies + 1;
                }
            }
            return Enemies;
        }

        private static void CastE()
        {
            if ((IsOverWall(ObjectManager.Player.ServerPosition, Game.CursorPos)        // 챔피언 위치와 커서포지션 사이가 벽이고...
                && GetWallLength(ObjectManager.Player.ServerPosition, Game.CursorPos) >= 250f) && (spells[SpellSlot.E].IsReady()    // 벽 길이가 250 이상이며
                || (TunnelNetworkID != -1 && (ObjectManager.Player.ServerPosition.Distance(TunnelEntrance) < 250f))))               // 아니면 사용자가 이미 터널을 만들었을 경우
            {
                MoveToLimited(GetFirstWallPoint(ObjectManager.Player.ServerPosition, Game.CursorPos));      // 터널 입구(혹은 예정지)까지 강제로 이동함.
            }
            else
            {
                MoveToLimited(Game.CursorPos);      // 아니면 커서 위치로 그냥 이동만 함.
            }

            if (GetItemValue<bool>("dz191.bard.flee.e"))        // Magical Journey Enable
            {
                var dir = ObjectManager.Player.ServerPosition.To2D() + ObjectManager.Player.Direction.To2D().Perpendicular() * (ObjectManager.Player.BoundingRadius * 2.5f);
                var Extended = Game.CursorPos;
                if (dir.IsWall() && IsOverWall(ObjectManager.Player.ServerPosition, Extended)
                    && spells[SpellSlot.E].IsReady()
                    && GetWallLength(ObjectManager.Player.ServerPosition, Extended) >= 250f)
                {
                    // 이미 터널 위치로 이동해있으니, 타겟 거리로 이동함.
                    spells[SpellSlot.E].Cast(Extended);
                }
            }
        }

        private static void HandleQ(Obj_AI_Hero comboTarget)
        {
            var QPrediction = spells[SpellSlot.Q].GetPrediction(comboTarget);

            if (QPrediction.Hitchance >= HitChance.High)
            {
                var QPushDistance = GetItemValue<Slider>("dz191.bard.misc.distance").Value;     // 250
                var QAccuracy = GetItemValue<Slider>("dz191.bard.misc.accuracy").Value;         // 20
                var PlayerPosition = ObjectManager.Player.ServerPosition;

                var BeamStartPositions = new List<Vector3>()
                    {
                        QPrediction.CastPosition,
                        QPrediction.UnitPosition,
                        comboTarget.ServerPosition,
                        comboTarget.Position
                    };

                if (comboTarget.IsDashing())
                {
                    BeamStartPositions.Add(comboTarget.GetDashInfo().EndPos.To3D());
                }

                var PositionsList = new List<Vector3>();
                var CollisionPositions = new List<Vector3>();

                foreach (var position in BeamStartPositions)
                {
                    var collisionableObjects = spells[SpellSlot.Q].GetCollision(position.To2D(),
                        new List<Vector2>() { position.Extend(PlayerPosition, -QPushDistance).To2D() });

                    if (collisionableObjects.Any())
                    {
                        if (collisionableObjects.Any(h => h is Obj_AI_Hero) &&
                            (collisionableObjects.All(h => h.IsValidTarget())))
                        {
                            spells[SpellSlot.Q].Cast(QPrediction.CastPosition);
                            break;
                        }

                        for (var i = 0; i < QPushDistance; i += (int)comboTarget.BoundingRadius)
                        {
                            CollisionPositions.Add(position.Extend(PlayerPosition, -i));
                        }
                    }

                    for (var i = 0; i < QPushDistance; i += (int)comboTarget.BoundingRadius)
                    {
                        PositionsList.Add(position.Extend(PlayerPosition, -i));
                    }
                }

                if (PositionsList.Any())
                {
                    // We don't want to divide by 0 Kappa
                    var WallNumber = PositionsList.Count(p => p.IsWall()) * 1.3f;
                    var CollisionPositionCount = CollisionPositions.Count;
                    var Percent = (WallNumber + CollisionPositionCount) / PositionsList.Count;
                    var AccuracyEx = QAccuracy / 100f;
                    if (Percent >= AccuracyEx)
                    {
                        spells[SpellSlot.Q].Cast(QPrediction.CastPosition);
                    }

                }
            }
            else if (QPrediction.Hitchance == HitChance.Collision)
            {
                var QCollision = QPrediction.CollisionObjects;
                if (QCollision.Count == 1)
                {
                    spells[SpellSlot.Q].Cast(QPrediction.CastPosition);
                }
            }
        }


        private static void HandleW()
        {
            if (ObjectManager.Player.IsRecalling() || ObjectManager.Player.InShop() || !spells[SpellSlot.W].IsReady())
            {
                return;
            }

            // 우선순위는 나에게 준다.
            if (ObjectManager.Player.HealthPercent <= GetItemValue<Slider>("dz191.bard.wtarget.healthpercent").Value)
            {
                var castPosition = ObjectManager.Player.ServerPosition.Extend(Game.CursorPos, 65);
                spells[SpellSlot.W].Cast(castPosition);
                return;
            }

            var LowHealthAlly = HeroManager.Allies
                .Where(ally => ally.IsValidTarget(spells[SpellSlot.W].Range, false)
                    && ally.HealthPercent <= GetItemValue<Slider>("dz191.bard.wtarget.healthpercent").Value
                    && GetItemValue<bool>(string.Format("dz191.bard.wtarget.{0}", ally.ChampionName.ToLower())))
                //.OrderBy(TargetSelector.GetPriority)
                .OrderBy(ally => ally.Health)
                .FirstOrDefault();

            if (LowHealthAlly != null)
            {
                var movementPrediction = Prediction.GetPrediction(LowHealthAlly, 0.25f);
                spells[SpellSlot.W].Cast(movementPrediction.UnitPosition);
            }
        }


        private static T GetItemValue<T>(string item)
        {
            return BardMenu.Item(item).GetValue<T>();
        }

        private static bool IsOverWall(Vector3 start, Vector3 end)
        {
            double distance = Vector3.Distance(start, end);
            for (uint i = 0; i < distance; i += 10)
            {
                var tempPosition = start.Extend(end, i).To2D();
                if (tempPosition.IsWall())
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 GetFirstWallPoint(Vector3 start, Vector3 end)
        {
            double distance = Vector3.Distance(start, end);
            for (uint i = 0; i < distance; i += 10)
            {
                var tempPosition = start.Extend(end, i);
                if (tempPosition.IsWall())
                {
                    return tempPosition.Extend(start, -35);
                }
            }

            return Vector3.Zero;
        }

        private static float GetWallLength(Vector3 start, Vector3 end)
        {
            double distance = Vector3.Distance(start, end);
            var firstPosition = Vector3.Zero;
            var lastPosition = Vector3.Zero;

            for (uint i = 0; i < distance; i += 10)
            {
                var tempPosition = start.Extend(end, i);
                if (tempPosition.IsWall() && firstPosition == Vector3.Zero)
                {
                    firstPosition = tempPosition;
                }
                lastPosition = tempPosition;
                if (!lastPosition.IsWall() && firstPosition != Vector3.Zero)
                {
                    break;
                }
            }

            return Vector3.Distance(firstPosition, lastPosition);
        }

        public static void MoveToLimited(Vector3 where)
        {
            if (Environment.TickCount - LastMoveC < 80)
            {
                return;
            }

            LastMoveC = Environment.TickCount;

            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, where);
        }

        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (GetItemValue<bool>("dz191.bard.Drawings.Q"))
                {
                    if (spells[SpellSlot.Q].Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, (spells[SpellSlot.Q].Range), System.Drawing.Color.White, 3);
                    }
                }

                if (GetItemValue<bool>("dz191.bard.mixed.useq")) DrawCheckWall();           // Q Stun Harras

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }


        private static void DrawCheckWall()
        {

            var nTarget = TargetSelector.GetTarget(spells[SpellSlot.Q].Range, TargetSelector.DamageType.Magical);
            var prediction = spells[SpellSlot.Q].GetPrediction(nTarget);
            var pushdistance = spells[SpellSlot.Q].Range - nTarget.Distance(ObjectManager.Player);
            var finalPosition = prediction.UnitPosition.Extend(ObjectManager.Player.Position, -pushdistance);

            if (finalPosition.IsWall())
            {
                Render.Circle.DrawCircle(finalPosition, 50, System.Drawing.Color.Yellow, 8);
                return;
            }
            else
                for (var i = 1; i < pushdistance; i += 20)
                {
                    var loc3 = prediction.UnitPosition.Extend(ObjectManager.Player.Position, -i);
                    if (loc3.IsWall())
                    {
                        Render.Circle.DrawCircle(loc3, 50, System.Drawing.Color.Red, 8);
                        return;
                    }
                }

        }
   
    }

}
