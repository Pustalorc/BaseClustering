using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Utilities
{
    public static class Extensions
    {
        public static Vector3 AverageCenter([NotNull] this IEnumerable<Vector3> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var list = source.ToList();

            var sum = Vector3.zero;

            checked
            {
                sum = list.Aggregate(sum, (current, element) => current + element);
            }

            if (list.Count > 0) return sum / list.Count;

            return Vector3.zero;
        }

        public static Vector3 AverageCenter<TSource>([NotNull] this IEnumerable<TSource> source,
            [NotNull] Func<TSource, Vector3> selector)
        {
            return source.Select(selector).AverageCenter();
        }

        [NotNull]
        public static IEnumerable<float> GetDistances<T>([NotNull] this IEnumerable<T> list,
            [NotNull] Func<T, Vector3> selector, Vector3 value)
        {
            return list.Select(selector).Select(l => (l - value).sqrMagnitude).ToList();
        }

        public static bool CheckArgsIncludeString([NotNull] this IEnumerable<string> args, string include,
            out int index)
        {
            index = args.ToList().FindIndex(k => k.Equals(include, StringComparison.OrdinalIgnoreCase));
            return index > -1;
        }

        [CanBeNull]
        public static ItemAsset GetItemAsset([NotNull] this IEnumerable<string> args, out int index)
        {
            var argsL = args.ToList();
            var assets = Assets.find(EAssetType.ITEM).Cast<ItemAsset>()
                .Where(k => k?.itemName != null && k.name != null).OrderBy(k => k.itemName.Length).ToList();

            for (index = 0; index < argsL.Count; index++)
            {
                var itemAsset = assets.FirstOrDefault(k =>
                    argsL[0].Equals(k.id.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    argsL[0].Split(' ').All(l => k.itemName.ToLower().Contains(l)) ||
                    argsL[0].Split(' ').All(l => k.name.ToLower().Contains(l)));

                if (itemAsset == null)
                    continue;

                return itemAsset;
            }

            index = -1;
            return null;
        }

        public static float GetFloat([NotNull] this IEnumerable<string> args, out int index)
        {
            var output = float.NegativeInfinity;
            index = args.ToList().FindIndex(k => float.TryParse(k, out output));
            return output;
        }

        public static IRocketPlayer GetIRocketPlayer([NotNull] this IEnumerable<string> args, out int index)
        {
            IRocketPlayer output = null;
            index = args.ToList().FindIndex(k =>
            {
                output = UnturnedPlayer.FromName(k);
                if (output == null && ulong.TryParse(k, out var id) && id > 76561197960265728)
                    output = new RocketPlayer(id.ToString());

                return output != null;
            });
            return output;
        }

        public static bool IsNegativeInfinity(this Vector3 vector)
        {
            return float.IsNegativeInfinity(vector.x) || float.IsNegativeInfinity(vector.y) ||
                   float.IsNegativeInfinity(vector.z);
        }
    }
}