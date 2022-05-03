using System;
using System.Collections.Generic;

namespace WellModesBot
{
    internal class WorksheetInfo
    {
        public string Name { get; }
        public List<FieldInfo> Fields { get; }
        public Dictionary<string, List<FieldInfo>> FieldsCombined { get; }
        public int[] RequiredData { get; }
        public List<string> ColumnNames { get; }
        public List<string> ColumnMetrics { get; }

        public WorksheetInfo(string name, List<FieldInfo> fields, Dictionary<string, List<FieldInfo>> fieldsCombined, int[] requiredData, List<string> columnNames, List<string> columnMetrics)
        {
            Name = name;
            Fields = fields;
            FieldsCombined = fieldsCombined;
            RequiredData = requiredData;
            ColumnNames = columnNames;
            ColumnMetrics = columnMetrics;
        }
    }
}
