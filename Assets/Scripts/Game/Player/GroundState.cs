using Reverie.Framework;

namespace Reverie.Game
{
    public abstract class GroundState : PlayerStateBase
    {
        protected GroundState(PlayerStateMachine machine) : base(machine) { }

        public override void OnEnter()
        {
            var v = Rb.linearVelocity;
            v.y = 0f;                 // 贴地
            Rb.linearVelocity = v;
        }

        protected void Move(float speed)
        {
            var v = Rb.linearVelocity;
            v.x = Input.MoveAxis.x * speed;
            Rb.linearVelocity = v;
        }
        
        public override void OnExit()
        {
            // if (Input.MoveX != 0f)
            // {
            //     Machine.ChangeState(PlayerStateId.Ground_Move);
            //     return true;
            // }
            // Machine.ChangeState(PlayerStateId.Ground_Idle);
        }
    }
}