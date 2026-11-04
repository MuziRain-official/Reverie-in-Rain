using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 配置中心：按表名查询配置表。
    ///
    ///   Config.Instance.Register("Enemies", CsvLoader.Load("Enemies"));   // 启动时注册
    ///   var row = Config.Instance.Table("Enemies").Get(1001);             // 运行时查
    ///   int hp  = row.GetInt("hp");
    ///
    /// 约定：表在启动时注册好，运行时只读。
    /// </summary>
    public class Config : Singleton<Config>
    {
        // 表名 -> 配置表
        private readonly Dictionary<string, ConfigTable> _tables = new Dictionary<string, ConfigTable>();

        public void Register(string name, ConfigTable table)
        {
            if (table == null)
            {
                Log.Error($"配置表 {name} 为空，未注册", LogChannel.Core);
                return;
            }
            _tables[name] = table;
        }

        /// <summary>取一张表，未注册返回 null。</summary>
        public ConfigTable Table(string name)
        {
            if (_tables.TryGetValue(name, out var table)) return table;
            Log.Error($"配置表 {name} 未注册", LogChannel.Core);
            return null;
        }

        protected override void OnRelease() => _tables.Clear();
    }
}
