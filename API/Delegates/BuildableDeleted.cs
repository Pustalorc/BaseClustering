namespace Pustalorc.Plugins.BaseClustering.API.Delegates;

/// <summary>
/// A delegate that handles deletion of buildables from nelson's code.
/// </summary>
/// <param name="instanceId">The instance Id of the buildable.</param>
/// <param name="isStructure">If the buildable was a structure or not.</param>
public delegate void BuildableDeleted(uint instanceId, bool isStructure);