namespace JinxsSupport
{
    using LeagueSharp.Common;

    /// <summary>
    ///     확장 챔피언 인터페이스
    /// </summary>
    internal interface IPlugin
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Creates the menu.
        /// </summary>
        /// <param name="rootMenu">The root menu.</param>
        /// <returns></returns>
        void CreateMenu();

        /// <summary>
        ///     Loads this instance.
        /// </summary>
        void Load();

        #endregion
    }
}