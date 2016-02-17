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
                menu = new Menu("Support Mode", "SupportMode", true);
                menu.AddItem(new MenuItem("enabled", "Enabled").SetValue(true)).Permashow(true, "Support Mode");
                //menu.AddItem(new MenuItem("enabled", "Enabled").SetValue(new KeyBind('8', KeyBindType.Toggle, true))).Permashow(true, "Support Mode");
                menu.AddToMainMenu();
                Orbwalking.BeforeAttack += BeforeAttack;

                PrintChat("<font color=\"#FFFFFF\" >Version " + Assembly.GetExecutingAssembly().GetName().Version + "</font>");

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
                    var alliesinrange = HeroManager.Allies.Count(x => !x.IsMe && x.Distance(Player) <= 1500);
                    if (alliesinrange > 0)
                    {
                        args.Process = false;
                    }
                }
            }
        }
        #endregion
    }
}