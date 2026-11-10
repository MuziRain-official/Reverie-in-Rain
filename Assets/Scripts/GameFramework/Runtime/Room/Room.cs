using System;
using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 房间管理器：管同一张大地图里的一间间房间。
    /// 只负责三件事：记住当前房间、切换房间内容、按门算玩家落点。
    /// 切大地图是 Scene 模块的事；相机边界由本模块算好，交给相机模块用。
    ///
    ///   Room.Instance.Init(rooms);                        // 关卡入口先把房间配好
    ///   Room.Instance.Enter("town", null, null);          // 直接进房
    ///   Room.Instance.Enter("forest", "doorA", () => { });// 从 doorA 走进去
    ///
    /// 房间内容统一挂在场景里的 Rooms 节点下，一间房一个子物体：
    ///
    ///   Rooms
    ///     ├── town     ← RootName = "town"
    ///     └── forest   ← RootName = "forest"
    ///
    /// 切换房间就是把这些子物体打开 / 关掉。挂在一个父节点下有两个好处：
    /// 一是 Transform.Find 能搜到未激活的子物体（GameObject.Find 不能，它只找激活的，
    /// 而目标房间切换前恰恰是关着的，用它永远找不到）；二是不必遍历整个场景。
    /// 不做卸载重建——同图内切换，物体一直存在，只是显隐，初学者最好懂。
    /// </summary>
    public class Room : Singleton<Room>
    {
        /// <summary>房间内容统一挂在这个名字的父节点下。</summary>
        public const string RoomsRootName = "Rooms";

        // 房间 id -> 房间数据
        private readonly System.Collections.Generic.Dictionary<string, RoomConfig> _rooms
            = new System.Collections.Generic.Dictionary<string, RoomConfig>();

        // 房间的根物体缓存：每次切换不用重新找
        private readonly System.Collections.Generic.Dictionary<string, Transform> _roots
            = new System.Collections.Generic.Dictionary<string, Transform>();

        // 场景里的 Rooms 父节点缓存
        private Transform _roomsRoot;

        /// <summary>当前房间 id，没进房时为 null。</summary>
        public string CurrentId { get; private set; }

        /// <summary>当前房间数据，没进房时为 null。</summary>
        public RoomConfig Current { get; private set; }

        /// <summary>当前相机边界（世界坐标），没进房时是零矩形。相机模块读它来约束视野。</summary>
        public Rect Boundary { get; private set; }

        /// <summary>正在切换房间时为 true。</summary>
        public bool IsSwitching { get; private set; }

        /// <summary>玩家从门前出现的位置。</summary>
        public Vector2 SpawnPosition { get; private set; }

        /// <summary>玩家出现时的朝向：1 = 朝右，-1 = 朝左。</summary>
        public int SpawnFacing { get; private set; } = 1;

        /// <summary>
        /// 关卡入口登记房间。重复登记同 id 会覆盖（方便编辑器里改了就读新的）。
        /// </summary>
        public void Init(RoomConfig[] configs)
        {
            _rooms.Clear();
            _roots.Clear();
            _roomsRoot = null;
            CurrentId = null;
            Current = null;
            Boundary = new Rect();
            IsSwitching = false;

            if (configs == null) return;

            for (int i = 0; i < configs.Length; i++)
            {
                var c = configs[i];
                if (c == null) continue;
                if (string.IsNullOrEmpty(c.Id))
                {
                    Log.Error("有房间没填 Id，已跳过", LogChannel.Room);
                    continue;
                }
                _rooms[c.Id] = c;
            }
        }

        /// <summary>
        /// 进入房间。doorId 为空表示直接进（比如存档读取、第一间房），
        /// 否则从这扇门走进来，落点取它 LinkId 指向的那扇门的 SpawnX / SpawnY。
        /// 房间 id 不存在时会报错并原样返回，不会留在半切换状态。
        /// </summary>
        public void Enter(string roomId, string doorId, Action onDone)
        {
            if (IsSwitching)
            {
                Log.Warn($"上一间房还在切换，忽略对 {roomId} 的请求", LogChannel.Room);
                return;
            }

            RoomConfig config;
            if (!_rooms.TryGetValue(roomId, out config))
            {
                Log.Error($"房间 {roomId} 不存在，已忽略", LogChannel.Room);
                onDone?.Invoke();
                return;
            }

            // 先算出从哪来，再关旧的、开新的
            if (!string.IsNullOrEmpty(doorId))
            {
                var door = config.FindDoor(doorId);
                if (door == null)
                {
                    Log.Warn($"房间 {roomId} 里没有门 {doorId}，从左上角进入", LogChannel.Room);
                    SpawnPosition = new Vector2(config.BoundaryMinX, config.BoundaryMinY);
                    SpawnFacing = 1;
                }
                else
                {
                    SpawnPosition = new Vector2(door.SpawnX, door.SpawnY);
                    SpawnFacing = door.Facing >= 0 ? 1 : -1;
                }
            }
            else
            {
                SpawnPosition = new Vector2(config.BoundaryMinX, config.BoundaryMinY);
                SpawnFacing = 1;
            }

            // 关旧房间，开新房间
            if (Current != null) SetRootActive(Current.RootName, false);
            if (!SetRootActive(config.RootName, true))
            {
                // 一个房间内容都没打开的房间没有意义，直接当失败，别让上层以为切成功了
                Log.Error($"房间 {roomId} 的内容物体 {config.RootName} 不存在，未切换", LogChannel.Room);
                onDone?.Invoke();
                return;
            }

            CurrentId = roomId;
            Current = config;
            Boundary = new Rect(config.BoundaryMinX, config.BoundaryMinY,
                                config.BoundaryMaxX - config.BoundaryMinX,
                                config.BoundaryMaxY - config.BoundaryMinY);

            Log.Info($"进入房间 {roomId}，落点 {SpawnPosition}，朝向 {SpawnFacing}", LogChannel.Room);

            onDone?.Invoke();
        }

        /// <summary>清空当前房间，回到「没进房」状态（离开关卡时调）。</summary>
        public void Clear()
        {
            if (Current != null) SetRootActive(Current.RootName, false);
            CurrentId = null;
            Current = null;
            Boundary = new Rect();
            SpawnPosition = Vector2.zero;
            SpawnFacing = 1;
            IsSwitching = false;
        }

        /// <summary>
        /// 按 RootName 在 Rooms 节点下找子物体并开关它。
        /// 返回 false 表示没找到，调用方自己决定要不要当失败。
        /// </summary>
        private bool SetRootActive(string rootName, bool active)
        {
            if (string.IsNullOrEmpty(rootName)) return false;

            Transform root;
            if (!_roots.TryGetValue(rootName, out root) || root == null)
            {
                // GameObject.Find 只搜激活物体，目标房间切换前是关着的，用它永远找不到；
                // Transform.Find 能搜到未激活的子物体，所以房间统一挂在 Rooms 下按名字查。
                Transform roomsRoot = FindRoomsRoot();
                if (roomsRoot == null)
                {
                    Log.Warn($"场景里没有 {RoomsRootName} 节点，找不到房间物体 {rootName}", LogChannel.Room);
                    return false;
                }

                root = roomsRoot.Find(rootName);
                if (root == null)
                {
                    Log.Warn($"{RoomsRootName} 下找不到房间物体 {rootName}", LogChannel.Room);
                    return false;
                }
                _roots[rootName] = root;
            }

            root.gameObject.SetActive(active);
            return true;
        }

        /// <summary>找场景里的 Rooms 节点，找不到返回 null 并只警告一次。</summary>
        private Transform FindRoomsRoot()
        {
            if (_roomsRoot != null) return _roomsRoot;

            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == RoomsRootName)
                {
                    _roomsRoot = roots[i].transform;
                    return _roomsRoot;
                }
            }

            return null;
        }

        protected override void OnRelease()
        {
            _rooms.Clear();
            _roots.Clear();
            _roomsRoot = null;
            CurrentId = null;
            Current = null;
            Boundary = new Rect();
            IsSwitching = false;
        }
    }
}
