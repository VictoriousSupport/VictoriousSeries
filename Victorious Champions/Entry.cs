/// <summary>
///  [Version: 6.0.1.2]
///     1. Jinx's Support: 코드 통합작업 진행
///        - Thresh 실패 (기존 쓰레쉬는 서브클래스 기반이 아니라 실패, OKTW은 너무 복잡해서 실패)
///        - 향후 Thresh 어플은 다른 어플 도입으로 진행할 예정
///        - OKTW은 그냥 기존 것을 그대로 사용하던가, 일부 수정할 부분만 수정하여 사용할 예정 (브랜드/벨코즈 R로직 변경 필요)
///        
///  [Version: 6.0.1.4]
///     1. Support Mode 포함 (기본 거리 1500)
///     
///  [Version: 6.0.1.5]
///     1. Lulu 추가 
///     
/// </summary>

namespace JinxsSupport
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Security.Permissions;

    using LeagueSharp;
    using LeagueSharp.Common;

    internal enum Spells
    {
        Q,
        W,
        E,
        R
    }
    /// <summary>
    ///     어셈블리 시작점, 챔피언 이름을 얻어 해당 플러그인을 적재함.
    /// </summary>

    internal class Entry
    {
        #region Delegates
        internal delegate T ObjectActivator<out T>(params object[] args);
        #endregion

        #region Public Properties
        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        public static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }
        /// <summary>
        ///     Gets script version
        /// </summary>
        /// <value>
        ///     The script version
        /// </value>
        public static string ScriptVersion
        {
            get
            {
                return typeof(Entry).Assembly.GetName().Version.ToString();
            }
        }
        private static Menu menu;      // Support Mode

        public enum TargetingMode
        {
            AutoPriority,
            LowHP,
            MostAD,
            MostAP,
            Closest,
            NearMouse,
            LessAttack,
            LessCast,
            MostStack
        }
        #endregion

        #region Public Methods and Operators

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        public static ObjectActivator<T> GetActivator<T>(ConstructorInfo ctor)
        {
            var paramsInfo = ctor.GetParameters();
            var param = Expression.Parameter(typeof(object[]), "args");
            var argsExp = new Expression[paramsInfo.Length];

            for (var i = 0; i < paramsInfo.Length; i++)
            {
                var paramCastExp = Expression.Convert(
                    Expression.ArrayIndex(param, Expression.Constant(i)),
                    paramsInfo[i].ParameterType);

                argsExp[i] = paramCastExp;
            }

            return
                (ObjectActivator<T>)
                Expression.Lambda(typeof(ObjectActivator<T>), Expression.New(ctor, argsExp), param).Compile();
        }

        /* <챔피언 어셈 확장하기>
        1. 일단 독립 프로젝트로 만들던가, 첨부터 가져오던가 하고...
        2. 기존 프로젝트의 OnLoad() 함수이름 변경: public void Load() 
        3. 기존 프로젝트의 메뉴제작 함수 이름 변경, 혹은 신규 생성: public void CreateMenu()
        4. 기존 프로젝트의 internal class 이름 변경: internal class <Champion Name> : IPlugin
        5. OKTW 등 공통 모듈은 Root 에 namespace 변경하여 포함: namespace JinxsSupport {
        * 이렇게 하면, 기존에 독립적으로 동작하던 어셈을 통합버전으로 합칠 수 있음. 가장 간단한 형태의 멀티 어셈!
        * Support Mode 등 공통 모드는 중앙에 포함시키기...
        */
        public static void OnLoad(EventArgs args)
        {
            try
            {
                menu = new Menu("Support Mode", "SupportMode", true).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.GreenYellow);
                menu.AddItem(new MenuItem("enabled", "Enabled").SetValue(true)).Permashow(true, "Support Mode");

                PrintChat("<font color=\"#FFFFFF\" >Version " + Assembly.GetExecutingAssembly().GetName().Version + "</font>");

#if true       // 동작 확인 필요
                Menu rootMenu = Menu.GetMenu("LeagueSharp.Common", "LeagueSharp.Common");
                //rootMenu.Item("TargetingMode").GetValue<StringList>().SelectedIndex // 해당값을 읽어올때
                if (rootMenu != null)
                {
                    string strMode = Enum.GetName(typeof(TargetingMode), rootMenu.Item("TargetingMode").GetValue<StringList>().SelectedIndex);
                    PrintChat(string.Format("Current Targeting Mode: {0}", strMode));

                    if (Player.CharData.BaseSkinName.ToLower() == "sivir" ||
                        Player.CharData.BaseSkinName.ToLower() == "tristana")
                    {
                        if (rootMenu.Item("TargetingMode").GetValue<StringList>().SelectedIndex != (int)TargetingMode.LessAttack)
                        {
                            rootMenu.Item("TargetingMode").SetValue(new StringList(Enum.GetNames(typeof(TargetingMode)), (int)TargetingMode.LessAttack));
                            PrintChat(string.Format("Targeting Mode Changed: {0}", Enum.GetName(typeof(TargetingMode), rootMenu.Item("TargetingMode").GetValue<StringList>().SelectedIndex)));
                        }
                    }
                    else if (Player.CharData.BaseSkinName.ToLower() == "bard" ||
                            Player.CharData.BaseSkinName.ToLower() == "thresh" ||
                            Player.CharData.BaseSkinName.ToLower() == "alistar" ||
                            Player.CharData.BaseSkinName.ToLower() == "lulu" ||
                            Player.CharData.BaseSkinName.ToLower() == "nami" ||
                            Player.CharData.BaseSkinName.ToLower() == "soraka")
                    {
                        if (rootMenu.Item("TargetingMode").GetValue<StringList>().SelectedIndex != (int)TargetingMode.LessCast)
                        {
                            rootMenu.Item("TargetingMode").SetValue(new StringList(Enum.GetNames(typeof(TargetingMode)), (int)TargetingMode.LessCast));
                            PrintChat(string.Format("Setup Targeting Mode: {0}", Enum.GetName(typeof(TargetingMode), rootMenu.Item("TargetingMode").GetValue<StringList>().SelectedIndex)));
                        }
                        menu.AddToMainMenu();
                        Orbwalking.BeforeAttack += BeforeAttack;
                    }
                    else
                    {
                        rootMenu.Item("TargetingMode").SetValue(new StringList(Enum.GetNames(typeof(TargetingMode)), (int)TargetingMode.AutoPriority));
                        PrintChat(string.Format("Default Targeting Mode: {0}", Enum.GetName(typeof(TargetingMode), rootMenu.Item("TargetingMode").GetValue<StringList>().SelectedIndex)));
                    }
                }
#endif

                var plugins =
                    Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .Where(x => typeof(IPlugin).IsAssignableFrom(x) && !x.IsInterface)
                        .Select(x => GetActivator<IPlugin>(x.GetConstructors().First())(null));

                foreach (var plugin in plugins)
                {
                    if (plugin.ToString().ToLower().Contains(Player.CharData.BaseSkinName.ToLower()))
                    {
                        plugin.Load();                      // 각 인터페이스 클래스(챔피언)는 Load() 함수 필요
                        plugin.CreateMenu();                // 각 인터페이스 클래스(챔피언)는 CreateMenu() 항목을 가져야 함.
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        public static bool OKTWCast_SebbyLib(Spell QWER, Obj_AI_Hero target, bool MultiTarget)//, SebbyLib.Prediction.HitChance hitChance)
        {
            SebbyLib.Prediction.SkillshotType CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotLine;
            bool aoe2 = false;

            if (QWER.Type == SkillshotType.SkillshotCircle)
            {
                CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotCircle;
                aoe2 = true;
            }

            if (QWER.Width > 80 && !QWER.Collision)         // 기술의 폭이 80 이상이고, 충돌이 일어나지 않는 기술이라면...
                aoe2 = true;

            if (MultiTarget && !QWER.Collision)             // 내가 이건 멀티타겟이라고 정의해버린 경우
                aoe2 = true;

            var predInput2 = new SebbyLib.Prediction.PredictionInput
            {
                Aoe = aoe2,
                Collision = QWER.Collision,
                Speed = QWER.Speed,
                Delay = QWER.Delay,
                Range = QWER.Range,
                From = HeroManager.Player.ServerPosition,
                Radius = QWER.Width,
                Unit = target,
                Type = CoreType2
            };
            var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(predInput2);

            if (QWER.Speed != float.MaxValue && SebbyLib.OktwCommon.CollisionYasuo(HeroManager.Player.ServerPosition, poutput2.CastPosition))
                return false;

            if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.VeryHigh)
            {
                return QWER.Cast(poutput2.CastPosition);
            }
            else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 && poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High && MultiTarget)
            {
                return QWER.Cast(poutput2.CastPosition);
            }

            return false;
        }

        /// <summary>
        ///     Prints to the local chat.
        /// </summary>
        /// <param name="msg">The message.</param>
        public static void PrintChat(string msg)
        {
            Game.PrintChat("<font color='#3492EB'>Victorious:</font> <font color='#FFFFFF'>" + msg + "</font>");
        }

        /// <summary>
        /// 아래와 같은 설정으로 해놓으면 X(Lasthit) 제외한 모든 미니언키가 불능이 됨. (C/V 키) 
        /// </summary>
        /// <param name="args"></param>
        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (menu.Item("enabled").GetValue<bool>())
            {
                var lhmode = Orbwalking.Orbwalker.Instances.Find(x => x.ActiveMode == Orbwalking.OrbwalkingMode.LastHit);
                if (lhmode != null) return;

                if (args.Target.Type == GameObjectType.obj_AI_Minion)
                {
                    args.Process = false;
                }
            }
        }
#endregion
    }
}