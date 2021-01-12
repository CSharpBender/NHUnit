using System.Collections.Generic;

namespace NHUnit
{
    internal class EntityNodeInfo
    {
        public int Level { get; set; } //0 for root
        public string Name { get; set; }
        public string PathName { get; set; }
        public bool IsList { get; set; }
        public List<EntityNodeInfo> Children { get; set; } = new List<EntityNodeInfo>();
    }
}