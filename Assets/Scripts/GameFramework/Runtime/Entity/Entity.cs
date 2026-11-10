using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 实体基类：只负责激活与休眠。
    /// 血量、交互、受伤等能力由子类和接口各自实现，不放这里。
    /// </summary>
    public abstract class Entity : MonoBehaviour
    {
        private bool _isActive = true;
        public bool IsActive
        {
            get { return _isActive; }
        }

        /// <summary>激活：恢复逻辑更新与触发器。</summary>
        public void Active()
        {
            if (_isActive) return;
            _isActive = true;
            enabled = true;
            OnActive();
        }

        /// <summary>休眠：停止逻辑更新与触发器，碰撞体与渲染保留。</summary>
        public void Deactive()
        {
            if (!_isActive) return;
            _isActive = false;
            OnDeactive();   // 先回调再关，子类此时还能干活
            enabled = false;
        }

        protected virtual void OnActive() { }
        protected virtual void OnDeactive() { }
    }
}