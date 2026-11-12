using Reverie.Framework;
using UnityEngine;

namespace Reverie.Game
{
    public class PlayerStateBase : FsmState<PlayerStateId>
    {
        protected readonly PlayerStateMachine Machine;
        protected PlayerContext Ctx;
        protected InputReader Input;
        protected Animator Anim;
        protected Rigidbody2D Rb;

        protected PlayerStateBase(PlayerStateMachine machine)
        {
            Machine = machine;
            Ctx = machine.Ctx;
            Input = Ctx.Input;
            Anim = Ctx.Anim;
            Rb = Ctx.Rb;
        }
        
    }
}