using UnityEngine;

namespace Reverie.Game
{
    public class MoveState : GroundState
    {
        public MoveState(PlayerStateMachine machine) : base(machine) { }
        
        public override void OnEnter()
        {
            base.OnEnter();
            Anim.Play(PlayerAnim.Move, 0, 0f);
        }

        public override void OnUpdate()
        {
            if (Mathf.Abs(Input.MoveAxis.x) < 0.01f)
            {
                Machine.ChangeState(PlayerStateId.Ground_Idle);
                return;
            }

            Move(Ctx.MoveSpeed);
        }
    }
}
