using OpenMod.Core.Eventing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pustalorc.Plugins.BaseClustering.API.BaseClusters.Events
{
    public class OnClusterChangeEvent : Event
    {
        public BaseCluster NewCluster { get; set; }
    }

    public class OnClustersGenerated : Event
    {
    }

    public class OnClusterAdded : OnClusterChangeEvent
    {
    }

    public class OnClusterRemoved : OnClusterChangeEvent
    {
    }
}
