using System.Threading;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class ThreadUtil2
    {
        public static Thread GameThread => ThreadUtil.gameThread;

        public static bool IsGameThread => Thread.CurrentThread == GameThread;
    }
}