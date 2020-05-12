using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pustalorc.Plugins.BaseClustering.API.Classes;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Statics
{
    public static class Extensions
    {
        [CanBeNull]
        public static BaseCluster FindBestCluster([NotNull] this IEnumerable<BaseCluster> source, Buildable target,
            float maxDist)
        {
            var validClusters = source.Where(k => Vector3.Distance(k.CenterBuildable, target.Position) <= maxDist);

            var baseClusters = validClusters.ToList();
            return baseClusters.Count == 0
                ? null
                : baseClusters.OrderBy(k => Vector3.Distance(k.CenterBuildable, target.Position)).FirstOrDefault();
        }

        public static int GetLocalCenterIndex([NotNull] this Dictionary<int, Vector3> source)
        {
            return source.GetLocalDensity().MinVector3();
        }

        public static int GetCenterIndex([NotNull] this IEnumerable<Buildable> source)
        {
            var globalDensity = source.GetDensity(k => k.Position);
            return globalDensity.IndexOf(globalDensity.MinVector3());
        }

        [NotNull]
        public static Dictionary<int, Vector3> GetCluster([NotNull] this IEnumerable<Buildable> source, Vector3 center,
            float radius)
        {
            var radSq = Math.Pow(radius, 2);
            var positions = source.Select(k => k.Position).ToList();
            var cluster = positions.SubtractMaintainOriginalIndices(center).DivideDictionaryVector3(radSq)
                .GetMatchingWithOriginal(positions, v => v.x < 1 && v.y < 1 && v.z < 1);

            var dist = cluster.GetDistances(center);
            var average = dist.Values.Average();
            for (var i = 0; i < dist.Count; i++)
            {
                var std = ExtendedMath.StandardDeviation(dist.Values);

                if (double.IsNaN(std))
                    continue;

                if (dist.Values.ToList()[i] - average <= 3 * std)
                    continue;

                cluster.Remove(i);
                dist.Remove(dist.Keys.ToList()[i]);
                i--;
            }

            return cluster;
        }

        public static Vector3 GetScalar([NotNull] this IEnumerable<Vector3> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var scalar = Vector3.zero;
            var list = source.ToList();

            foreach (var element in list)
            {
                scalar.x += element.x * element.x;
                scalar.y += element.y * element.y;
                scalar.z += element.z * element.z;
            }

            if (list.Count > 0) return scalar / list.Count;

            throw new Exception("The collection had no elements. Cannot divide by 0.");
        }

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

            throw new Exception("The collection had no elements. Cannot divide by 0.");
        }

        [NotNull]
        public static List<Vector3> GetDensity([NotNull] this IEnumerable<Vector3> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var list = source.ToList();
            var mean = list.AverageCenter();
            var scalar = list.GetScalar();

            return list.Select(element =>
                ExtendedMath.Pow(ExtendedMath.Abs(element - mean), 2) + scalar - ExtendedMath.Pow(mean, 2)).ToList();
        }

        [NotNull]
        public static List<Vector3> GetDensity<TSource>([NotNull] this IEnumerable<TSource> source,
            [NotNull] Func<TSource, Vector3> selector)
        {
            return source.Select(selector).GetDensity();
        }

        [NotNull]
        public static Dictionary<int, Vector3> SubtractMaintainOriginalIndices(
            [NotNull] this IEnumerable<Vector3> source,
            Vector3 value)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var list = source.ToList();
            var output = new Dictionary<int, Vector3>();

            for (var i = 0; i < list.Count; i++) output.Add(i, ExtendedMath.Pow(list[i] - value, 2));

            return output;
        }

        public static Vector3 AverageCenter<TSource>([NotNull] this IEnumerable<TSource> source,
            [NotNull] Func<TSource, Vector3> selector)
        {
            return source.Select(selector).AverageCenter();
        }

        [NotNull]
        public static Dictionary<int, Vector3> DivideDictionaryVector3([NotNull] this Dictionary<int, Vector3> source,
            double value)
        {
            return source.ToDictionary(d => d.Key, d => d.Value / (float) value);
        }

        [NotNull]
        public static Dictionary<int, Vector3> GetMatchingWithOriginal([NotNull] this Dictionary<int, Vector3> value,
            List<Vector3> source,
            Predicate<Vector3> match)
        {
            return value.Where(l => match(l.Value)).ToDictionary(l => l.Key, l => source[l.Key]);
        }

        [NotNull]
        public static Dictionary<int, float> GetDistances([NotNull] this Dictionary<int, Vector3> list, Vector3 value)
        {
            return list.ToDictionary(l => l.Key, l => Vector3.Distance(l.Value, value));
        }

        [NotNull]
        public static IEnumerable<float> GetDistances<T>([NotNull] this IEnumerable<T> list,
            [NotNull] Func<T, Vector3> selector, Vector3 value)
        {
            return list.Select(selector).Select(l => Vector3.Distance(l, value)).ToList();
        }

        [NotNull]
        public static List<Vector3> GetLocalScalar([NotNull] this Dictionary<int, Vector3> data)
        {
            return data.Select(d => ExtendedMath.Pow(d.Value, 2) / data.Count).ToList();
        }

        [NotNull]
        public static Dictionary<int, Vector3> GetLocalDensity([NotNull] this Dictionary<int, Vector3> data)
        {
            var density = new Dictionary<int, Vector3>();
            var mean = data.Values.AverageCenter();
            var scalar = data.GetLocalScalar();

            for (var i = 0; i < data.Count; i++)
                density.Add(data.Keys.ToList()[i],
                    ExtendedMath.Pow(ExtendedMath.Abs(data.Values.ToList()[i] - mean), 2) + scalar[i] -
                    ExtendedMath.Pow(mean, 2));

            return density;
        }

        public static Vector3 MinVector3([NotNull] this IEnumerable<Vector3> vectors)
        {
            var smallestVector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            foreach (var vector in vectors.Where(vector => IsSmaller(vector, smallestVector)))
                smallestVector = vector;

            return smallestVector;
        }

        public static int MinVector3([NotNull] this Dictionary<int, Vector3> vectors)
        {
            var smallestVector = new KeyValuePair<int, Vector3>(int.MaxValue,
                new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));

            foreach (var vector in vectors.Where(vector => IsSmaller(vector.Value, smallestVector.Value)))
                smallestVector = vector;

            return smallestVector.Key;
        }

        public static bool IsSmaller(Vector3 x, Vector3 y)
        {
            var total = 0;

            if (x.x < y.x)
                total++;

            if (x.y < y.y)
                total++;

            if (x.z < y.z)
                total++;

            return total >= 2;
        }

        public static bool CheckArgsIncludeString([NotNull] this IEnumerable<string> args, string include, out int index)
        {
            index = args.ToList().FindIndex(k => k.Equals(include, StringComparison.OrdinalIgnoreCase));
            return index > -1;
        }

        public static ushort GetUshort([NotNull] this IEnumerable<string> args, out int index)
        {
            var output = ushort.MaxValue;
            index = args.ToList().FindIndex(k => ushort.TryParse(k, out output));
            return output;
        }

        public static float GetFloat([NotNull] this IEnumerable<string> args, out int index)
        {
            var output = float.MaxValue;
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

                return output == null;
            });
            return output;
        }
    }
}