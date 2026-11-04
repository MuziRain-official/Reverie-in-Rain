using System.Collections.Generic;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 对象池：按路径（Resource 路径）缓存 GameObject，减少频繁 Instantiate / Destroy 的开销。
    ///
    ///   var fx = Pool.Instance.Get("Prefabs/FX/Hit", pos, rot);
    ///   Pool.Instance.Release(fx);      // 归还，不用传路径
    ///   Pool.Instance.Clear();          // 切房间时统一清空
    /// </summary>
    public class Pool : MonoSingleton<Pool>
    {
        // 路径 -> 空闲对象
        private readonly Dictionary<string, Stack<GameObject>> _idle = new();

        // 使用中的对象 -> 它属于哪个路径（Release 时不用再传路径，也用来识别重复归还）
        private readonly Dictionary<GameObject, string> _active = new();

        // ------------------- 取出 -------------------

        public GameObject Get(string path) => Get(path, Vector3.zero, Quaternion.identity);

        public GameObject Get(string path, Vector3 position, Quaternion rotation)
        {
            GameObject obj = Pop(path);
            if (obj == null) obj = Create(path);
            if (obj == null) return null;    // 资源加载失败，Resource 内部已记录错误

            obj.transform.SetParent(null);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);

            _active[obj] = path;
            return obj;
        }

        // ------------------- 归还 -------------------

        public void Release(GameObject obj)
        {
            if (obj == null) return;
            // 不在使用中说明已归还过或不归本池所有，忽略（防重复归还）
            if (!_active.TryGetValue(obj, out string path)) return;
            _active.Remove(obj);

            Reset(obj);

            if (!_idle.TryGetValue(path, out var stack))
                _idle[path] = stack = new Stack<GameObject>();
            stack.Push(obj);
        }

        // ------------------- 清空 -------------------

        /// <summary>销毁所有使用中和空闲的对象（切房间时调用）。</summary>
        public void Clear()
        {
            foreach (var obj in _active.Keys)
                if (obj != null) Destroy(obj);
            _active.Clear();

            foreach (var stack in _idle.Values)
                foreach (var obj in stack)
                    if (obj != null) Destroy(obj);
            _idle.Clear();
        }

        protected override void OnRelease() => Clear();

        // ------------------- 内部 -------------------

        private GameObject Pop(string path)
        {
            if (!_idle.TryGetValue(path, out var stack)) return null;

            while (stack.Count > 0)
            {
                GameObject obj = stack.Pop();
                if (obj != null) return obj;    // 场景卸载后 Unity 会留下空引用，跳过
            }
            return null;
        }

        private GameObject Create(string path)
        {
            GameObject prefab = Resource.Instance.Load<GameObject>(path);
            if (prefab == null) return null;

            GameObject obj = Instantiate(prefab);
            obj.transform.SetParent(transform);   // 先挂在池对象下，非激活状态
            obj.SetActive(false);
            return obj;
        }

        /// <summary>归池时复原：停用 + 复位变换与物理状态。</summary>
        private void Reset(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }
}
