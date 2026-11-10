using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 相机控制：跟随目标、按房间边界限制视野、跟随平滑。
    /// 只认「一张地图里的矩形边界」，边界从哪来它不关心，由业务层（通常是 Room）喂进来。
    ///
    ///   CameraController.Instance.SetTarget(player.transform);   // 开始跟随
    ///   CameraController.Instance.SetBoundary(room.Boundary);    // 进房间时换边界
    ///   CameraController.Instance.SetBoundary(Rect.zero);        // 传零矩形 = 解除限制
    ///
    /// 相机组件不在这里 AddComponent：在 Awake/OnInit 里给自己加 Camera 会触发
    /// 「不能给正在启用的 GameObject 添加同组件的依赖」这类问题。
    /// 做法是——场景里有主相机就接管，没有就单独建一个子物体挂相机，由它来看。
    /// </summary>
    public class CameraController : MonoSingleton<CameraController>
    {
        /// <summary>跟随目标，为空时相机只受边界约束。</summary>
        public Transform Target { get; private set; }

        /// <summary>当前边界（世界坐标）。宽或高为 0 表示不限制。</summary>
        public Rect Boundary { get; private set; }

        /// <summary>跟随平滑：0 = 硬跟随，越大越慢。</summary>
        public float SmoothTime = 0.12f;

        private Vector3 _velocity;

        /// <summary>实际渲染用的相机。可能是场景里的主相机，也可能是本模块自动建的。</summary>
        private UnityEngine.Camera _camera;

        protected override void OnInit()
        {
            _camera = AcquireCamera();
            if (_camera != null)
            {
                // 2D 横版：正交投影
                _camera.orthographic = true;
            }
            else
            {
                Log.Error("找不到可用相机，相机模块只做跟随不做渲染", LogChannel.Core);
            }

            _velocity = Vector3.zero;
        }

        /// <summary>
        /// 找一个能用的相机：优先场景已有的主相机（直接接管），没有就把本模块的物体当相机用。
        /// 注意 MonoSingleton 已经给本物体挂了本组件，再给它挂 Camera 会踩 Unity 的组件依赖限制，
        /// 所以这里必须返回「别的物体上的相机」。
        /// </summary>
        private UnityEngine.Camera AcquireCamera()
        {
            var main = UnityEngine.Camera.main;
            if (main != null) return main;

            var any = FindFirstObjectByType<UnityEngine.Camera>();
            if (any != null) return any;

            var go = new GameObject("Camera (Auto)");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, -10f);
            return go.AddComponent<UnityEngine.Camera>();
        }

        /// <summary>设置跟随目标；传 null 取消跟随。</summary>
        public void SetTarget(Transform target)
        {
            Target = target;
            _velocity = Vector3.zero;
        }

        /// <summary>设置边界；传宽或高为 0 的矩形表示解除限制。</summary>
        public void SetBoundary(Rect boundary)
        {
            Boundary = boundary;
        }

        /// <summary>是否有有效边界。</summary>
        public bool HasBoundary => Boundary.width > 0f && Boundary.height > 0f;

        /// <summary>
        /// 立刻把相机归位到目标（不做平滑）。进房间、读档时用，避免镜头从上一间房飘过来。
        /// </summary>
        public void Snap()
        {
            if (Target == null || _camera == null) return;
            _camera.transform.position = ClampToBoundary(Target.position);
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            var camPoint = _camera.transform.position;

            if (Target == null)
            {
                // 没有目标时也要遵守边界（比如对话时镜头钉在某处）
                if (HasBoundary) _camera.transform.position = ClampToBoundary(camPoint);
                return;
            }

            Vector3 desired = ClampToBoundary(Target.position);
            Vector3 next = Vector3.SmoothDamp(camPoint, desired, ref _velocity, SmoothTime);
            next.z = camPoint.z;
            _camera.transform.position = next;
        }

        /// <summary>把「以目标为中心」的相机位置夹进边界，保证视野不露出房间外。</summary>
        private Vector3 ClampToBoundary(Vector3 focus)
        {
            float z = _camera != null ? _camera.transform.position.z : -10f;

            if (!HasBoundary) return new Vector3(focus.x, focus.y, z);

            float halfH = _camera.orthographicSize;
            float halfW = halfH * Screen.width / Screen.height;

            float minX = Boundary.xMin + halfW;
            float maxX = Boundary.xMax - halfW;
            float minY = Boundary.yMin + halfH;
            float maxY = Boundary.yMax - halfH;

            // 房间比视野还小时范围会翻过来，取中点免得乱跳
            float x = minX <= maxX ? Mathf.Clamp(focus.x, minX, maxX) : Boundary.center.x;
            float y = minY <= maxY ? Mathf.Clamp(focus.y, minY, maxY) : Boundary.center.y;

            return new Vector3(x, y, z);
        }

        protected override void OnRelease()
        {
            Target = null;
            Boundary = Rect.zero;
            _velocity = Vector3.zero;
        }
    }
}
