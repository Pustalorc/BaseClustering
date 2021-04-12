using System.Collections.Generic;
using Pustalorc.Plugins.BaseClustering.API.Buildables;

namespace Pustalorc.Plugins.BaseClustering.API.Delegates
{
    public delegate void BuildablesChanged(IEnumerable<Buildable> buildables);
}