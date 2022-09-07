using System.Collections.Generic;

namespace WellModesBot
{
    internal class WellData
    {
        public int WellsCount => Wells.Length;
        public WellDataItem[] Wells { get; set; }
    }
}