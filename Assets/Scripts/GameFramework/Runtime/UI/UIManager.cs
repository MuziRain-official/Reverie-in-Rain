using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Reverie.Framework
{
    /// <summary>
    /// UI 门面：业务只通过它开关界面，不直接碰 WindowLayer / PanelLayer。
    ///
    /// 框架没有场景级的 UI 根节点，所以启动时自动创建：
    ///   [UI]（Canvas + CanvasScaler 1920×1080 + GraphicRaycaster，跨场景常驻）
    ///     ├── Scene    PanelLayer
    ///     ├── Hud      PanelLayer
    ///     ├── Window   WindowLayer
    ///     ├── Popup    WindowLayer
    ///     ├── Toast    PanelLayer
    ///     ├── Loading  PanelLayer
    ///     └── [EventSystem]（场景里没有时补一个，否则按钮点不动）
    ///
    /// 界面由业务实例化后登记，框架不负责加载预制体：
    ///   var go = Instantiate(Resource.Instance.Load&lt;GameObject&gt;("UI/Window/Pause"));
    ///   UIManager.Instance.Register(UILayerType.Window, "Pause", go.GetComponent&lt;UIScreen&gt;());
    ///
    ///   UIManager.Instance.Show(UILayerType.Window, "Pause", props);
    ///   UIManager.Instance.Hide(UILayerType.Window, "Pause");   // 窗口层 = 返回上一步
    ///   UIManager.Instance.Back();
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        // 层类型 → 层实例。按 UILayerType 的声明顺序创建，字典只用来按类型查
        private readonly Dictionary<UILayerType, UILayer> _layers = new Dictionary<UILayerType, UILayer>();

        private Canvas _canvas;

        // 两个栈式层的引用，Back 要按「弹窗优先」的顺序依次尝试
        private WindowLayer _window;
        private WindowLayer _popup;

        // ---------------- 生命周期 ----------------

        protected override void OnInit() => Build();

        /// <summary>释放：隐藏所有界面（不播动画）并销毁整个 UI 根节点。</summary>
        protected override void OnRelease()
        {
            HideAll(false);
            _layers.Clear();
            _window = null;
            _popup = null;

            if (_canvas != null) Object.Destroy(_canvas.gameObject);
            _canvas = null;
        }

        // ---------------- 注册 ----------------

        /// <summary>登记界面（业务实例化好再登记），之后用 id 开关；登记后界面会自动隐藏。</summary>
        public void Register(UILayerType layerType, string id, UIScreen screen)
        {
            UILayer layer = GetLayer(layerType);
            if (layer != null) layer.Register(id, screen);
        }

        /// <summary>从登记表移除界面（不销毁），需要回收时由业务自己 Destroy。</summary>
        public void Unregister(UILayerType layerType, string id)
        {
            UILayer layer = GetLayer(layerType);
            if (layer != null) layer.Unregister(id);
        }

        // ---------------- 打开 / 关闭 ----------------

        /// <summary>打开界面并返回实例；没登记过返回 null（不会自动加载预制体）。</summary>
        public UIScreen Show(UILayerType layerType, string id, object props = null)
        {
            UILayer layer = GetLayer(layerType);
            if (layer == null) return null;

            UIScreen screen = layer.Get(id);
            if (screen == null)
            {
                Log.Warn($"界面未登记：{id}", LogChannel.UI);
                return null;
            }

            layer.ShowScreen(screen, props);
            return screen;
        }

        /// <summary>关闭界面；栈式层（窗口 / 弹窗）等同于返回上一步，平铺层是单纯隐藏。</summary>
        public void Hide(UILayerType layerType, string id)
        {
            UILayer layer = GetLayer(layerType);
            if (layer != null) layer.HideById(id);
        }

        /// <summary>隐藏所有层；animate 为 false 时跳过退出动画（切场景 / 释放时用）。</summary>
        public void HideAll(bool animate = true)
        {
            foreach (UILayer layer in _layers.Values)
                layer.HideAllScreens(animate);
        }

        /// <summary>返回键：先弹窗后窗口逐层退回；没有可关的界面返回 false，交给上层流程。</summary>
        public bool Back()
        {
            if (_popup != null && _popup.Back()) return true;
            return _window != null && _window.Back();
        }

        /// <summary>界面是否处于打开状态（不在登记表里也算未打开）。</summary>
        public bool IsShowing(UILayerType layerType, string id)
        {
            UILayer layer = GetLayer(layerType);
            UIScreen screen = layer == null ? null : layer.Get(id);
            return screen != null && screen.IsShowing;
        }

        /// <summary>取整层，用于按层批量操作；层类型非法时打错误并返回 null。</summary>
        public UILayer GetLayer(UILayerType layerType)
        {
            if (_layers.TryGetValue(layerType, out UILayer layer)) return layer;

            Log.Error($"UI 层不存在：{layerType}", LogChannel.UI);
            return null;
        }

        // ---------------- 搭建（框架内部，启动时执行一次） ----------------

        /// <summary>创建 Canvas、六个层和缺失的 EventSystem；新增层要同时改 UILayerType 与这里。</summary>
        private void Build()
        {
            _canvas = CreateCanvas();

            // 按显示顺序创建：越后面的层越靠前
            AddLayer<PanelLayer>(UILayerType.Scene);
            AddLayer<PanelLayer>(UILayerType.Hud);
            _window = AddLayer<WindowLayer>(UILayerType.Window);
            _popup = AddLayer<WindowLayer>(UILayerType.Popup);
            AddLayer<PanelLayer>(UILayerType.Toast);
            AddLayer<PanelLayer>(UILayerType.Loading);

            EnsureEventSystem();
        }

        /// <summary>根 Canvas：屏幕空间覆盖、1920×1080 适配、跨场景常驻。</summary>
        private Canvas CreateCanvas()
        {
            GameObject go = new GameObject("[UI]", typeof(RectTransform));
            Object.DontDestroyOnLoad(go);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>建一个层节点，挂上对应的层脚本，并让它铺满 Canvas。</summary>
        private T AddLayer<T>(UILayerType type) where T : UILayer
        {
            GameObject go = new GameObject(type.ToString(), typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            Stretch(go.GetComponent<RectTransform>());

            T layer = go.AddComponent<T>();
            _layers[type] = layer;
            return layer;
        }

        /// <summary>场景里没有 EventSystem 时补一个，按钮才能点击；场景自带则跳过。</summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject go = new GameObject("[EventSystem]", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(_canvas.transform, false);
        }

        /// <summary>把 RectTransform 铺满父节点（四边对齐）。</summary>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
