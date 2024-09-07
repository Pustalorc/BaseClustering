extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using Pustalorc.Libraries.BaseClustering.API.BaseClusters.Configuration.Interfaces;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Abstraction;
using Pustalorc.Libraries.BuildableAbstractions.API.Buildables.Implementations;
using UnityEngine;

namespace Pustalorc.Libraries.BaseClustering.API.BaseClusters.Clusters.Implementation;

/// <inheritdoc />
[PublicAPI]
public class GlobalBaseCluster : DefaultBaseCluster
{
    /// <inheritdoc />
    public GlobalBaseCluster(IClusterRules clusterRules, uint instanceId) : base(clusterRules, instanceId)
    {
    }

    /// <inheritdoc />
    public override void AddBuildable(Buildable build)
    {
        if (build is StructureBuildable)
            throw new NotSupportedException("StructureBuildables are not supported by global clusters.");

        base.AddBuildable(build);
    }

    /// <inheritdoc />
    protected override void AddBuildablesInternal(IEnumerable<Buildable> buildables, bool raiseEvent = true)
    {
        base.AddBuildablesInternal(buildables.Where(static buildable => buildable is not StructureBuildable),
            raiseEvent);
    }

    /// <inheritdoc />
    public override void Destroy(bool shouldDropItems = true)
    {
        base.Destroy(shouldDropItems);
        IsBeingDestroyed = false;
    }

    /// <inheritdoc />
    /// <summary>
    ///     A global cluster does not have integrity. It holds all objects within range that are not in a separate cluster.
    /// </summary>
    protected override void VerifyAndCorrectIntegrity()
    {
    }

    /// <inheritdoc />
    /// <summary>
    ///     A global cluster does not have integrity. It holds all objects within range that are not in a separate cluster.
    /// </summary>
    protected override bool VerifyBarricadeIntegrity()
    {
        return true;
    }

    /// <inheritdoc />
    /// <summary>
    ///     A global cluster does not have integrity. It holds all objects within range that are not in a separate cluster.
    /// </summary>
    protected override bool VerifyStructureIntegrity()
    {
        return true;
    }

    /// <inheritdoc />
    /// <summary>
    ///     A global cluster does not have integrity. It holds all objects within range that are not in a separate cluster.
    /// </summary>
    public override bool IsWithinRange(Buildable buildable)
    {
        return false;
    }

    /// <inheritdoc />
    /// <summary>
    ///     A global cluster does not have integrity. It holds all objects within range that are not in a separate cluster.
    /// </summary>
    public override bool IsWithinRange(Vector3 vector)
    {
        return false;
    }
}