using System.Collections.Generic;
using System.Xml.Serialization;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    public sealed class RustOptions
    {
        [XmlArray("ItemId")] public List<ushort> FloorIds;
        public float ExtraRadius;

        public RustOptions()
        {
        }

        public RustOptions(List<ushort> floors, float extraRadius)
        {
            FloorIds = floors;
            ExtraRadius = extraRadius;
        }
    }
}