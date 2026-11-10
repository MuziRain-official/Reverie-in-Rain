namespace Reverie.Framework
{
    /// <summary>
    /// 游戏流程的应用级状态。和房间无关：房间管地图单元，流程管「游戏现在处在哪个阶段」。
    /// </summary>
    public enum GameFlowState
    {
        /// <summary>启动：框架初始化已完成，准备进标题。</summary>
        Boot,

        /// <summary>标题：开始游戏 / 退出。</summary>
        Title,

        /// <summary>载入：切地图、等资源，完成后进游玩。</summary>
        Loading,

        /// <summary>游玩：正常操作中（按 ESC 进暂停）。</summary>
        Playing,

        /// <summary>暂停：锁输入，弹出暂停面板，再按 ESC 回游玩。</summary>
        Paused,
    }
}
