using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Buildables;

namespace Pustalorc.Plugins.BaseClustering.API.Utilities
{
    [UsedImplicitly]
    public class QueuedDamage
    {
        public Buildable TargetBuildable;
        public ushort TargetDamage;
        public bool EnableIsBeingDestroyed;
        public bool DisableItemDrop;

        public QueuedDamage(Buildable buildable, ushort damage, bool enableIsBeingDestroyed, bool disableItemDrop)
        {
            TargetBuildable = buildable;
            TargetDamage = damage;
            EnableIsBeingDestroyed = enableIsBeingDestroyed;
            DisableItemDrop = disableItemDrop;
        }
    }
}