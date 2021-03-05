using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using Rocket.Core.Utils;
using SDG.Unturned;

namespace Pustalorc.Plugins.BaseClustering.API.Utilities
{
    public sealed class BuildableDamageQueue
    {
        private readonly ConcurrentQueue<QueuedDamage> m_Queue;
        private readonly BaseClusteringPlugin m_Plugin;
        private readonly SemaphoreSlim m_Semaphore;
        public readonly BackgroundWorker Worker;

        public BuildableDamageQueue(BaseClusteringPlugin instance)
        {
            m_Plugin = instance;
            m_Queue = new ConcurrentQueue<QueuedDamage>();
            m_Semaphore = new SemaphoreSlim(1, 1);
            Worker = new BackgroundWorker();
            Worker.DoWork += DoWork;
            Worker.WorkerReportsProgress = true;
            Worker.WorkerSupportsCancellation = false;
        }

        public void CleanupQueue()
        {
            while (Worker.IsBusy)
                Thread.Sleep(100);

            while (m_Queue.TryDequeue(out var element) || m_Queue.Count > 0)
                ProcessElement(element);
        }

        public void Enqueue(QueuedDamage damage)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                m_Queue.Enqueue(damage);

                if (!Worker.IsBusy)
                    Worker.RunWorkerAsync();
            });
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            var processed = 0;
            while (m_Queue.Count > 0)
            {
                if (!m_Queue.TryDequeue(out var element)) continue;

                m_Semaphore.Wait();
                processed++;
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    ProcessElement(element);
                    m_Semaphore.Release();
                });

                Worker.ReportProgress(processed / (m_Queue.Count + processed) * 100);
            }
        }

        // Process a queued element.
        private void ProcessElement([CanBeNull] QueuedDamage element)
        {
            if (element == null)
                return;

            BaseCluster cluster = null;

            // Get the belonging cluster from the element.
            if (m_Plugin != null)
                cluster = m_Plugin.GetClusterWithElement(element.TargetBuildable);

            // If IsBeingDestroyed should be turned to true, and cluster isn't null, switch IsBeingDestroyed to true.
            if (element.EnableIsBeingDestroyed && cluster?.IsBeingDestroyed == false)
                cluster.IsBeingDestroyed = true;

            InteractableStorage store = null;
            if (element.DisableItemDrop && element.TargetBuildable.Interactable is InteractableStorage storage)
            {
                store = storage;
                storage.despawnWhenDestroyed = true;
            }

            // Force unsafe damage/destroy, this means that this method MUST HAVE BEEN CALLED FROM THE MAIN THREAD.
            if (element.TargetDamage >= element.TargetBuildable.Health)
                element.TargetBuildable.UnsafeDestroy();
            else
                element.TargetBuildable.UnsafeDamage(element.TargetDamage);

            if (element.DisableItemDrop && store != null)
                store.despawnWhenDestroyed = false;

            // Finally, if cluster wasn't null earlier when we switched this option, verify that the option is false now.
            if (element.EnableIsBeingDestroyed && cluster?.IsBeingDestroyed == true)
                cluster.IsBeingDestroyed = false;
        }
    }
}