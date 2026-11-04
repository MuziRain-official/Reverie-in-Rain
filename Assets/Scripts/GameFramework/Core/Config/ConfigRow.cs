using System.Collections.Generic;
using System.Globalization;

namespace Reverie.Framework
{
    /// <summary>
    /// 配置表的一行：按列名取值。列名来自 CSV 第一行表头。
    /// 约定：同一个列只放一种类型。
    /// </summary>
    public class ConfigRow
    {
        private readonly Dictionary<string, string> _fields;

        internal ConfigRow(Dictionary<string, string> fields) => _fields = fields;

        public string GetString(string column) => _fields[column];

        public int GetInt(string column) => int.Parse(_fields[column]);

        // 用不变文化解析，避免不同系统区域设置把小数点当成逗号
        public float GetFloat(string column) => float.Parse(_fields[column], CultureInfo.InvariantCulture);

        public bool GetBool(string column) => _fields[column] == "1";
    }
}
