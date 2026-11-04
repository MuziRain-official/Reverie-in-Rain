using UnityEngine;

namespace Reverie.Framework
{
    /// <summary>
    /// 从 Resources 读取 CSV 并解析成 ConfigTable。
    /// 文件放在 Assets/Resources/Configs/ 下；解析逻辑在 Core，这里只负责读文件。
    /// </summary>
    public static class CsvLoader
    {
        /// <summary>读取 Resources/Configs/{name}.csv，找不到返回 null。</summary>
        public static ConfigTable Load(string name)
        {
            var asset = Resources.Load<TextAsset>("Configs/" + name);
            if (asset == null)
            {
                Log.Error($"找不到配置表 Resources/Configs/{name}.csv", LogChannel.Core);
                return null;
            }

            var table = new ConfigTable();
            table.Load(asset.text);
            return table;
        }
    }
}
