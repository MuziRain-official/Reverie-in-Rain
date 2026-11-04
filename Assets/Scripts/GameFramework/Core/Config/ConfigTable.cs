using System.Collections.Generic;

namespace Reverie.Framework
{
    /// <summary>
    /// 一张配置表：CSV 文本 -> 按 id 查询的行。
    /// 约定：CSV 第一行是表头，第一列是 id，字段里不要出现英文逗号。
    /// </summary>
    public class ConfigTable
    {
        // id -> 行
        private readonly Dictionary<string, ConfigRow> _rows = new Dictionary<string, ConfigRow>();

        public int Count => _rows.Count;

        /// <summary>按 id 取一行，不存在返回 null。</summary>
        public ConfigRow Get(string id) => _rows.TryGetValue(id, out var row) ? row : null;

        /// <summary>按数字 id 取一行，不存在返回 null。</summary>
        public ConfigRow Get(int id) => Get(id.ToString());

        /// <summary>解析 CSV 文本。</summary>
        public void Load(string csv)
        {
            _rows.Clear();
            if (string.IsNullOrEmpty(csv)) return;

            var lines = csv.Split('\n');
            var headers = SplitLine(lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                var cells = SplitLine(line);
                var fields = new Dictionary<string, string>();
                for (int c = 0; c < headers.Length; c++)
                    fields[headers[c]] = c < cells.Length ? cells[c].Trim() : "";

                _rows[fields[headers[0]]] = new ConfigRow(fields);
            }
        }

        private static string[] SplitLine(string line)
        {
            var parts = line.Trim().Split(',');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim();
            return parts;
        }
    }
}
