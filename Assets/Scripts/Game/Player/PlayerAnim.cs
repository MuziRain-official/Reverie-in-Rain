namespace Reverie.Game
{
    /// <summary>
    /// 动画状态名：与 Assets/Art/ThirdParty_PixelPacks/乔安娜/Player.controller 里的 state 名一致。
    /// Animator.Play 按 state 名查找，写错是静默失败（只打一条 Error 日志），所以统一放这里。
    /// </summary>
    public static class PlayerAnim
    {
        public const string Idle = "乔安娜_待机";
        public const string Move = "乔安娜_行走";
    }
}
