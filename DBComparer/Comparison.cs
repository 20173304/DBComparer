using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace DBComparer
{
    internal sealed class ValueRule
    {
        public string Column;
        public string Source;
        public string Target;
    }
    internal sealed class ComparisonResult
    {
        public DataTable Details;
        public int Equal;
        public int Different;
        public int LeftOnly;
        public int RightOnly;
        public int DifferentCells;
        public int ConvertedCells;
        public readonly HashSet<string> DifferentColumns = new HashSet<string>(StringComparer.Ordinal);
    }
    internal static class Comparison
    {
        private static string CreateValueToken(object value)
        {
            if (value == DBNull.Value)
                return "N";
            if (value is byte[])
                return "B" + Convert.ToBase64String((byte[])value);
            if (value is DateTime)
                return "D" + ((DateTime)value).ToString("O", CultureInfo.InvariantCulture);
            if (value is DateTimeOffset)
                return "O" + ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture);
            return value.GetType().FullName + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string CreateRowKey(DataRow row, string[] keys)
        {
            return string.Concat(keys.Select(k => { var value = CreateValueToken(row[k]); return value.Length + ":" + value; }));
        }

        private static Dictionary<string, DataRow> IndexRowsByKey(DataTable table, string[] keys, string side)
        {
            var result = new Dictionary<string, DataRow>(StringComparer.Ordinal);
            foreach (DataRow row in table.Rows)
            {
                var key = CreateRowKey(row, keys);
                if (result.ContainsKey(key))
                    throw new InvalidOperationException(side + " 데이터의 비교 키가 중복됩니다. 행을 고유하게 식별하는 키 조합을 선택하세요.");
                result.Add(key, row);
            }
            return result;
        }

        private static string FormatValue(object value)
        {
            if (value == DBNull.Value)
                return "<NULL>";
            if (value is byte[])
                return "0x" + BitConverter.ToString((byte[])value).Replace("-", "");
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public static ComparisonResult Run(DataTable left, DataTable right, string[] columns, string[] keys, IList<ValueRule> rules = null)
        {
            foreach (var column in columns)
                if (left.Columns[column].DataType != right.Columns[column].DataType)
                    throw new InvalidOperationException("컬럼 자료형이 다릅니다: " + column);
            var mappings = BuildValueMappings(left, columns, keys, rules);
            var sourceRows = IndexRowsByKey(left, keys, "DB1");
            var targetRows = IndexRowsByKey(right, keys, "DB2");
            var result = new ComparisonResult { Details = new DataTable() };
            foreach (var column in new[] { "상태", "비교 키", "컬럼", "DB1 값", "DB2 값", "DB2 기대 값" })
                result.Details.Columns.Add(column);
            foreach (var item in sourceRows)
            {
                DataRow other;
                var keyLabel = string.Join(", ", keys.Select(k => k + "=" + FormatValue(item.Value[k])));
                if (!targetRows.TryGetValue(item.Key, out other))
                {
                    result.LeftOnly++;
                    result.Details.Rows.Add("DB1에만 존재", keyLabel, "<행 전체>", FormatRow(item.Value, columns), "<행 없음>");
                    continue;
                }
                bool equal = true;
                foreach (var column in columns)
                {
                    object expected = item.Value[column];
                    Dictionary<string, string> values;
                    string replacement;
                    bool converted = false;
                    if (expected is string && mappings.TryGetValue(column, out values) && values.TryGetValue((string)expected, out replacement))
                    {
                        expected = replacement;
                        converted = true;
                    }
                    if (!String.Equals(CreateValueToken(expected), CreateValueToken(other[column]), StringComparison.Ordinal))
                    {
                        equal = false;
                        result.DifferentCells++;
                        result.DifferentColumns.Add(column);
                        result.Details.Rows.Add("값 불일치", keyLabel, column, FormatValue(item.Value[column]), FormatValue(other[column]), FormatValue(expected));
                    }
                    else if (converted)
                        result.ConvertedCells++;
                }
                if (equal)
                    result.Equal++;
                else
                    result.Different++;
            }
            foreach (var item in targetRows.Where(p => !sourceRows.ContainsKey(p.Key)))
            {
                result.RightOnly++;
                var label = string.Join(", ", keys.Select(k => k + "=" + FormatValue(item.Value[k])));
                result.Details.Rows.Add("DB2에만 존재", label, "<행 전체>", "<행 없음>", FormatRow(item.Value, columns));
            }
            return result;
        }

        private static Dictionary<string, Dictionary<string, string>> BuildValueMappings(
            DataTable left, string[] columns, string[] keys, IList<ValueRule> rules)
        {
            var mappings = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            foreach (var rule in rules ?? new ValueRule[0])
            {
                if (!columns.Contains(rule.Column, StringComparer.Ordinal))
                    throw new InvalidOperationException("변환 규칙의 컬럼을 비교 대상으로 선택하세요: " + rule.Column);
                if (keys.Contains(rule.Column, StringComparer.Ordinal))
                    throw new InvalidOperationException("비교 키에는 값 변환 규칙을 적용할 수 없습니다: " + rule.Column);
                if (left.Columns[rule.Column].DataType != typeof(string))
                    throw new InvalidOperationException("값 변환은 문자열 컬럼에만 적용할 수 있습니다: " + rule.Column);
                if (rule.Source == null || rule.Target == null)
                    throw new InvalidOperationException("NULL 변환은 지원하지 않습니다. 빈 문자열은 허용합니다.");
                Dictionary<string, string> values;
                if (!mappings.TryGetValue(rule.Column, out values))
                    mappings.Add(rule.Column, values = new Dictionary<string, string>(StringComparer.Ordinal));
                if (values.ContainsKey(rule.Source))
                    throw new InvalidOperationException("중복 변환 규칙: " + rule.Column + " / " + rule.Source);
                values.Add(rule.Source, rule.Target);
            }
            return mappings;
        }

        private static string FormatRow(DataRow row, string[] columns)
        {
            return string.Join(", ", columns.Select(column => column + "=" + FormatValue(row[column])));
        }
    }
}
