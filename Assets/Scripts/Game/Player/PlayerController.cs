using Reverie.Framework;
using UnityEngine;

namespace Reverie.Game
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Animator    anim;
        [SerializeField] private Rigidbody2D rb;

        private PlayerStateMachine _fsm;

        void Awake()
        {
            // 第一步不做重力，用代码直接控速度，避免物理干扰
            if (rb != null)
            {
                rb.gravityScale   = 0f;
                rb.freezeRotation = true;
            }

            var ctx = new PlayerContext
            {
                Transform = transform,
                Rb        = rb,
                Anim      = anim,
                Input     = InputReader.Instance,
                MoveSpeed = 6f,
            };

            _fsm = new PlayerStateMachine(ctx);
        }

        void Start()
        {
            _fsm.Start(PlayerStateId.Ground_Idle);
        }

        void Update()
        {
            _fsm.Update();
        }
    }
}
