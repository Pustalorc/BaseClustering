using System.Collections.Generic;
using Pustalorc.Plugins.BaseClustering.API.Buildables;

namespace Pustalorc.Plugins.BaseClustering.API.Delegates;

/// <summary>
/// A delegate that handles a notification about multiple <see cref="Buildable"/>s changing (being added or removed).
/// </summary>
/// <param name="buildables">The affected <see cref="Buildable"/>s.</param>
public delegate void BuildablesChanged(IEnumerable<Buildable> buildables);