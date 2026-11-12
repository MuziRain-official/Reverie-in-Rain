using UnityEngine;
using UnityEngine.InputSystem;

namespace Reverie.Framework
{
    /// <summary>
    /// 输入：封装 project-wide Input Actions，业务只读逻辑动作，不直接碰 InputAction。
    ///
    ///   Vector2 move = InputReader.Instance.MoveAxis;      // 移动轴，-1 ~ 1
    ///   bool jump    = InputReader.Instance.JumpPressed;   // 本帧是否按下跳跃
    ///   InputReader.Instance.Lock();                        // 切房间 / 过场时锁移动
    ///   InputReader.Instance.Unlock();
    ///
    /// 动作配置在 Assets/InputSystem_Actions.inputactions 的 Player 表里。
    /// </summary>
    public class InputReader : Singleton<InputReader>
    {
        private const string MoveAction = "Player/Move";
        private const string JumpAction = "Player/Jump";

        private InputAction _move;
        private InputAction _jump;

        // 锁定层数：多个系统可能同时锁输入（过场 + 暂停），全部解锁后才恢复
        private int _lockCount;

        public bool Locked => _lockCount > 0;

        public Vector2 MoveAxis => Locked ? Vector2.zero : _move.ReadValue<Vector2>();

        // 跳跃不受锁影响：标题页要锁住移动，但仍要能按跳跃键开始游戏
        public bool JumpPressed => _jump.WasPressedThisFrame();
        public bool JumpHeld => _jump.IsPressed();

        public void Lock() => _lockCount++;
        public void Unlock() => _lockCount = Mathf.Max(0, _lockCount - 1);

        protected override void OnInit()
        {
            var asset = InputSystem.actions;
            if (asset == null)
            {
                Log.Error("未配置 project-wide Input Actions，输入模块不可用", LogChannel.Core);
                return;
            }

            _move = asset.FindAction(MoveAction, throwIfNotFound: false);
            _jump = asset.FindAction(JumpAction, throwIfNotFound: false);
            if (_move == null || _jump == null)
            {
                Log.Error($"Input Actions 缺少动作 {MoveAction} 或 {JumpAction}", LogChannel.Core);
                return;
            }

            _move.Enable();
            _jump.Enable();
        }

        protected override void OnRelease()
        {
            _move?.Disable();
            _jump?.Disable();
        }
    }
}
