using Pustalorc.Plugins.BaseClustering.API.Buildables;

namespace Pustalorc.Plugins.BaseClustering.API.Delegates
{
    /// <summary>
    /// A delegate that handles any notification about a buildable changing (being added, removed).
    /// </summary>
    /// <param name="buildable">The affected <see cref="Buildable"/>.</param>
    /// <remarks>
    /// This delegate is to not be used and fired if multiple buildables changed.
    /// <br/>
    /// If multiple of them changed, please use <see cref="BuildablesChanged"/>.
    /// </remarks>
    public delegate void BuildableChange(Buildable buildable);
}