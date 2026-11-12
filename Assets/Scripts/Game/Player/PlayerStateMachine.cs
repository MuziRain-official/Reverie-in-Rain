using Reverie.Framework;

namespace Reverie.Game
{
    public class PlayerStateMachine : FsmBase<PlayerStateId>
    {
        public PlayerContext Ctx;
        public PlayerStateMachine(PlayerContext ctx)
        {
            Ctx = ctx;
            
            Register(PlayerStateId.Ground_Idle,new IdleState(this));
            Register(PlayerStateId.Ground_Move,new MoveState(this));
            
        }
        
    }
}