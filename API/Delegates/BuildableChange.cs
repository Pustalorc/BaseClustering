using Pustalorc.Plugins.BaseClustering.API.Buildables;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Delegates
{
    public delegate void BuildableChange(Buildable buildable);
    public delegate void BuildableDeleted(Transform buildable);
}