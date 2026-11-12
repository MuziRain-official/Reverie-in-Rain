using Reverie.Framework;
using UnityEngine;


namespace Reverie.Game
{
    public class PlayerContext
    {
        public Transform   Transform;
        public Rigidbody2D Rb;
        public Animator    Anim;
        public InputReader Input;

        public float MoveSpeed = 6f;
    }
}