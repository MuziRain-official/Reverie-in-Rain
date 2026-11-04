namespace Reverie.Game
{
    /// <summary>
    /// 全局数据（GameContext）使用的 key 常量。
    /// 约定：同一个 key 只用一种类型。这里是业务层唯一认识游戏概念的地方。
    /// </summary>
    public static class GameKeys
    {
        // ---- 玩家属性 ----
        public const string PlayerHp    = "player.hp";
        public const string PlayerMaxHp = "player.maxHp";

        // ---- 地图 ----
        public const string CurrentRoom = "room.current";

        // ---- 已解锁技能 ----
        public const string SkillDoubleJump = "skill.doubleJump";
        public const string SkillDash       = "skill.dash";

        /// <summary>需要遍历全部技能时使用（存档、技能面板）。新增技能记得同步加这里。</summary>
        public static readonly string[] AllSkills =
        {
            SkillDoubleJump,
            SkillDash,
        };
    }
}
