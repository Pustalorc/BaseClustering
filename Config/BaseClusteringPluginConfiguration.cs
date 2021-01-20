using System.Collections.Generic;
using System.Xml.Serialization;
using Rocket.API;

namespace Pustalorc.Plugins.BaseClustering.Config
{
    public sealed class BaseClusteringPluginConfiguration : IRocketPluginConfiguration
    {
        public bool VerboseLogging;
        public float ExtraRadius;
        [XmlArray("ItemId")] public List<ushort> FloorItemIds;

        public void LoadDefaults()
        {
            VerboseLogging = false;
            ExtraRadius = 6.1f;
            FloorItemIds = new List<ushort> {31, 51, 52, 369, 1262, 1263, 1264};
        }
    }
}