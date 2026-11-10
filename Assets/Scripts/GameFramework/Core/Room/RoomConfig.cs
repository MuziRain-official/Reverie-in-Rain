using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 单个房间的纯数据（不依赖 Unity，不挂物体）。
    /// 由业务层的配置资产（ScriptableObject）填好后传给 Room 模块。
    /// 坐标都是世界坐标。
    /// </summary>
    public class RoomConfig
    {
        /// <summary>房间唯一 ID，存档和地图用它认房间。</summary>
        public string Id;

        /// <summary>房间内容所在的物体名（同图内切换，靠它找物体，不切场景）。</summary>
        public string RootName;

        // ---- 相机边界 ----
        public float BoundaryMinX;
        public float BoundaryMinY;
        public float BoundaryMaxX;
        public float BoundaryMaxY;

        /// <summary>所有门，按 id 查。</summary>
        public List<RoomDoor> Doors = new List<RoomDoor>();

        /// <summary>按门 id 找门，找不到返回 null。</summary>
        public RoomDoor FindDoor(string doorId)
        {
            if (string.IsNullOrEmpty(doorId)) return null;
            for (int i = 0; i < Doors.Count; i++)
            {
                if (Doors[i].Id == doorId) return Doors[i];
            }
            return null;
        }
    }

    /// <summary>
    /// 一扇门：玩家从远处那扇门走进来时，应该出现在这里的落点与朝向。
    /// 约定：A 门的 LinkId 指向 B 门的 Id，表示「从 A 进去，从 B 出来」。
    /// </summary>
    public class RoomDoor
    {
        public string Id;

        /// <summary>对面那扇门的 Id，用来算落点和朝向。</summary>
        public string LinkId;

        /// <summary>玩家出现的位置（世界坐标）。</summary>
        public float SpawnX;
        public float SpawnY;

        /// <summary>玩家出现时的朝向：1 = 朝右，-1 = 朝左。</summary>
        public int Facing = 1;
    }
}
