using System;
using System.Collections;
using UnityEngine;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Reverie.Framework
{
    /// <summary>
    /// 场景管理器：只负责切大地图。房间切换在同一张地图内完成，与本模块无关。
    ///
    ///   Scene.Instance.LoadMap("Area2", () => { });                              // 不管进度
    ///   Scene.Instance.LoadMap("Area2", p => bar.fillAmount = p, () => { });     // 带进度
    ///
    /// 黑屏和进度条由业务层自己用 UIManager 拼，本模块只往外抛进度。
    /// </summary>
    public class Scene : MonoSingleton<Scene>
    {
        // 正在加载时为 true，挡住重复请求：Unity 的异步队列是串行的，
        // 连发两个会变成「点了没反应，过一会儿连切两次」。
        private bool _loading;

        /// <summary>当前活动场景名，如 "Area2"。</summary>
        public string CurrentMap => SceneManager.GetActiveScene().name;

        /// <summary>切大地图，不管进度。</summary>
        public void LoadMap(string mapName, Action onDone)
        {
            LoadMap(mapName, null, onDone);
        }

        /// <summary>
        /// 切大地图，带进度。onProgress 收到 0~1 的换算值，最后一次必为 1。
        /// 失败时只回调 onDone，业务层用 CurrentMap 对比即可判断成败。
        /// </summary>
        public void LoadMap(string mapName, Action<float> onProgress, Action onDone)
        {
            if (_loading)
            {
                Log.Warn($"上一张地图还在加载，忽略对 {mapName} 的请求", LogChannel.Core);
                return;
            }

            StartCoroutine(LoadCoroutine(mapName, onProgress, onDone));
        }

        private IEnumerator LoadCoroutine(string mapName, Action<float> onProgress, Action onDone)
        {
            _loading = true;

            yield return null;   // 等一帧，否则同一帧读到的 progress 还是 0

            // 默认 Single 模式：旧场景由 Unity 自动卸载，加载完自动成为活动场景
            AsyncOperation op = SceneManager.LoadSceneAsync(mapName);
            if (op == null)
            {
                Log.Error($"场景 {mapName} 加载失败", LogChannel.Core);
                _loading = false;
                onDone?.Invoke();
                yield break;
            }

            while (!op.isDone)
            {
                // Unity 的 progress 在激活前最多到 0.9，换算成 0~1；Clamp01 防止超 1
                onProgress?.Invoke(Mathf.Clamp01(op.progress / 0.9f));
                yield return null;
            }

            onProgress?.Invoke(1f);   // 进度条必须收在 1，否则停在 90%
            _loading = false;
            onDone?.Invoke();
        }
    }
}