using System;
using System.Collections.Generic;

namespace WellModesBot
{
    internal class FieldInfo
    {
        public string Number { get; internal set; }
        public string FieldName { get; internal set; }
        public int RowIndex { get; internal set; }
        public List<object> Data { get; internal set; }
        public string FullName => Number + FieldName;
        public int WorksheetNumber { get; set; }

        public override string ToString()
        {
            return FullName;
        }
    }
}
