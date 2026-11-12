namespace Reverie.Game
{
    /// <summary>
    /// 玩家状态标识：FsmBase 字典的 key。
    /// 按五大类分组（地面 / 空中 / 攻击 / 受击 / 死亡），值一次性开满，
    /// 但第一步只实现并注册 Idle、Move 两个。
    /// </summary>
    public enum PlayerStateId
    {
        // —— 地面 ——
        Ground_Idle,
        Ground_Move,
        Ground_Crouch,

        // —— 空中 ——
        Air_Jump,
        Air_Fall,
        Air_Land,

        // —— 攻击 ——
        Attack_Light,
        Attack_Heavy,
        Attack_Combo,

        // —— 受击 ——
        Hurt,
        Knockback,

        // —— 死亡 ——
        Dead,
    }
}