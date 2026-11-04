using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 资源管理器：对 Resources 的薄封装，带一层缓存。
    ///
    ///   var prefab = Resource.Instance.Load&lt;GameObject&gt;("Prefabs/Player");
    ///   Resource.Instance.LoadAsync&lt;AudioClip&gt;("Audio/Bgm/Title", clip => ...);
    ///   var all = Resource.Instance.LoadAll&lt;Sprite&gt;("Sprites/Tiles");
    ///   Resource.Instance.Clear();   // 切房间时统一释放
    /// </summary>
    public class Resource : MonoSingleton<Resource>
    {
        // 缓存键带类型，避免同名路径的不同类型资源互相覆盖
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        private static string Key<T>(string path) => typeof(T).Name + ":" + path;

        // ------------------- 单个资源 -------------------

        public T Load<T>(string path) where T : UnityEngine.Object
        {
            string key = Key<T>(path);
            if (_cache.TryGetValue(key, out object cached))
                return cached as T;

            T resource = Resources.Load<T>(path);
            if (resource != null) _cache[key] = resource;
            else Log.Error($"资源加载失败：{path}", LogChannel.Core);

            return resource;
        }

        public void LoadAsync<T>(string path, Action<T> callback) where T : UnityEngine.Object
        {
            StartCoroutine(LoadAsyncCoroutine<T>(path, callback));
        }

        private System.Collections.IEnumerator LoadAsyncCoroutine<T>(string path, Action<T> callback)
            where T : UnityEngine.Object
        {
            // 缓存命中也等一帧再回调，保证「异步回调一定晚于当前调用栈」
            yield return null;

            string key = Key<T>(path);
            if (_cache.TryGetValue(key, out object cached))
            {
                callback(cached as T);
                yield break;
            }

            ResourceRequest request = Resources.LoadAsync<T>(path);
            yield return request;

            T resource = request.asset as T;
            if (resource != null) _cache[key] = resource;
            else Log.Error($"资源异步加载失败：{path}", LogChannel.Core);

            callback(resource);
        }

        // ------------------- 整个文件夹 -------------------

        public T[] LoadAll<T>(string folderPath) where T : UnityEngine.Object
        {
            string key = Key<T>(folderPath);
            if (_cache.TryGetValue(key, out object cached))
                return cached as T[];

            T[] resources = Resources.LoadAll<T>(folderPath);
            if (resources != null) _cache[key] = resources;
            else Log.Error($"资源文件夹加载失败：{folderPath}", LogChannel.Core);

            return resources;
        }

        // ------------------- 释放 -------------------

        /// <summary>清空缓存并卸载未使用的资源（切房间 / 回过场时调用）。</summary>
        public void Clear()
        {
            // 必须一起清缓存：字典里的引用会让 UnloadUnusedAssets 认为资源还在用
            _cache.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}
