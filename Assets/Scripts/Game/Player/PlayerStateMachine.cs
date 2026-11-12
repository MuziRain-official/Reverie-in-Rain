using Reverie.Framework;

namespace Reverie.Game
{
    public abstract class PlayerState : FsmState<PlayerStateId>
    {
        protected PlayerController Player { get; }
        protected PlayerState(PlayerController player)
        {
            Player = player;
            
        }
        
    }
}