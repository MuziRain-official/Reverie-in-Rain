namespace Reverie.Game
{
    using UnityEngine;

    public class IdleState : GroundState
    {
        public IdleState(PlayerStateMachine machine) : base(machine) { }
        
        public override void OnEnter()
        {
            base.OnEnter();
            Anim.Play(PlayerAnim.Idle, 0, 0f);
            Move(0f);
        }

        public override void OnUpdate()
        {
            if (Mathf.Abs(Input.MoveAxis.x) > 0.01f)
                Machine.ChangeState(PlayerStateId.Ground_Move);
        }
    }
}